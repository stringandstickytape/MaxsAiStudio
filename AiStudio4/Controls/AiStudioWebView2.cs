using Microsoft.Web.WebView2.Wpf;

using SharedClasses.Helpers;

using Microsoft.Extensions.DependencyInjection;


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
                UserDataFolder = PathHelper.ProfileRootPath
            };

            this.WebMessageReceived += AiStudioWebView2_WebMessageReceived;
        }

        private void AiStudioWebView2_WebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Placeholder
        }

        public async void Initialize(bool allowConnectionsOutsideLocalhost)
        {
            await this.EnsureCoreWebView2Async();
            this.CoreWebView2.AddHostObjectToScript("windowManager", _windowManager);

            // Add handlers
            //this.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            // Register for resource handling
            //this.CoreWebView2.AddWebResourceRequestedFilter("*", Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);

            if (allowConnectionsOutsideLocalhost)
            {
                this.CoreWebView2.Navigate("https://localhost:35005/");
            }
            else this.CoreWebView2.Navigate("http://localhost:35005/");
        }
    }
}
