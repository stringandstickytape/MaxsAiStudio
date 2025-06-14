using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

using System.Net.Http;

namespace AiStudio4.InjectedDependencies
{
    public class FileServer
    {
        private readonly string _webRootPath;
        private readonly HttpClient _httpClient;
        private const string DevServerUrl = "http://localhost:5173";

        public FileServer(IConfiguration configuration)
        {
            _webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "AiStudioClient", "dist");
            _httpClient = new HttpClient();
        }

        public async Task HandleFileRequest(HttpContext context)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                await ForwardToDevServer(context);
            }
            else
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
        }

        private async Task ForwardToDevServer(HttpContext context)
        {
            try
            {
                var targetUri = new Uri($"{DevServerUrl}{context.Request.Path}{context.Request.QueryString}");
                var response = await _httpClient.GetAsync(targetUri);

                context.Response.StatusCode = (int)response.StatusCode;

                foreach (var header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in response.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                await response.Content.CopyToAsync(context.Response.Body);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync($"Error forwarding to dev server: {ex.Message}");
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
