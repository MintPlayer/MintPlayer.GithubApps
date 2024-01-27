using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Issues;

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
        // This base method is using a case-sensitive Dictionary.
        // This means that headers can't be found in most situations.
        // We override the method, and create a case-insensitive Dictionary instead.

        var caseInsensitiveHeaders = new Dictionary<string, StringValues>(headers, StringComparer.OrdinalIgnoreCase);
        var webhookHeaders = WebhookHeaders.Parse(caseInsensitiveHeaders);
        var webhookEvent = this.DeserializeWebhookEvent(webhookHeaders, body);
        await ProcessWebhookAsync(webhookHeaders, webhookEvent);
    }

    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        var gitHubClient = await authenticatedGithubService.GetAuthenticatedGithubClient(issuesEvent.Installation!.Id);
        await gitHubClient.Issue.Comment.Create(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, "Thanks for creating an issue");
    }
}
