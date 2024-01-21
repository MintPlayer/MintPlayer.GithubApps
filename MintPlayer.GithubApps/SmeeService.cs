using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using Smee.IO.Client;
using System.Security.Cryptography;
using System.Text;

namespace MintPlayer.GithubApps;

public class SmeeService : IHostedService
{
    private readonly ISmeeClient smeeClient;
    private readonly IConfiguration configuration;
    public SmeeService(ISmeeClient smeeClient, IConfiguration configuration)
    {
        this.smeeClient = smeeClient;
        this.configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        smeeClient.OnMessage += SmeeClient_OnMessage;
        await smeeClient.StartAsync(cancellationToken);
    }

    //List<SmeeConnectedClient> clients = new List<SmeeConnectedClient>();

    private async void SmeeClient_OnMessage(object? sender, Smee.IO.Client.Dto.SmeeEvent e)
    {
        if (e.Event == SmeeEventType.Message)
        {
            var msg = (JObject)e.Data.Body;

            var action = msg["action"].Value<string>();
            if (action == "created")
            {
                var accessTokensUrl = msg["installation"]["access_tokens_url"];
                var appId = msg["installation"]["app_id"];
                var installationId = msg["installation"]["id"];

                //clients.Add(new SmeeConnectedClient
                //{
                //    AppId = appId.Value<string>(),
                //    AccessTokensUrl = accessTokensUrl.Value<string>(),
                //    InstallationId = installationId.Value<long>(),
                //});
            }
            
            if (action == "deleted")
            {

            }

            if (action == "opened")
            {
                //var accessTokensUrl = msg["installation"]["access_tokens_url"];
                //var appId = msg["installation"]["app_id"];
                var installationId = msg["installation"]["id"].Value<long>();

                var jwt = GetJwt(configuration["GithubApp:AppId"]!, configuration["GithubApp:PrivateKeyPath"]!);

                //var httpclient = new HttpClient();
                //var content = new StringContent("");
                //content.Headers.Add(HeaderNames.Authorization, jwt);
                //var response = await httpclient.PostAsync(accessTokensUrl.ToString(), content);
                //var data = await response.Content.ReadAsStringAsync();
                //var githubResponse = JsonConvert.DeserializeObject<GithubAuthResponse>(data);

                var header = new ProductHeaderValue("Test", "0.0.1");
                var ghclient = new GitHubClient(header)
                {
                    Credentials = new Credentials(jwt, AuthenticationType.Bearer)
                };

                //var installations = await client.GitHubApps.GetAllInstallationsForCurrent();
                //foreach (var i in installations)
                //{
                //    log.LogInformation($"installation: {i.Id} {i.HtmlUrl}");
                //}

                var response = await ghclient.GitHubApps.CreateInstallationToken(installationId);
                var repoClient = new GitHubClient(header)
                {
                    Credentials = new Credentials(response.Token)
                };

                var repositoryId = msg["repository"]["id"].Value<long>();
                var issueNumber = msg["issue"]["number"].Value<int>();
                await repoClient.Issue.Comment.Create(repositoryId, issueNumber, "Thanks for creating an issue");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        smeeClient.Stop();
        return Task.CompletedTask;
    }

    private string GetJwt(string appId, string privateKeyPath)
    {
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
        {
            alg = "RS256",
            typ = "JWT"
        }))).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
        {
            iat = DateTimeOffset.UtcNow.AddSeconds(-10).ToUnixTimeSeconds(),
            exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
            iss = appId,
        })));

        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(privateKeyPath));

        var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes($"{header}.{payload}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var jwt = $"{header}.{payload}.{signature}";
        return jwt;
    }
}

class SmeeConnectedClient
{
    public string AppId { get; set; }
    public string AccessTokensUrl { get; set; }
    public long InstallationId { get; set; }
}

class GithubPermissions
{
    public string Issues { get; set; }
    public string Metadata { get; set; }
}

class GithubAuthResponse
{
    public string? Token { get; set; }
    [JsonProperty("expires_at")]
    public DateTime ExpiresAt { get; set; }
    public GithubPermissions? Permissions { get; set; }
    [JsonProperty("repository_selection")]
    public string? RepositorySelection { get; set; }
}