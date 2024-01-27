using Octokit;

namespace MintPlayer.AspNetCore.BotFramework.Abstractions;

public interface IAuthenticatedGithubService
{
    Task<IGitHubClient> GetAuthenticatedGithubClient(long installationId);
}
