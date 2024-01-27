using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using Smee.IO.Client;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using MintPlayer.AspNetCore.BotFramework.Services;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Microsoft.Extensions.Options;

namespace MintPlayer.GithubApps;

internal class SmeeWorker : IHostedService
{
    private readonly ISmeeClient smeeClient;
    private readonly IOptions<BotOptions> botOptions;
    private readonly IServiceProvider serviceProvider;
    private readonly ISignatureService signatureService;
    public SmeeWorker(ISmeeClient smeeClient, IOptions<BotOptions> botOptions, IServiceProvider serviceProvider, ISignatureService signatureService)
    {
        this.smeeClient = smeeClient;
        this.botOptions = botOptions;
        this.serviceProvider = serviceProvider;
        this.signatureService = signatureService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        smeeClient.OnMessage += SmeeClient_OnMessage;
        await smeeClient.StartAsync(cancellationToken);
    }

    private async void SmeeClient_OnMessage(object? sender, Smee.IO.Client.Dto.SmeeEvent e)
    {
        if (e.Event == SmeeEventType.Message)
        {
            var jsonFormatted = e.Data.GetFormattedJson();
            var signatureSha256 = e.Data.Headers["x-hub-signature-256"];
            var secret = botOptions.Value.WebhookSecret;
            if (!signatureService.VerifySignature(signatureSha256, secret, jsonFormatted))
            {
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<WebhookEventProcessor>();
                var json = System.Text.Json.JsonSerializer.Deserialize<IssuesEvent>(jsonFormatted);
                await processor.ProcessWebhookAsync(
                    e.Data.Headers.ToDictionary(h => h.Key, h => new Microsoft.Extensions.Primitives.StringValues(h.Value)),
                    jsonFormatted
                );
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        smeeClient.Stop();
        return Task.CompletedTask;
    }
}
