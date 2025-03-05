using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SharedClasses.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VSIXTest.UI
{
    public class VsixWebViewManager
    {
        private readonly WebView2 _webView;
        private readonly ButtonManager _buttonManager;
        private bool _webViewInitialized = false;

        public VsixWebViewManager(WebView2 webView, ButtonManager buttonManager)
        {
            _webView = webView;
            _buttonManager = buttonManager;
        }

        public async Task InitializeAsync()
        {
            if (_webViewInitialized) return;

            var env = await CoreWebView2Environment.CreateAsync(null, "C:\\temp");
            if (_webView.CoreWebView2 == null)
            {
                await _webView.EnsureCoreWebView2Async(env);
            }

            _webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            _webView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;


            if(VsixChat.NewUi)
            {
                _webView.Source = new Uri("https://localhost:35005");
            }
            else
            {
                foreach (var resource in AssemblyHelper.GetResourceDetails())
                {
                    _webView.CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
                }

                _webView.NavigateToString(AssemblyHelper.GetEmbeddedResource("SharedClasses", "SharedClasses.HTML.ChatWebView2.html"));

                string[] scriptResources = new[]
                {
                    "SharedClasses.JavaScriptViewers.JsonViewer.js",
                    "SharedClasses.JavaScriptViewers.ThemeEditor.js",
                    "SharedClasses.JavaScriptViewers.SvgViewer.js",
                    "SharedClasses.JavaScriptViewers.MermaidViewer.js",
                    "SharedClasses.JavaScriptViewers.DotViewer.js",
                    "SharedClasses.JavaScriptViewers.FindAndReplacer.js"
                };

                foreach (var resource in scriptResources)
                {
                    await _webView.ExecuteScriptAsync(AssemblyHelper.GetEmbeddedResource("SharedClasses", resource));
                }
            }
                
            _webViewInitialized = true;
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(_buttonManager.GenerateButtonScript());
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var rd = AssemblyHelper.GetResourceDetails();
            var matching = rd.Where(x => e.Request.Uri == x.Uri).ToList();

            AssemblyHelper.GetResourceDetails()
                .Where(x => e.Request.Uri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(x =>
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    if (!assembly.GetManifestResourceNames().Contains(x.ResourceName))
                    {
                        assembly = Assembly.Load("SharedClasses");
                    }

                    using (Stream stream = assembly.GetManifestResourceStream(x.ResourceName))
                    {
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                                var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(memoryStream, 200, "OK", $"Content-Type: {x.MimeType}");
                                e.Response = response;
                                e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", "*");
                                return;
                            }
                        }
                        throw new Exception("Probably forgot to embed the resource :(");
                    }
                });
        }

        public async Task AddContextMenuItemAsync(string label, string messageType)
        {
            string script = $@"
                window.addCustomContextMenuItem({{
                    label: `{label}`,
                    onClick: () => window.chrome.webview.postMessage({{
                        type: `{messageType}`
                    }})
                }});
            ";
            await _webView.ExecuteScriptAsync(script);
        }
    }
}