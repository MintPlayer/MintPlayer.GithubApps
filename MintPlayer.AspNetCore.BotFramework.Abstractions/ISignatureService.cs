namespace MintPlayer.AspNetCore.BotFramework.Abstractions;

public interface ISignatureService
{
    bool VerifySignature(string signature, string? secret, string requestBody);
}
