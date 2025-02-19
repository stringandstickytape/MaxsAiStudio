using Microsoft.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace AiStudio4.Controls
{
    public class WebServer
    {
        private WebApplication app;
        private readonly IConfiguration _configuration;
        private readonly UiRequestBroker _uiRequestBroker;
        private readonly FileServer _fileServer;
        private readonly WebSocketServer _wsServer;

        public WebServer(IConfiguration configuration, UiRequestBroker uiRequestBroker)
        {
            _configuration = configuration;
            _uiRequestBroker = uiRequestBroker;
            _fileServer = new FileServer(Path.Combine(Directory.GetCurrentDirectory(), "AiStudio4.Web", "dist"));
            _wsServer = new WebSocketServer();
        }

        public async Task StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            var port = _configuration.GetValue<int>("WebServer:Port", 35005);
            builder.WebHost.UseUrls($"http://*:{port}");

            app = builder.Build();
            app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) });

            ConfigureRoutes();

            await app.RunAsync();
        }

        private void ConfigureRoutes()
        {
            // Root path
            app.MapGet("/", async context => await _fileServer.ServeFile(context, "index.html"));

            // API requests
            app.MapPost("/api/{requestType}", HandleApiRequest);

            // Static files
            app.MapGet("/{*path}", _fileServer.HandleFileRequest);

            // WebSocket
            app.Map("/ws", _wsServer.HandleWebSocketRequest);
        }

        private async Task HandleApiRequest(HttpContext context)
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