using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace AiStudio4.InjectedDependencies
{
    public class WebSocketServer
    {
        private readonly ConcurrentDictionary<string, WebSocket> _connectedClients = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly IConfiguration _configuration;

        public WebSocketServer(IConfiguration configuration)
        {
            _configuration = configuration;
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

            _connectedClients.TryAdd(clientId, webSocket);

            var json = JsonConvert.SerializeObject(new { messageType = "clientId", content = clientId});

            // Send the client their ID immediately after connection
            var clientIdMessage = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(
                new ArraySegment<byte>(clientIdMessage),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token);

            // Send test generic packet
            await webSocket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { messageType = "message", content = @"# hello
```filetype
hello world!
```
"
                }))),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token);

            try
            {
                await HandleWebSocketConnection(clientId, webSocket);
            }
            finally
            {
                _connectedClients.TryRemove(clientId, out _);
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        "Connection closed by server",
                        _cancellationTokenSource.Token);
                }
            }
        }

        private async Task HandleWebSocketConnection(string clientId, WebSocket webSocket)
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
                    }
                    else
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Received message from {clientId}: {message}");
                    }
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocket error for client {clientId}: {ex.Message}");
            }
        }

        public async Task SendToAllClientsAsync(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer);

            foreach (var client in _connectedClients)
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
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"Error sending to client {client.Key}: {ex.Message}");
                    _connectedClients.TryRemove(client.Key, out _);
                }
            }
        }

        public async Task SendToClientAsync(string clientId, string message)
        {
            if (_connectedClients.TryGetValue(clientId, out var webSocket))
            {
                try
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(
                            new ArraySegment<byte>(buffer),
                            WebSocketMessageType.Text,
                            true,
                            _cancellationTokenSource.Token);
                    }
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"Error sending to client {clientId}: {ex.Message}");
                    _connectedClients.TryRemove(clientId, out _);
                }
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();

            foreach (var client in _connectedClients)
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
                    Console.WriteLine($"Error closing WebSocket for client {client.Key}: {ex.Message}");
                }
            }

            _connectedClients.Clear();
            _cancellationTokenSource.Dispose();
        }
    }
}