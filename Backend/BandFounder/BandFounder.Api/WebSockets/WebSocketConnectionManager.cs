using System.Net.WebSockets;
using System.Text;

namespace BandFounder.Api.WebSockets;

public class WebSocketConnectionManager
{
    private readonly Dictionary<Guid, List<WebSocket>> _connections = new();

    public IEnumerable<WebSocket> GetConnections(Guid chatRoomId)
    {
        return _connections.TryGetValue(chatRoomId, out var connection) ? connection : Enumerable.Empty<WebSocket>();
    }

    public async Task HandleWebSocketConnectionAsync(HttpContext context, WebSocket webSocket)
    {
        // Extract chat room ID from query string
        var chatRoomId = Guid.Parse(context.Request.Query["chatRoomId"]!);
        AddConnection(chatRoomId, webSocket);

        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received in chat room {chatRoomId}: {receivedMessage}");

                // Broadcast message to all connections in the chat room
                var messagePayload = Encoding.UTF8.GetBytes(receivedMessage);
                foreach (var socket in GetConnections(chatRoomId))
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(new ArraySegment<byte>(messagePayload),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            else if (result is { MessageType: WebSocketMessageType.Close, CloseStatus: not null })
            {
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                RemoveConnection(chatRoomId, webSocket);
            }
        }
    }

    private void AddConnection(Guid chatRoomId, WebSocket webSocket)
    {
        if (!_connections.TryGetValue(chatRoomId, out var value))
        {
            value = [];
            _connections[chatRoomId] = value;
        }

        value.Add(webSocket);
    }

    private void RemoveConnection(Guid chatRoomId, WebSocket webSocket)
    {
        if (_connections.TryGetValue(chatRoomId, out var connection))
        {
            connection.Remove(webSocket);
        }
    }
}