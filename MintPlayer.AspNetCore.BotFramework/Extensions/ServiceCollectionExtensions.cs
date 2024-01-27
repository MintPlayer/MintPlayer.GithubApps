using Microsoft.Extensions.Options;
using MintPlayer.AspNetCore.BotFramework.Abstractions;
using MintPlayer.AspNetCore.BotFramework.Services;
using MintPlayer.GithubApps;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using Octokit.Webhooks.Models;
using Smee.IO.Client;

namespace MintPlayer.AspNetCore.BotFramework.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotFramework<TWebhookEventProcessor>(this WebApplicationBuilder builder)
        where TWebhookEventProcessor : WebhookEventProcessor //, IHasAuthenticatedGithubClient
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHostedService<SmeeWorker>();
            builder.Services.AddSingleton<ISmeeClient>(provider =>
            {
                var options = provider.GetService<IOptions<BotOptions>>();
                return new SmeeClient(new Uri(options?.Value.WebhookUrl ?? string.Empty));
            });
        }

        return builder.Services
            .AddTransient<ISignatureService, SignatureService>()
            .AddScoped<IAuthenticatedGithubService, AuthenticatedGithubService>()
            .AddScoped<WebhookEventProcessor, TWebhookEventProcessor>();
    }

    public static IEndpointRouteBuilder MapBotFramework(this WebApplication app, string path = "/api/github/webhooks")
    {
        var options = app.Services.GetRequiredService<IOptions<BotOptions>>();
        app.MapGitHubWebhooks(secret: options.Value.WebhookSecret ?? string.Empty);
        return app;
    }
}
