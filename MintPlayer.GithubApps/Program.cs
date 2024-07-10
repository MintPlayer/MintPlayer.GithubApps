using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Extensions;
using MintPlayer.AspNetCore.LoggerProviders;
using MintPlayer.GithubApps;
using Newtonsoft.Json;
using Octokit.Webhooks;
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
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var proxyUser = builder.Configuration["WebhookProxy:Username"];
        var proxyPassword = builder.Configuration["WebhookProxy:Password"];

        using var ws = await context.WebSockets.AcceptWebSocketAsync("wss");

        // Receive handshake
        var handshake = await ws.ReadObject<Handshake>();
        //if (handshake.Username != proxyUser || handshake.Password != proxyPassword)
        //{
        //    await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Wrong credentials", CancellationToken.None);
        //    return;
        //}

        var socketService = app.Services.GetRequiredService<IDevSocketService>();
        await socketService.NewSocketClient(new SocketClient(ws));
    });
}
else if (builder.Environment.IsDevelopment())
{
    var username = builder.Configuration["WebhookProxy:Username"];
    var password = builder.Configuration["WebhookProxy:Password"];
    var url = builder.Configuration["WebhookProxy:ProductionWebsocketUrl"] ?? throw new InvalidDataException();

    var ws = new ClientWebSocket();
    ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(1);
    ws.Options.AddSubProtocol("ws");
    ws.Options.AddSubProtocol("wss");
    await ws.ConnectAsync(new Uri(url), CancellationToken.None);

    await Task.Run(async () =>
    {
        var handshake = new Handshake
        {
            Username = username,
            Password = password,
        };
        await ws.WriteObject(handshake);

        var buffer = new byte[512];
        while (true)
        {
            var message = await ws.ReadMessage();

            var split = message.Split("\r\n\r\n");
            var headers = split[0].Split("\r\n")
                .Select(h => h.Split(':'))
                .ToDictionary(h => h[0].Trim(), h => new Microsoft.Extensions.Primitives.StringValues(h[1].Trim()));
            var body = split[1];


            using var scope = app.Services.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<WebhookEventProcessor>();

            await processor.ProcessWebhookAsync(
                headers,
                body
            );
        }
    });
}

app.Run();

static partial class Regexes
{
    [GeneratedRegex(@"Basic:\s*(?<username>.+?)\s*\;\s*(?<password>.+)$")]
    public static partial Regex authorizationRegex();
}

class Handshake
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

static class SocketExtensions
{
    public static async Task<string> ReadMessage(this WebSocket ws)
    {
        var buffer = new byte[512];
        byte[] fullMessage = [];
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            fullMessage = fullMessage.Concat(buffer).ToArray();
        }
        while (!result.EndOfMessage);

        if (result.MessageType == WebSocketMessageType.Close)
        {
            throw new WebSocketException("The websocket was closed");
        }

        var message = Encoding.UTF8.GetString(fullMessage);
        return message;
    }

    public static async Task WriteMessage(this WebSocket ws, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public static async Task<T> ReadObject<T>(this WebSocket ws)
    {
        var message = await ws.ReadMessage();
        var obj = JsonConvert.DeserializeObject<T>(message);
        return obj;
    }

    public static async Task WriteObject<T>(this WebSocket ws, T obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        await ws.WriteMessage(json);
    }
}