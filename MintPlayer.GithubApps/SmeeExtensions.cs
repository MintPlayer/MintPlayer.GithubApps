using Smee.IO.Client.Dto;
using System.Text.Json;

namespace MintPlayer.GithubApps;

public static class SmeeExtensions
{
    /// <summary>Correctly reads the webhook from the smee channel.</summary>
    public static async Task<string> GetFormattedJsonAsync(this SmeeData data)
    {
        // Format JSON correctly
        var rawBody = data.Body.ToString();
        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);
        JsonDocument.Parse(rawBody!).WriteTo(writer);

        await writer.FlushAsync();
        ms.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(ms);

        var jsonFormatted = await reader.ReadToEndAsync();
        return jsonFormatted;
    }
}
