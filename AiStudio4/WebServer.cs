using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

public class WebServer
{
    private WebApplication app;
    private readonly IConfiguration _configuration;

    public WebServer(IConfiguration configuration)
    {
        _configuration = configuration;
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
            await ServeEmbeddedResource(context, "AiStudio4.AiStudio4.Web.dist.index.html");
        });

        // Handle all other paths
        app.MapGet("/{*path}", async context =>
        {
            var path = context.Request.Path.Value?.TrimStart('/');
            var resourcePath = $"AiStudio4.AiStudio4.Web.dist.{path?.Replace("/", ".")}";
            await ServeEmbeddedResource(context, resourcePath);
        });

        await app.RunAsync();
    }

    private async Task ServeEmbeddedResource(HttpContext context, string resourcePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourcePath);

        if (stream == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        context.Response.ContentType = GetContentType(resourcePath);
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        await stream.CopyToAsync(context.Response.Body);
    }

    private string GetContentType(string path)
    {
        // Your existing GetContentType method implementation
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