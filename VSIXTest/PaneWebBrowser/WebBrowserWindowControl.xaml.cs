using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Wpf;

namespace VSIXTest.PaneWebBrowser
{
    public partial class WebBrowserWindowControl : UserControl
    {
        // Public properties to access the WebView2 and address bar from the pane
        public Microsoft.Web.WebView2.Wpf.WebView2 WebBrowser { get; private set; }
        public TextBox AddressBar { get; private set; }

        public WebBrowserWindowControl()
        {
            InitializeComponent();

            // Get references to the controls
            WebBrowser = webView;
            AddressBar = addressBar;

            // Set up the Go button click event
            goButton.Click += GoButton_Click;

            // Set up keyboard handling for the address bar
            addressBar.KeyDown += AddressBar_KeyDown;
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(addressBar.Text);
        }

        private void AddressBar_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                NavigateToUrl(addressBar.Text);
                e.Handled = true;
            }
        }

        private void NavigateToUrl(string url)
        {
            // Ensure URL has protocol prefix
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            try
            {
                if (WebBrowser.CoreWebView2 != null)
                {
                    WebBrowser.CoreWebView2.Navigate(url);
                }
                else
                {
                    // Store the URL to navigate to once CoreWebView2 is initialized
                    WebBrowser.Source = new Uri(url);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to URL: {ex.Message}");
            }
        }
    }
}