using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.PullRequest;

namespace MintPlayer.GithubApps;

// In your Github App, you should set the webhook-url to
// https://example.com/api/github/webhooks
// When this website is deployed.

public class GithubProcessor : BaseWebhookProcessor
{
    #region Constructor
    private readonly IAuthenticatedGithubService authenticatedGithubService;
    public GithubProcessor(IAuthenticatedGithubService authenticatedGithubService, ISignatureService signatureService, IOptions<BotOptions> botOptions)
        : base(signatureService, botOptions)
    {
        this.authenticatedGithubService = authenticatedGithubService;
    }
    #endregion

    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        var gitHubClient = await authenticatedGithubService.GetAuthenticatedGithubClient(issuesEvent.Installation!.Id);
        await gitHubClient.Issue.Comment.Create(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, "Thanks for creating an issue");
    }

    protected override async Task ProcessPullRequestWebhookAsync(WebhookHeaders headers, PullRequestEvent pullRequestEvent, PullRequestAction action)
    {
        if (pullRequestEvent is PullRequestClosedEvent ev)
        {
            var client = await authenticatedGithubService.GetAuthenticatedGithubClient(ev.Installation!.Id);
            var mergeCommit = await client.Git.Commit.Get(ev.Repository!.Id, ev.PullRequest.MergeCommitSha);

            //await client.Git.Reference.Update(0, "master", new Octokit.ReferenceUpdate())
            foreach (var label in ev.PullRequest.Labels)
            {
                var releaseBranch = await client.Repository.Branch.Get(ev.Repository!.Id, label.Name);
                if (releaseBranch != null)
                {
                    // Here we should be able to cherry-pick the mergeCommit into the releaseBranch
                }
            }
        }
    }
}
