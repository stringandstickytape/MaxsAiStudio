using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies.WebSocketManagement;
using AiStudio4.InjectedDependencies.WebSocket;

namespace AiStudio4.InjectedDependencies
{
    public class WebSocketServer
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly WebSocketMessageHandler _messageHandler;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebSocketServer> _logger;

        public WebSocketServer(
            WebSocketConnectionManager connectionManager,
            WebSocketMessageHandler messageHandler,
            IConfiguration configuration,
            ILogger<WebSocketServer> logger)
        {
            _connectionManager = connectionManager;
            _messageHandler = messageHandler;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task HandleWebSocketRequest(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var clientId = Guid.NewGuid().ToString();

            _connectionManager.AddConnection(clientId, webSocket);

            var json = JsonConvert.SerializeObject(new { messageType = "clientId", content = clientId});
            await _messageHandler.SendToClientAsync(clientId, json);

            try
            {
                await _messageHandler.HandleIncomingMessages(clientId, webSocket);
            }
            finally
            {
                _connectionManager.RemoveConnection(clientId);
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed by server",
                        CancellationToken.None);
                }
            }
        }

        public string GetClientIdFromWebSocket(System.Net.WebSockets.WebSocket socket)
        {
            return _connectionManager.GetClientId(socket);
        }

        public Task SendToAllClientsAsync(string message)
        {
            return _messageHandler.SendToAllClientsAsync(message);
        }

        public Task SendToClientAsync(string clientId, string message)
        {
            return _messageHandler.SendToClientAsync(clientId, message);
        }

        public async Task StopAsync()
        {
            foreach (var client in _connectionManager.GetAllConnections())
            {
                try
                {
                    if (client.Value.State == WebSocketState.Open)
                    {
                        await client.Value.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Server shutting down",
                            CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing WebSocket for client {ClientId}", client.Key);
                }
            }

            _connectionManager.Clear();
            _messageHandler.Dispose();
        }
    }
}