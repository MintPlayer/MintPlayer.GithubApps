using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using Smee.IO.Client;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MintPlayer.AspNetCore.BotFramework;

namespace MintPlayer.GithubApps;

public class SmeeService : IHostedService
{
    private readonly ISmeeClient smeeClient;
    private readonly IConfiguration configuration;
    private readonly IServiceProvider serviceProvider;
    private readonly ISignatureService signatureService;
    public SmeeService(ISmeeClient smeeClient, IConfiguration configuration, IServiceProvider serviceProvider, ISignatureService signatureService)
    {
        this.smeeClient = smeeClient;
        this.configuration = configuration;
        this.serviceProvider = serviceProvider;
        this.signatureService = signatureService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        smeeClient.OnMessage += SmeeClient_OnMessage;
        await smeeClient.StartAsync(cancellationToken);
    }

    private async void SmeeClient_OnMessage(object? sender, Smee.IO.Client.Dto.SmeeEvent e)
    {
        if (e.Event == SmeeEventType.Message)
        {
            var jsonFormatted = e.Data.GetFormattedJson();
            var signatureSha256 = e.Data.Headers["x-hub-signature-256"];
            var secret = configuration["GithubApp:WebhookSecret"];
            if (!signatureService.VerifySignature(signatureSha256, secret, jsonFormatted))
            {
                return;
            }

            using (var scope = serviceProvider.CreateScope())
            {
                var msgBody = (JObject)e.Data.Body;
                var githubService = scope.ServiceProvider.GetRequiredService<IGithubService>();
                await githubService.OnMessage(msgBody);

                var installationId = msgBody["installation"]["id"].Value<long>();
                var repoClient = await GetAuthenticatedGithubClient(installationId);

                var action = msgBody["action"].Value<string>();
                switch (action)
                {
                    case "created":
                        var accessTokensUrl = msgBody["installation"]["access_tokens_url"];
                        var appId = msgBody["installation"]["app_id"];
                        break;

                    case "deleted":
                        break;

                    case "opened":
                        //var accessTokensUrl = msg["installation"]["access_tokens_url"];
                        //var appId = msg["installation"]["app_id"];

                        var repositoryId = msgBody["repository"]["id"].Value<long>();
                        var issueNumber = msgBody["issue"]["number"].Value<int>();
                        await githubService.OnIssueOpened(repositoryId, issueNumber, repoClient, msgBody);
                        break;
                }
            }
        }
    }

    private async Task<GitHubClient> GetAuthenticatedGithubClient(long installationId)
    {
        var jwt = GetJwt(configuration["GithubApp:AppId"]!, configuration["GithubApp:PrivateKeyPath"]!);

        var header = new ProductHeaderValue("Test", "0.0.1");
        var ghclient = new GitHubClient(header)
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };

        var response = await ghclient.GitHubApps.CreateInstallationToken(installationId);
        var repoClient = new GitHubClient(header)
        {
            Credentials = new Credentials(response.Token)
        };
        return repoClient;
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
