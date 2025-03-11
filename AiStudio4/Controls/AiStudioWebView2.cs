using Microsoft.Web.WebView2.Wpf;
using System.Windows;
using SharedClasses.Helpers;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using AiStudio4.InjectedDependencies;

namespace AiStudio4.Controls
{
    public class AiStudioWebView2 : WebView2
    {
        private readonly WindowManager _windowManager;
        private readonly UiRequestBroker _uiRequestBroker;

        public AiStudioWebView2()
        {
            // Get services from the service provider
            var services = ((App)Application.Current).Services;
            _windowManager = services.GetRequiredService<WindowManager>();
            _uiRequestBroker = services.GetRequiredService<UiRequestBroker>();

            this.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            this.CreationProperties = new Microsoft.Web.WebView2.Wpf.CoreWebView2CreationProperties
            {
                UserDataFolder = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    "AiStudio4")
            };

            this.WebMessageReceived += AiStudioWebView2_WebMessageReceived;
        }

        private void AiStudioWebView2_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string jsonMessage = e.WebMessageAsJson;

                dynamic message = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonMessage);

                if (message != null && message.type != null && message.type.ToString().ToLower() == "exit")
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing WebView message: {ex.Message}");
            }
        }

        public async void Initialize()
        {
            await this.EnsureCoreWebView2Async();
            this.CoreWebView2.AddHostObjectToScript("windowManager", _windowManager);

            // Add handlers
            //this.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            // Register for resource handling
            //this.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);

            this.CoreWebView2.Navigate("https://localhost:35005/");
        }
        //private async void CoreWebView2_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        //{
        //    var uri = new Uri(e.Request.Uri);
        //    System.Diagnostics.Debug.WriteLine($"Requested URI: {e.Request.Uri}");
        //
        //    try
        //    {
        //        // Check if this is an API request
        //        if (uri.AbsolutePath.StartsWith("/api/"))
        //        {
        //            var requestType = uri.AbsolutePath.Split('/').Last();
        //            string requestData = "";
        //
        //            // Read request body if POST
        //            if (e.Request.Method == "POST")
        //            {
        //                var content = e.Request.Content;
        //                if (content != null)
        //                {
        //                    using (var stream = content)
        //                    using (var reader = new StreamReader(stream))
        //                    {
        //                        requestData = reader.ReadToEnd();
        //                    }
        //                }
        //            }
        //
        //            var response = await _uiRequestBroker.HandleRequestAsync(requestType, requestData);
        //            var bytes2 = System.Text.Encoding.UTF8.GetBytes(response);
        //            var memoryStream2 = new MemoryStream(bytes2);
        //
        //            e.Response = this.CoreWebView2.Environment.CreateWebResourceResponse(
        //                memoryStream2,
        //                200,
        //                "OK",
        //                "Content-Type: application/json\nAccess-Control-Allow-Origin: *");
        //            return;
        //        }
        //
        //        // Handle static files
        //        string filePath = "./AiStudio4.Web/dist" + (uri.AbsolutePath == "/" ? "/index.html" : uri.AbsolutePath);
        //        string text = File.ReadAllText(filePath);
        //        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
        //        var memoryStream = new MemoryStream(bytes);
        //
        //        e.Response = this.CoreWebView2.Environment.CreateWebResourceResponse(
        //            memoryStream,
        //            200,
        //            "OK",
        //            $"Content-Type: {GetContentType(filePath)}\nAccess-Control-Allow-Origin: *");
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error handling request: {ex.Message}");
        //    }
        //}

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