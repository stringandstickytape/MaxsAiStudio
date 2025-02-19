using Microsoft.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;

namespace AiStudio4.Controls
{
    public class WebServer
    {
        private WebApplication app;
        private readonly IConfiguration _configuration;
        private readonly string _webRootPath;
        private readonly UiRequestBroker _uiRequestBroker;
        private readonly ConcurrentDictionary<string, WebSocket> _connectedClients = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public WebServer(IConfiguration configuration, UiRequestBroker uiRequestBroker)
        {
            _configuration = configuration;
            _uiRequestBroker = uiRequestBroker;
            _webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "AiStudio4.Web", "dist");
        }

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();

            // Get port from configuration, with fallback
            var port = _configuration.GetValue<int>("WebServer:Port", 35005);
            builder.WebHost.UseUrls($"http://localhost:{port}");

            app = builder.Build();

            // Just use the WebSockets middleware directly
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });


            // Handle root path
            app.MapGet("/", async context =>
            {
                await ServeFile(context, "index.html");
            });

            // Handle API requests
            app.MapPost("/api/{requestType}", async context =>
            {
                try
                {
                    var requestType = context.Request.RouteValues["requestType"]?.ToString();
                    using var reader = new StreamReader(context.Request.Body);
                    var requestData = await reader.ReadToEndAsync();

                    var response = await _uiRequestBroker.HandleRequestAsync(requestType, requestData);
                    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(response);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync($"Error handling request: {ex.Message}");
                }
            });

            // Handle file requests
            app.MapGet("/{*path}", async context =>
            {
                var path = context.Request.Path.Value?.TrimStart('/');
                if (string.IsNullOrEmpty(path))
                {
                    await ServeFile(context, "index.html");
                }
                else
                {
                    await ServeFile(context, path);
                }
            });

            // Handle WebSocket connections
            app.Map("/ws", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var clientId = Guid.NewGuid().ToString();

                    _connectedClients.TryAdd(clientId, webSocket);

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
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            });
            
            await app.RunAsync();
        }

        private async Task ServeFile(HttpContext context, string relativePath)
        {
            var fullPath = Path.Combine(_webRootPath, relativePath);

            if (!File.Exists(fullPath))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File not found: {relativePath}");
                return;
            }

            try
            {
                context.Response.ContentType = GetContentType(relativePath);
                context.Response.Headers.Append("Access-Control-Allow-Origin", "*");

                // Read and send the file
                await using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                await fileStream.CopyToAsync(context.Response.Body);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Error serving file: {ex.Message}");
            }
        }

        private string GetContentType(string path)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".js": return "application/javascript";
                case ".css": return "text/css";
                case ".html": return "text/html";
                case ".png": return "image/png";
                case ".svg": return "image/svg+xml";
                default: return "application/octet-stream";
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
                        // Handle received message if needed
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
            if (app != null)
            {
                _cancellationTokenSource.Cancel();

                // Close all WebSocket connections
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
                await app.StopAsync();
                await app.DisposeAsync();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}