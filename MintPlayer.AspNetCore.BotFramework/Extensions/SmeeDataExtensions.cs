using Newtonsoft.Json;
using Smee.IO.Client.Dto;

namespace MintPlayer.AspNetCore.BotFramework;

public static class SmeeDataExtensions
{
    /// <summary>Correctly reads the webhook from the smee channel.</summary>
    public static string GetFormattedJson(this SmeeData data)
    {
        // Format JSON correctly
        var minified = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(data.Body.ToString() ?? throw new Exception("Smee body cannot be empty")));
        return minified;
    }
}
