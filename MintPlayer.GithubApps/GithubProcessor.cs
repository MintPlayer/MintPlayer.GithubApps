using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework.Services;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;

namespace MintPlayer.GithubApps;

public class GithubProcessor : WebhookEventProcessor
{
    #region Constructor
    private readonly IAuthenticatedGithubService authenticatedGithubService;
    public GithubProcessor(IAuthenticatedGithubService authenticatedGithubService)
    {
        this.authenticatedGithubService = authenticatedGithubService;
    }
    #endregion

    public override async Task ProcessWebhookAsync(IDictionary<string, StringValues> headers, string body)
    {
        var json = System.Text.Json.JsonSerializer.Deserialize<IssuesEvent>(body);

        await base.ProcessWebhookAsync(headers, body);
    }
}
