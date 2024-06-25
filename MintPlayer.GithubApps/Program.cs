using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Extensions;
using MintPlayer.AspNetCore.LoggerProviders;
using MintPlayer.GithubApps;
using Octokit.Webhooks.AspNetCore;
using Smee.IO.Client;
using System.Net.WebSockets;
using System.Text;

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
app.Map("/ws", async (context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var ws = await context.WebSockets.AcceptWebSocketAsync();

    var message = "Hello World!";
    var bytes = Encoding.UTF8.GetBytes(message);

    var working = true;
    while (working)
    {
        switch (ws.State)
        {
            case WebSocketState.Open:
                var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);
                await ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
                break;
            case WebSocketState.Closed:
            case WebSocketState.Aborted:
                working = false;
                break;
        }

        await Task.Delay(1000);
    }
});

app.Run();
