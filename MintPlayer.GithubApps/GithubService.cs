using Newtonsoft.Json.Linq;

namespace MintPlayer.GithubApps;

public interface IGithubService
{
    Task OnMessage(JObject message);
}

public class GithubService : IGithubService
{
    public Task OnMessage(JObject message)
    {
        throw new NotImplementedException();
    }
}
