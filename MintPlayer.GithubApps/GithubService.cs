using Newtonsoft.Json.Linq;
using Octokit;

namespace MintPlayer.GithubApps;

public interface IGithubService
{
    Task OnMessage(JObject message);
    Task OnIssueOpened(long repositoryId, int issueNumber, IGitHubClient gitHubClient, JObject message);
}

public class GithubService : IGithubService
{
    public async Task OnIssueOpened(long repositoryId, int issueNumber, IGitHubClient gitHubClient, JObject message)
    {
        await gitHubClient.Issue.Comment.Create(repositoryId, issueNumber, "Thanks for creating an issue");
    }

    public Task OnMessage(JObject message) => Task.CompletedTask;
}
