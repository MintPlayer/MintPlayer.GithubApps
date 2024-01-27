using Octokit;

namespace MintPlayer.AspNetCore.BotFramework.Services;

public interface IAuthenticatedGithubService
{
    Task<GitHubClient> GetAuthenticatedGithubClient(long installationId);
}
