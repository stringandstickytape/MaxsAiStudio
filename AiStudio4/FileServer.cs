using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AiStudio4.Controls
{
    public class FileServer
    {
        private readonly string _webRootPath;

        public FileServer(IConfiguration configuration)
        {
            _webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "AiStudio4.Web", "dist");
        }

        public async Task HandleFileRequest(HttpContext context)
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
        }

        public async Task ServeFile(HttpContext context, string relativePath)
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
            return Path.GetExtension(path).ToLower() switch
            {
                ".js" => "application/javascript",
                ".css" => "text/css",
                ".html" => "text/html",
                ".png" => "image/png",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }
    }
}