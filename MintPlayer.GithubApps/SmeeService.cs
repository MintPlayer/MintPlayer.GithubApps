using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using Smee.IO.Client;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MintPlayer.GithubApps;

public class SmeeService : IHostedService
{
    private readonly ISmeeClient smeeClient;
    private readonly IConfiguration configuration;
    private readonly IServiceProvider serviceProvider;
    public SmeeService(ISmeeClient smeeClient, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        this.smeeClient = smeeClient;
        this.configuration = configuration;
        this.serviceProvider = serviceProvider;
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

            var rawBody = e.Data.Body.ToString(); //.Replace("\r", string.Empty).Replace("\n", string.Empty);

            //var trimmed = string.Join(
            //    string.Empty,
            //    rawBody.Split(Environment.NewLine)
            //        .Select(l => l.Trim(' ').Replace(": ", ":"))
            //);

            using var ms = new MemoryStream();
            using var writer = new Utf8JsonWriter(ms);
            JsonDocument.Parse(rawBody!).WriteTo(writer);

            await writer.FlushAsync();
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            var jsonFormatted = await reader.ReadToEndAsync();

            var signatureSha256 = e.Data.Headers["x-hub-signature-256"];
            var secret = configuration["GithubApp:WebhookSecret"];
            if (!string.IsNullOrEmpty(secret))
            {
                var keyBytes = Encoding.UTF8.GetBytes(secret);
                var bodyBytes = Encoding.UTF8.GetBytes(jsonFormatted);

                var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
                var hashHex = Convert.ToHexString(hash);
                var expectedHeader = $"sha256={hashHex.ToLower(CultureInfo.InvariantCulture)}";
                if (signatureSha256.ToString() != expectedHeader)
                {
                    return;
                }
            }

            var msg = (JObject)e.Data.Body;

            var action = msg["action"].Value<string>();
            if (action == "created")
            {
                var accessTokensUrl = msg["installation"]["access_tokens_url"];
                var appId = msg["installation"]["app_id"];
                var installationId = msg["installation"]["id"];
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
