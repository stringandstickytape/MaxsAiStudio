using Microsoft.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace AiStudio4.InjectedDependencies
{
    public class WebServer
    {
        private WebApplication app;
        private readonly IConfiguration _configuration;
        private readonly UiRequestBroker _uiRequestBroker;
        private readonly FileServer _fileServer;
        private readonly WebSocketServer _wsServer;

        public WebServer(IConfiguration configuration, UiRequestBroker uiRequestBroker, FileServer fileServer, WebSocketServer wsServer)
        {
            _configuration = configuration;
            _uiRequestBroker = uiRequestBroker;
            _fileServer = fileServer;
            _wsServer = wsServer;
        }

        //public async Task StartAsync()
        //{
        //    var builder = WebApplication.CreateBuilder();
        //    var port = _configuration.GetValue("WebServer:Port", 35005);
        //    builder.WebHost.UseUrls($"http://*:{port}");
        //
        //    app = builder.Build();
        //    app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) });
        //
        //    ConfigureRoutes();
        //
        //    await app.RunAsync();
        //}

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            var port = _configuration.GetValue("WebServer:Port", 35005);

            // Configure Kestrel for HTTPS
            builder.WebHost.UseKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    // Load the certificate from the file
                    string certPath = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\aistudio4.pfx";

                    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    string thumbprint = "53C6156992D3C796B2B13A9C0B8DCD26508C0BF5";
                    var cert = store.Certificates
                        .Find(X509FindType.FindByThumbprint, thumbprint, false)[0]; //YourStrongPassword

                    listenOptions.UseHttps(cert);
                });
            });

            app = builder.Build();
            app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) });

            ConfigureRoutes();

            await app.RunAsync();
        }

        private void ConfigureRoutes()
        {
            // Root path
            app.MapGet("/", _fileServer.HandleFileRequest);

            // API requests
            app.MapPost("/api/{requestType}", HandleApiRequest);
            app.MapPost("/api/{requestType}/{action}", HandleApiRequest);

            // Static files
            app.MapGet("/{*path}", _fileServer.HandleFileRequest);

            // WebSocket
            app.Map("/ws", _wsServer.HandleWebSocketRequest);
        }

        private async Task HandleApiRequest(HttpContext context)
        {
            try
            {
                var clientId = context.Request.Headers["X-Client-Id"].ToString();

                // Get the request type from the route values
                var requestType = context.Request.RouteValues["requestType"]?.ToString();

                // Check if there's an action specified (for nested routes like pinnedCommands/get)
                var action = context.Request.RouteValues["action"]?.ToString();

                // Combine requestType and action if both are present
                if (!string.IsNullOrEmpty(action))
                {
                    requestType = $"{requestType}/{action}";
                }

                using var reader = new StreamReader(context.Request.Body);
                var requestData = await reader.ReadToEndAsync();

                var response = await _uiRequestBroker.HandleRequestAsync(clientId, requestType, requestData);
                context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(response);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Error handling request: {ex.Message}");
            }
        }

        public Task SendToAllClientsAsync(string message) => _wsServer.SendToAllClientsAsync(message);
        public Task SendToClientAsync(string clientId, string message) => _wsServer.SendToClientAsync(clientId, message);
        public async Task StopAsync()
        {
            if (app != null)
            {
                await _wsServer.StopAsync();
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}