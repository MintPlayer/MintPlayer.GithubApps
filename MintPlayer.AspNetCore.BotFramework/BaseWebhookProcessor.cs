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

    // The processor should be registered as a scoped service
    // When the signature can be read from the headers,
    // We can store whether it's correct.
    private bool verifiedSignature = false;

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
            else
            {
                verifiedSignature = true;
            }
        }

        await base.ProcessWebhookAsync(caseInsensitiveHeaders, body);
    }

    public override async Task ProcessWebhookAsync(WebhookHeaders headers, WebhookEvent webhookEvent)
    {
        /// Note: the code MUST ALWAYS call <see cref="ProcessWebhookAsync(IDictionary[string, StringValues] headers, string body)"></see>
        /// The middleware already calls that method initially
        /// The SMEE service must too
        /// Otherwise the VerifySignature won't happen
        if (!string.IsNullOrEmpty(botOptions.Value.WebhookSecret))
        {
            if (!verifiedSignature)
            {
                throw new InvalidOperationException("Signature verification was skipped");
            }
        }
        await base.ProcessWebhookAsync(headers, webhookEvent);
    }
}
