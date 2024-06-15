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
    private readonly IServiceProvider serviceProvider;
    public SmeeWorker(ISmeeClient smeeClient, IServiceProvider serviceProvider)
    {
        this.smeeClient = smeeClient;
        this.serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        smeeClient.OnMessage += SmeeClient_OnMessage;

        // StartAsync is actually a blocking call
        smeeClient.StartAsync(cancellationToken).ContinueWith(delegate { }, TaskContinuationOptions.OnlyOnFaulted);
        return Task.CompletedTask;
    }

    private async void SmeeClient_OnMessage(object? sender, Smee.IO.Client.Dto.SmeeEvent e)
    {
        if (e.Event == SmeeEventType.Message)
        {
            var jsonFormatted = e.Data.GetFormattedJson();
            using (var scope = serviceProvider.CreateScope())
            {
                var processor = scope.ServiceProvider.GetRequiredService<WebhookEventProcessor>();
                //var json = System.Text.Json.JsonSerializer.Deserialize<IssuesEvent>(jsonFormatted);
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
