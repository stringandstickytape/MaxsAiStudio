using Microsoft.Web.WebView2.Wpf;
using System.Windows;
using SharedClasses.Helpers;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.Controls
{
    public class AiStudioWebView2 : WebView2
    {
        private readonly WindowManager _windowManager;

        public AiStudioWebView2()
        {
            // Get the WindowManager from the service provider
            _windowManager = ((App)Application.Current).Services.GetRequiredService<WindowManager>();

            this.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            this.CreationProperties = new Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties
            {
                UserDataFolder = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "AiStudio4")
            };
        }

        public async void Initialize()
        {
            await this.EnsureCoreWebView2Async();
            this.CoreWebView2.AddHostObjectToScript("windowManager", _windowManager);

            // Add handlers
            this.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            // Register for resource handling
            this.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);

            // Navigate to localhost URL
            this.CoreWebView2.Navigate("http://localhost:35002/");
        }
        private void CoreWebView2_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var uri = new Uri(e.Request.Uri);

            System.Diagnostics.Debug.WriteLine($"Requested URI: {e.Request.Uri}");

            string resourcePath;
            if (uri.AbsolutePath == "/" && uri.Authority == "localhost:35002")
            {
                resourcePath = "AiStudio4.AiStudio4.Web.dist.index.html";
            }
            else
            {
                resourcePath = $"AiStudio4.AiStudio4.Web.dist{uri.AbsolutePath.Replace("/",".")}";
            }

            System.Diagnostics.Debug.WriteLine($"Looking for resource: {resourcePath}");

            // Create a MemoryStream that will stay alive after this method returns
            var memoryStream = new MemoryStream();
            var res = assembly.GetManifestResourceNames();
            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Resource not found: {resourcePath}");
                    return;
                }

                // Copy the content to the MemoryStream
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
            }

            var response = this.CoreWebView2.Environment.CreateWebResourceResponse(
                memoryStream,
                200,
                "OK",
                $"Content-Type: {GetContentType(resourcePath)}\n" +
                "Access-Control-Allow-Origin: *");
            e.Response = response;

            // The WebView2 will dispose of the MemoryStream when it's done with it
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
    }
}