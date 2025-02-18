using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Reflection;

public class WebServer
{
    private WebApplication app;

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:35005");

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