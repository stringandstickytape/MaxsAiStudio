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
        private readonly string _webRootPath;
        private readonly UiRequestBroker _uiRequestBroker;

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

        public async Task StopAsync()
        {
            if (app != null)
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}