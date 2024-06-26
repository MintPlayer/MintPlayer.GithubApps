using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Extensions;
using MintPlayer.AspNetCore.LoggerProviders;
using MintPlayer.GithubApps;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using Smee.IO.Client;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services
    .Configure<BotOptions>(options =>
    {
        options.AppId = builder.Configuration["GithubApp:AppId"];
        options.ClientId = builder.Configuration["GithubApp:ClientId"];
        options.WebhookUrl = builder.Configuration["GithubApp:WebhookUrl"];
        options.WebhookSecret = builder.Configuration["GithubApp:WebhookSecret"];
        options.PrivateKey = builder.Configuration["GithubApp:PrivateKey"];
        options.PrivateKeyPath = builder.Configuration["GithubApp:PrivateKeyPath"];
    })
    .Configure<FileLoggerOptions>(options =>
    {
        options.FileName = "Logs/Log.txt";
    });

// Add services to the container.
//builder.Services.AddScoped<IGithubService, GithubService>();
builder.AddBotFramework<GithubProcessor>();
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IDevSocketService, DevSocketService>();
}

builder.Services.AddLogging(options =>
{
    options.AddFileProvider();
});
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    options.RequestBodyLogLimit = 16384;
    options.ResponseBodyLogLimit = 16384;
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpLogging();

if (builder.Environment.IsProduction())
{
    app.UseWebSockets();
}

app.UseHttpsRedirection();
app.MapHealthChecks("/healthz");
app.MapGet("/test/{number:int}", (int number) => $"Hello world {number}");
app.MapBotFramework();

if (builder.Environment.IsProduction())
{
    app.Map("/ws", async (context) =>
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            //context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.StatusCode = 502;
            return;
        }

        if (!context.Request.Headers.TryGetValue("Webhook-Proxy-Authorization", out var authorizationHeader))
        {
            //context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.StatusCode = 503;
            return;
        }

        var match = Regexes.authorizationRegex().Match(authorizationHeader[0] ?? string.Empty);
        if (!match.Success)
        {
            //context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.StatusCode = 504;
            return;
        }

        var proxyUser = builder.Configuration["WebhookProxy:Username"];
        var proxyPassword = builder.Configuration["WebhookProxy:Password"];

        if (match.Groups["username"].Value != proxyUser || match.Groups["password"].Value != proxyPassword)
        {
            //context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.StatusCode = 505;
            return;
        }

        using var ws = await context.WebSockets.AcceptWebSocketAsync();

        var socketService = app.Services.GetRequiredService<IDevSocketService>();
        await socketService.NewSocketClient(new SocketClient(ws));

        //var message = "Hello World!";
        //var bytes = Encoding.UTF8.GetBytes(message);

        //var working = true;
        //while (working)
        //{
        //    switch (ws.State)
        //    {
        //        case WebSocketState.Open:
        //            var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
        //            await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        //            break;
        //        case WebSocketState.Closed:
        //        case WebSocketState.Aborted:
        //            working = false;
        //            break;
        //    }

        //    await Task.Delay(1000);
        //}
    });
}
else if (builder.Environment.IsDevelopment())
{
    var username = builder.Configuration["WebhookProxy:Username"];
    var password = builder.Configuration["WebhookProxy:Password"];
    var url = builder.Configuration["WebhookProxy:ProductionWebsocketUrl"] ?? throw new InvalidDataException();

    var ws = new ClientWebSocket();
    ws.Options.SetRequestHeader("Webhook-Proxy-Authorization", $"Basic: {username}; {password}");
    await ws.ConnectAsync(new Uri(url), CancellationToken.None);

    //await Task.Run(async () =>
    //{
    //    var buffer = new byte[4096];
    //    while (true)
    //    {
    //        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
    //        if (result.MessageType == WebSocketMessageType.Close) break;
    //        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);


    //        var split = message.Split("\r\n\r\n");
    //        var headers = split[0].Split("\r\n")
    //            .Select(h => h.Split(':'))
    //            .ToDictionary(h => h[0].Trim(), h => new Microsoft.Extensions.Primitives.StringValues(h[1].Trim()));
    //        var body = split[1];


    //        using var scope = app.Services.CreateScope();
    //        var processor = scope.ServiceProvider.GetRequiredService<WebhookEventProcessor>();

    //        await processor.ProcessWebhookAsync(
    //            headers,
    //            body
    //        );
    //    }
    //});
}

app.Run();

static partial class Regexes
{
    [GeneratedRegex(@"Basic:\s*(?<username>.+?)\s*\;\s*(?<password>.+)$")]
    public static partial Regex authorizationRegex();
}
