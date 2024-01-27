using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.Issues;

namespace MintPlayer.GithubApps;

public class GithubProcessor : WebhookEventProcessor
{
    #region Constructor
    private readonly IAuthenticatedGithubService authenticatedGithubService;
    private readonly ISignatureService signatureService;
    private readonly IOptions<BotOptions> botOptions;
    public GithubProcessor(IAuthenticatedGithubService authenticatedGithubService, ISignatureService signatureService, IOptions<BotOptions> botOptions)
    {
        this.authenticatedGithubService = authenticatedGithubService;
        this.signatureService = signatureService;
        this.botOptions = botOptions;
    }
    #endregion

    public override async Task ProcessWebhookAsync(IDictionary<string, StringValues> headers, string body)
    {
        // This base method is using a case-sensitive Dictionary.
        // This means that headers can't be found in most situations.
        // We override the method, and create a case-insensitive Dictionary instead.

        var caseInsensitiveHeaders = new Dictionary<string, StringValues>(headers, StringComparer.OrdinalIgnoreCase);
        var webhookHeaders = WebhookHeaders.Parse(caseInsensitiveHeaders);

        // Additionally, this is the perfect place to verify the signature against the secret
        caseInsensitiveHeaders.TryGetValue("X-Hub-Signature-256", out var signatureSha256);
        if (!signatureService.VerifySignature(signatureSha256, botOptions.Value.WebhookSecret, body))
        {
            return;
        }

        var webhookEvent = this.DeserializeWebhookEvent(webhookHeaders, body);
        await ProcessWebhookAsync(webhookHeaders, webhookEvent);
    }

    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        var gitHubClient = await authenticatedGithubService.GetAuthenticatedGithubClient(issuesEvent.Installation!.Id);
        await gitHubClient.Issue.Comment.Create(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, "Thanks for creating an issue");
    }
}
