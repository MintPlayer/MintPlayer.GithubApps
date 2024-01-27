using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using MintPlayer.AspNetCore.BotFramework.Services;
using Octokit.Webhooks;

namespace MintPlayer.AspNetCore.BotFramework;

public abstract class BaseWebhookProcessor : WebhookEventProcessor
{
    private readonly ISignatureService signatureService;
    private readonly IOptions<BotOptions> botOptions;
    public BaseWebhookProcessor(ISignatureService signatureService, IOptions<BotOptions> botOptions)
    {
        this.signatureService = signatureService;
        this.botOptions = botOptions;
    }

    public override async Task ProcessWebhookAsync(IDictionary<string, StringValues> headers, string body)
    {
        // This base method is using a case-sensitive Dictionary.
        // This means that headers can't be found in most situations.
        // We override the method, and create a case-insensitive Dictionary instead.
        var caseInsensitiveHeaders = new Dictionary<string, StringValues>(headers, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrEmpty(botOptions.Value.WebhookSecret))
        {
            // This is the perfect place to verify the signature against the secret
            caseInsensitiveHeaders.TryGetValue("X-Hub-Signature-256", out var signatureSha256);
            if (!signatureService.VerifySignature(signatureSha256, botOptions.Value.WebhookSecret, body))
            {
                return;
            }
        }

        // PROBLEM: This method calls below method, and fails because there's no secret
        await base.ProcessWebhookAsync(caseInsensitiveHeaders, body);
    }

    public override async Task ProcessWebhookAsync(WebhookHeaders headers, WebhookEvent webhookEvent)
    {
        if (!string.IsNullOrEmpty(botOptions.Value.WebhookSecret))
        {
            // We cannot read the webhook secret from the headers here :-(
            throw new InvalidOperationException("When a webhook secret is set, the overload ProcessWebhookAsync(WebhookHeaders, WebhookEvent) is not available");
        }

        await base.ProcessWebhookAsync(headers, webhookEvent);
    }
}
