using AiStudio4.Resources;
using Microsoft.Web.WebView2.Wpf;
using System.Windows;

namespace AiStudio4.Controls
{
    public class AiStudioWebView2 : WebView2
    {

        public AiStudioWebView2()
        {
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
            this.CoreWebView2.AddHostObjectToScript("windowManager", WindowManager.Instance);

            // Add navigation interceptor
            this.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

            // Navigate to localhost URL
            this.CoreWebView2.Navigate("http://localhost:35002/");
        }

        private void CoreWebView2_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("http://localhost:35002"))
            {
                e.Cancel = true;
                this.CoreWebView2.NavigateToString(WebContent.TEST_HTML);
            }
        }
    }
}