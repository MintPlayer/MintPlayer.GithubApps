using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework.Services;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Issues;
using System.Text.Json;

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

    //public override WebhookEvent DeserializeWebhookEvent(WebhookHeaders headers, string body)
    //{
    //    return headers.Event switch
    //    {
    //        WebhookEventType.BranchProtectionRule => JsonSerializer.Deserialize<BranchProtectionRuleEvent>(body)!,
    //        WebhookEventType.CheckRun => JsonSerializer.Deserialize<CheckRunEvent>(body)!,
    //        WebhookEventType.CheckSuite => JsonSerializer.Deserialize<CheckSuiteEvent>(body)!,
    //        WebhookEventType.CodeScanningAlert => JsonSerializer.Deserialize<CodeScanningAlertEvent>(body)!,
    //        WebhookEventType.CommitComment => JsonSerializer.Deserialize<CommitCommentEvent>(body)!,
    //        WebhookEventType.ContentReference => JsonSerializer.Deserialize<ContentReferenceEvent>(body)!,
    //        WebhookEventType.Create => JsonSerializer.Deserialize<CreateEvent>(body)!,
    //        WebhookEventType.CustomProperty => JsonSerializer.Deserialize<CustomPropertyEvent>(body)!,
    //        WebhookEventType.CustomPropertyValues => JsonSerializer.Deserialize<CustomPropertyValuesEvent>(body)!,
    //        WebhookEventType.Delete => JsonSerializer.Deserialize<DeleteEvent>(body)!,
    //        WebhookEventType.DependabotAlert => JsonSerializer.Deserialize<DependabotAlertEvent>(body)!,
    //        WebhookEventType.DeployKey => JsonSerializer.Deserialize<DeployKeyEvent>(body)!,
    //        WebhookEventType.Deployment => JsonSerializer.Deserialize<DeploymentEvent>(body)!,
    //        WebhookEventType.DeploymentProtectionRule => JsonSerializer.Deserialize<DeploymentProtectionRuleEvent>(body)!,
    //        WebhookEventType.DeploymentReview => JsonSerializer.Deserialize<DeploymentReviewEvent>(body)!,
    //        WebhookEventType.DeploymentStatus => JsonSerializer.Deserialize<DeploymentStatusEvent>(body)!,
    //        WebhookEventType.Discussion => JsonSerializer.Deserialize<DiscussionEvent>(body)!,
    //        WebhookEventType.DiscussionComment => JsonSerializer.Deserialize<DiscussionCommentEvent>(body)!,
    //        WebhookEventType.Fork => JsonSerializer.Deserialize<ForkEvent>(body)!,
    //        WebhookEventType.GithubAppAuthorization => JsonSerializer.Deserialize<GithubAppAuthorizationEvent>(body)!,
    //        WebhookEventType.Gollum => JsonSerializer.Deserialize<GollumEvent>(body)!,
    //        WebhookEventType.Installation => JsonSerializer.Deserialize<InstallationEvent>(body)!,
    //        WebhookEventType.InstallationRepositories => JsonSerializer.Deserialize<InstallationRepositoriesEvent>(body)!,
    //        WebhookEventType.InstallationTarget => JsonSerializer.Deserialize<InstallationTargetEvent>(body)!,
    //        WebhookEventType.IssueComment => JsonSerializer.Deserialize<IssueCommentEvent>(body)!,
    //        WebhookEventType.Issues => JsonSerializer.Deserialize<IssuesEvent>(body)!,
    //        WebhookEventType.Label => JsonSerializer.Deserialize<LabelEvent>(body)!,
    //        WebhookEventType.MarketplacePurchase => JsonSerializer.Deserialize<MarketplacePurchaseEvent>(body)!,
    //        WebhookEventType.Member => JsonSerializer.Deserialize<MemberEvent>(body)!,
    //        WebhookEventType.Membership => JsonSerializer.Deserialize<MembershipEvent>(body)!,
    //        WebhookEventType.MergeGroup => JsonSerializer.Deserialize<MergeGroupEvent>(body)!,
    //        WebhookEventType.MergeQueueEntry => JsonSerializer.Deserialize<MergeQueueEntryEvent>(body)!,
    //        WebhookEventType.Meta => JsonSerializer.Deserialize<MetaEvent>(body)!,
    //        WebhookEventType.Milestone => JsonSerializer.Deserialize<MilestoneEvent>(body)!,
    //        WebhookEventType.OrgBlock => JsonSerializer.Deserialize<OrgBlockEvent>(body)!,
    //        WebhookEventType.Organization => JsonSerializer.Deserialize<OrganizationEvent>(body)!,
    //        WebhookEventType.Package => JsonSerializer.Deserialize<PackageEvent>(body)!,
    //        WebhookEventType.PageBuild => JsonSerializer.Deserialize<PageBuildEvent>(body)!,
    //        WebhookEventType.Ping => JsonSerializer.Deserialize<PingEvent>(body)!,
    //        WebhookEventType.Project => JsonSerializer.Deserialize<ProjectEvent>(body)!,
    //        WebhookEventType.ProjectCard => JsonSerializer.Deserialize<ProjectCardEvent>(body)!,
    //        WebhookEventType.ProjectColumn => JsonSerializer.Deserialize<ProjectColumnEvent>(body)!,
    //        WebhookEventType.ProjectsV2Item => JsonSerializer.Deserialize<ProjectsV2ItemEvent>(body)!,
    //        WebhookEventType.Public => JsonSerializer.Deserialize<PublicEvent>(body)!,
    //        WebhookEventType.PullRequest => JsonSerializer.Deserialize<PullRequestEvent>(body)!,
    //        WebhookEventType.PullRequestReview => JsonSerializer.Deserialize<PullRequestReviewEvent>(body)!,
    //        WebhookEventType.PullRequestReviewComment => JsonSerializer.Deserialize<PullRequestReviewCommentEvent>(body)!,
    //        WebhookEventType.PullRequestReviewThread => JsonSerializer.Deserialize<PullRequestReviewThreadEvent>(body)!,
    //        WebhookEventType.Push => JsonSerializer.Deserialize<PushEvent>(body)!,
    //        WebhookEventType.Release => JsonSerializer.Deserialize<ReleaseEvent>(body)!,
    //        WebhookEventType.RegistryPackage => JsonSerializer.Deserialize<RegistryPackageEvent>(body)!,
    //        WebhookEventType.Repository => JsonSerializer.Deserialize<RepositoryEvent>(body)!,
    //        WebhookEventType.RepositoryDispatch => JsonSerializer.Deserialize<RepositoryDispatchEvent>(body)!,
    //        WebhookEventType.RepositoryImport => JsonSerializer.Deserialize<RepositoryImportEvent>(body)!,
    //        WebhookEventType.RepositoryRuleset => JsonSerializer.Deserialize<RepositoryRulesetEvent>(body)!,
    //        WebhookEventType.RepositoryVulnerabilityAlert => JsonSerializer.Deserialize<RepositoryVulnerabilityAlertEvent>(body)!,
    //        WebhookEventType.SecretScanningAlert => JsonSerializer.Deserialize<SecretScanningAlertEvent>(body)!,
    //        WebhookEventType.SecurityAdvisory => JsonSerializer.Deserialize<SecurityAdvisoryEvent>(body)!,
    //        WebhookEventType.Sponsorship => JsonSerializer.Deserialize<SponsorshipEvent>(body)!,
    //        WebhookEventType.Star => JsonSerializer.Deserialize<StarEvent>(body)!,
    //        WebhookEventType.Status => JsonSerializer.Deserialize<StatusEvent>(body)!,
    //        WebhookEventType.Team => JsonSerializer.Deserialize<TeamEvent>(body)!,
    //        WebhookEventType.TeamAdd => JsonSerializer.Deserialize<TeamAddEvent>(body)!,
    //        WebhookEventType.Watch => JsonSerializer.Deserialize<WatchEvent>(body)!,
    //        WebhookEventType.WorkflowDispatch => JsonSerializer.Deserialize<WorkflowDispatchEvent>(body)!,
    //        WebhookEventType.WorkflowJob => JsonSerializer.Deserialize<WorkflowJobEvent>(body)!,
    //        WebhookEventType.WorkflowRun => JsonSerializer.Deserialize<WorkflowRunEvent>(body)!,
    //        _ => throw new JsonException("Unable to deserialize event"),
    //    };
    //}

    public override async Task ProcessWebhookAsync(IDictionary<string, StringValues> headers, string body)
    {
        var caseInsensitiveHeaders = new Dictionary<string, StringValues>(headers, StringComparer.OrdinalIgnoreCase);
        var webhookHeaders = WebhookHeaders.Parse(caseInsensitiveHeaders);
        var webhookEvent = this.DeserializeWebhookEvent(webhookHeaders, body);
        await ProcessWebhookAsync(webhookHeaders, webhookEvent);










        //caseInsensitiveHeaders.TryGetValue("X-GitHub-Event", out var eventName);
        //var webhook = eventName[0] switch
        //{
        //    WebhookEventType.Issues => System.Text.Json.JsonSerializer.Deserialize<IssuesEvent>(body)!
        //};
        
        //var result = webhook switch
        //{
        //    IssuesEvent issuesEvent2 => System.Text.Json.JsonSerializer.Deserialize<IssuesEvent>(body)!
        //};
        //var webhookHeaders = WebhookHeaders.Parse(headers);

        //var issuesEvent = (IssuesEvent)webhook;
        //switch (issuesEvent.Action)
        //{
        //    case IssuesActionValue.Opened:
        //        break;
        //    default:
        //        break;
        //}
    }

    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        var gitHubClient = await authenticatedGithubService.GetAuthenticatedGithubClient(issuesEvent.Installation!.Id);
        await gitHubClient.Issue.Comment.Create(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, "Thanks for creating an issue");
    }
}
