using Newtonsoft.Json;
using Smee.IO.Client.Dto;
using System.Text.Json;

namespace MintPlayer.GithubApps;

public static class SmeeExtensions
{
    /// <summary>Correctly reads the webhook from the smee channel.</summary>
    public static string GetFormattedJson(this SmeeData data)
    {
        // Format JSON correctly
        var minified = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(data.Body.ToString()));
        return minified;
    }
}
