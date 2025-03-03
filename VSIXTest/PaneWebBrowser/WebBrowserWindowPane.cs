using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;
using System.Windows.Controls;

namespace VSIXTest.PaneWebBrowser
{
    [Guid("743967b7-4ad8-4103-8a28-bf2933a5bdf8")]
    public class WebBrowserWindowPane : ToolWindowPane, IVsWindowFrameNotify3
    {
        private WebView2 _webView;
        private System.Windows.Controls.TextBox _addressBar;

        public WebBrowserWindowPane() : base(null)
        {
            this.Caption = "Max's AI Studio Web Browser";

            // Create a WebBrowserWindowControl and initialize it
            var control = new WebBrowserWindowControl();
            _webView = control.WebBrowser;
            _addressBar = control.AddressBar;

            // Set up event handlers
            _webView.NavigationCompleted += WebView_NavigationCompleted;

            // Set the Content to the control
            this.Content = control;

            // Initialize WebView2 - this needs to happen after the control is loaded
            control.Loaded += Control_Loaded;
        }

        private async void Control_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Initialize WebView2
                var env = await CoreWebView2Environment.CreateAsync(null, "C:\\temp");
                await _webView.EnsureCoreWebView2Async(env);

                // Navigate to a default page
                _webView.Source = new Uri("https://localhost:35005");

                // Set up additional event handlers if needed
                _webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing WebView2: {ex.Message}");
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            // Handle opening links in the same window instead of new windows
            e.Handled = true;
            _webView.CoreWebView2.Navigate(e.Uri);
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Update address bar with the current URL
            if (_addressBar != null && _webView.CoreWebView2 != null)
            {
                _addressBar.Text = _webView.CoreWebView2.Source;
            }
        }

        public int OnShow(int fShow)
        {
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int OnMove(int x, int y, int w, int h) => Microsoft.VisualStudio.VSConstants.S_OK;

        public int OnSize(int x, int y, int w, int h) => Microsoft.VisualStudio.VSConstants.S_OK;

        public int OnDockableChange(int fDockable, int x, int y, int w, int h) => Microsoft.VisualStudio.VSConstants.S_OK;

        public int OnClose(ref uint pgrfSaveOptions) => Microsoft.VisualStudio.VSConstants.S_OK;

        // Helper method to navigate to a URL
        public void Navigate(string url)
        {
            if (_webView?.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Navigate(url);
            }
        }
    }
}