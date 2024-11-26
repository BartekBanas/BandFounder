using System.Net.WebSockets;

namespace BandFounder.Api.WebSockets;

public class WebSocketConnectionManager
{
    private readonly Dictionary<Guid, List<WebSocket>> _connections = new();

    public void AddConnection(Guid chatRoomId, WebSocket webSocket)
    {
        if (!_connections.ContainsKey(chatRoomId))
        {
            _connections[chatRoomId] = new List<WebSocket>();
        }
        _connections[chatRoomId].Add(webSocket);
    }

    public IEnumerable<WebSocket> GetConnections(Guid chatRoomId)
    {
        return _connections.ContainsKey(chatRoomId) ? _connections[chatRoomId] : Enumerable.Empty<WebSocket>();
    }

    public void RemoveConnection(Guid chatRoomId, WebSocket webSocket)
    {
        if (_connections.ContainsKey(chatRoomId))
        {
            _connections[chatRoomId].Remove(webSocket);
        }
    }
}
