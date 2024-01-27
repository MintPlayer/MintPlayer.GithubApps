using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Octokit;
using System.Security.Cryptography;
using System.Text;

namespace MintPlayer.AspNetCore.BotFramework.Services;

internal class AuthenticatedGithubService : Abstractions.IAuthenticatedGithubService
{
    #region Constructor
    private readonly IOptions<BotOptions> botOptions;
    public AuthenticatedGithubService(IOptions<BotOptions> botOptions)
    {
        this.botOptions = botOptions;
    }
    #endregion
        
    public async Task<IGitHubClient> GetAuthenticatedGithubClient(long installationId)
    {
        var jwt = GetJwt(botOptions.Value.AppId!, botOptions.Value.PrivateKeyPath!);

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
