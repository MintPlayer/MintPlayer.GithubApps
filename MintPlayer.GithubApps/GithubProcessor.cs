using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Events.PullRequest;
using Octokit.Webhooks.Events.PullRequestReview;

namespace MintPlayer.GithubApps;

// In your Github App, you should set the webhook-url to
// https://example.com/api/github/webhooks
// https://cherry-picker.mintplayer.com/api/github/webhooks
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

    // protected override async Task ProcessPullRequestWebhookAsync(WebhookHeaders headers, PullRequestEvent pullRequestEvent, PullRequestAction action)
    // {
    //     switch (pullRequestEvent)
    //     {
    //         case PullRequestClosedEvent closeEv:
    //             var client = await authenticatedGithubService.GetAuthenticatedGithubClient(closeEv.Installation!.Id);
    //             var mergeCommit = await client.Git.Commit.Get(closeEv.Repository!.Id, closeEv.PullRequest.MergeCommitSha);

    //             //await client.Git.Reference.Update(0, "master", new Octokit.ReferenceUpdate())
    //             foreach (var label in closeEv.PullRequest.Labels)
    //             {
    //                 var releaseBranch = await client.Repository.Branch.Get(closeEv.Repository!.Id, label.Name);
    //                 if (releaseBranch != null)
    //                 {
    //                     // Here we should be able to cherry-pick the mergeCommit into the releaseBranch
    //                     var c = new Octokit.NewCommit("", "");
    //                     //c.Contents = ;
    //                     //client.Git.Commit.Create(, )
    //                 }
    //             }
    //             break;
    //         case PullRequestSynchronizeEvent syncEv:
    //             break;
    //     }
    // }

    // protected override async Task ProcessPullRequestReviewWebhookAsync(WebhookHeaders headers, Octokit.Webhooks.Events.PullRequestReviewEvent pullRequestReviewEvent, PullRequestReviewAction action)
    // {
    //     if (pullRequestReviewEvent.Review.State.Value == Octokit.Webhooks.Models.PullRequestReviewEvent.ReviewState.ChangesRequested)
    //     {

    //     }
    //     else
    //     {
    //         await base.ProcessPullRequestReviewWebhookAsync(headers, pullRequestReviewEvent, action);
    //     }
    // }
}
