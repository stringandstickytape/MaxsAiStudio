using System.Net.WebSockets;
using System.Text;
using AiStudio4.InjectedDependencies.WebSocketManagement;
using Microsoft.Extensions.Logging;

namespace AiStudio4.InjectedDependencies.WebSocket
{
    public class WebSocketMessageHandler
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly ILogger<WebSocketMessageHandler> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public WebSocketMessageHandler(
            WebSocketConnectionManager connectionManager,
            ILogger<WebSocketMessageHandler> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task SendToClientAsync(string clientId, string message)
        {
            try
            {
                var socket = _connectionManager.GetSocket(clientId);
                if (socket?.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource.Token);
                    _logger.LogTrace("Sent message to client {ClientId}", clientId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to client {ClientId}", clientId);
                _connectionManager.RemoveConnection(clientId);
            }
        }

        public async Task SendToAllClientsAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer);

            foreach (var client in _connectionManager.GetAllConnections())
            {
                try
                {
                    if (client.Value.State == WebSocketState.Open)
                    {
                        await client.Value.SendAsync(
                            arraySegment,
                            WebSocketMessageType.Text,
                            true,
                            _cancellationTokenSource.Token);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to client {ClientId}", client.Key);
                    _connectionManager.RemoveConnection(client.Key);
                }
            }
        }

        public async Task HandleIncomingMessages(string clientId, System.Net.WebSockets.WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed by client",
                            _cancellationTokenSource.Token);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogTrace("Received message from client {ClientId}: {Message}", clientId, message);
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error for client {ClientId}", clientId);
            }
            finally
            {
                _connectionManager.RemoveConnection(clientId);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}