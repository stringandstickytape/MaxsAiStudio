using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;

namespace AiStudio4.InjectedDependencies.WebSocketManagement
{
    public class WebSocketConnectionManager
    {
        private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _connectedClients = new();
        private readonly ConcurrentDictionary<System.Net.WebSockets.WebSocket, string> _socketToClientId = new();
        private readonly ILogger<WebSocketConnectionManager> _logger;

        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
        {
            _logger = logger;
        }

        public void AddConnection(string clientId, System.Net.WebSockets.WebSocket webSocket)
        {
            _connectedClients.TryAdd(clientId, webSocket);
            _socketToClientId.TryAdd(webSocket, clientId);
            _logger.LogInformation("Added new WebSocket connection for client {ClientId}", clientId);
        }

        public void RemoveConnection(string clientId)
        {
            if (_connectedClients.TryRemove(clientId, out var socket))
            {
                _socketToClientId.TryRemove(socket, out _);
                _logger.LogInformation("Removed WebSocket connection for client {ClientId}", clientId);
            }
        }

        public string GetClientId(System.Net.WebSockets.WebSocket socket)
        {
            _socketToClientId.TryGetValue(socket, out var clientId);
            return clientId;
        }

        public System.Net.WebSockets.WebSocket GetSocket(string clientId)
        {
            _connectedClients.TryGetValue(clientId, out var socket);
            return socket;
        }

        public IEnumerable<KeyValuePair<string, System.Net.WebSockets.WebSocket>> GetAllConnections()
        {
            return _connectedClients;
        }

        public void Clear()
        {
            _connectedClients.Clear();
            _socketToClientId.Clear();
            _logger.LogInformation("Cleared all WebSocket connections");
        }
    }
}