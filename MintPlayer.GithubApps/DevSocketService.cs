﻿using Microsoft.Extensions.Primitives;
using System.Net.WebSockets;
using System.Text;

namespace MintPlayer.GithubApps;

public interface IDevSocketService
{
    Task NewSocketClient(SocketClient client);
    Task SendToClients(IDictionary<string, StringValues> headers, string body);
}

public class DevSocketService : IDevSocketService
{
    private readonly List<SocketClient> clients = new List<SocketClient>();
    public DevSocketService()
    {
    }

    public Task NewSocketClient(SocketClient client)
    {
        clients.Add(client);
        // TODO: On close remove from list

        return Task.CompletedTask;
    }

    public async Task SendToClients(IDictionary<string, StringValues> headers, string body)
    {
        var payload = $"""
            {string.Join(Environment.NewLine, headers.Select(h => $"{h.Key}: {h.Value}"))}

            {body}
            """;
        var bytes = Encoding.UTF8.GetBytes(payload);
        var arraySegment = new ArraySegment<byte>(bytes, 0, bytes.Length);

        foreach (var client in clients.Where(c => c.WebSocket.State == WebSocketState.Open))
        {
            await client.WebSocket.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}

public class SocketClient
{
    public SocketClient(WebSocket webSocket) => WebSocket = webSocket;

    public WebSocket WebSocket { get; }
}