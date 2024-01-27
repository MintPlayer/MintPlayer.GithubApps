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

namespace MintPlayer.GithubApps;

internal class SmeeWorker : IHostedService
{
    private readonly ISmeeClient smeeClient;
    private readonly IConfiguration configuration;
    private readonly IServiceProvider serviceProvider;
    private readonly ISignatureService signatureService;
    public SmeeWorker(ISmeeClient smeeClient, IConfiguration configuration, IServiceProvider serviceProvider, ISignatureService signatureService)
    {
        this.smeeClient = smeeClient;
        this.configuration = configuration;
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
            var secret = configuration["GithubApp:WebhookSecret"];
            if (!signatureService.VerifySignature(signatureSha256, secret, jsonFormatted))
            {
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var msgBody = (JObject)e.Data.Body;
                //var githubService = scope.ServiceProvider.GetRequiredService<IGithubService>();
                //await githubService.OnMessage(msgBody);

                var installationId = msgBody["installation"]["id"].Value<long>();
                var githubService = scope.ServiceProvider.GetRequiredService<IAuthenticatedGithubService>();
                var repoClient = await githubService.GetAuthenticatedGithubClient(installationId);

                var processor = scope.ServiceProvider.GetRequiredService<WebhookEventProcessor>();
                var json = System.Text.Json.JsonSerializer.Deserialize<IssuesEvent>(jsonFormatted);
                await processor.ProcessWebhookAsync(
                    e.Data.Headers.ToDictionary(h => h.Key, h => new Microsoft.Extensions.Primitives.StringValues(h.Value)),
                    jsonFormatted
                );

                //var action = msgBody["action"].Value<string>();
                //switch (action)
                //{
                //    case "created":
                //        var accessTokensUrl = msgBody["installation"]["access_tokens_url"];
                //        var appId = msgBody["installation"]["app_id"];
                //        break;

                //    case "deleted":
                //        break;

                //    case "opened":
                //        //var accessTokensUrl = msg["installation"]["access_tokens_url"];
                //        //var appId = msg["installation"]["app_id"];

                //        var repositoryId = msgBody["repository"]["id"].Value<long>();
                //        var issueNumber = msgBody["issue"]["number"].Value<int>();
                //        await githubService.OnIssueOpened(repositoryId, issueNumber, repoClient, msgBody);
                //        break;
                //}
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        smeeClient.Stop();
        return Task.CompletedTask;
    }
}
