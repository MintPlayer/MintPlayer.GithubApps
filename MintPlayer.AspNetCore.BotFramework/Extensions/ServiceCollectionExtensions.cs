using MintPlayer.AspNetCore.BotFramework.Abstractions;
using MintPlayer.AspNetCore.BotFramework.Services;

namespace MintPlayer.AspNetCore.BotFramework.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBotFramework(this IServiceCollection services)
    {
        return services
            .AddTransient<ISignatureService, SignatureService>();
    }
}
