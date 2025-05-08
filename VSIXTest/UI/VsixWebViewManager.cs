using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SharedClasses.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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


                _webView.Source = new Uri("https://localhost:35005");

                
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

        public async Task SendEndAsync(bool isCtrlPressed, bool isShiftPressed, bool isAltPressed)
        {
            string script = $@"
(function() {{
    const activeElement = document.activeElement;
    
    // Create and dispatch keydown event
    const keyEvent = new KeyboardEvent('keydown', {{
        key: 'End',
        code: 'End',
        keyCode: 35,
        which: 35,
        bubbles: true,
        cancelable: true,
        ctrlKey: {isCtrlPressed.ToString().ToLower()},
        shiftKey: {isShiftPressed.ToString().ToLower()},
        altKey: {isAltPressed.ToString().ToLower()}
    }});
    
    if (activeElement) {{
        activeElement.dispatchEvent(keyEvent);
    }} else {{
        document.dispatchEvent(keyEvent);
    }}
    
    // If it's an input element, handle cursor positioning based on modifiers
    if (activeElement && (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA')) {{
        if (activeElement.tagName === 'INPUT') {{
            // For input elements, End always goes to the end of the input value
            if ({isShiftPressed.ToString().ToLower()}) {{
                // When shift is pressed, select from current position to the end
                const currentPos = activeElement.selectionStart;
                activeElement.selectionStart = currentPos;
                activeElement.selectionEnd = activeElement.value.length;
            }} else {{
                // Without shift, just move cursor to end
                activeElement.selectionStart = activeElement.value.length;
                activeElement.selectionEnd = activeElement.value.length;
            }}
        }} else if (activeElement.tagName === 'TEXTAREA') {{
            if ({isCtrlPressed.ToString().ToLower()}) {{
                // Ctrl+End goes to the end of the entire textarea
                if ({isShiftPressed.ToString().ToLower()}) {{
                    // Select from current position to the end of the document
                    const currentPos = activeElement.selectionStart;
                    activeElement.selectionStart = currentPos;
                    activeElement.selectionEnd = activeElement.value.length;
                }} else {{
                    // Move to end of document
                    activeElement.selectionStart = activeElement.value.length;
                    activeElement.selectionEnd = activeElement.value.length;
                }}
            }} else {{
                // Regular End key behavior in textarea (end of current line)
                // This is more complex as we need to find the end of the current line
                const value = activeElement.value;
                const currentPos = activeElement.selectionStart;
                
                // Find the next newline character after current position
                let endOfLinePos = value.indexOf('\n', currentPos);
                if (endOfLinePos === -1) {{
                    // If no newline found, go to the end of the text
                    endOfLinePos = value.length;
                }}
                
                if ({isShiftPressed.ToString().ToLower()}) {{
                    // Select from current position to end of line
                    activeElement.selectionStart = currentPos;
                    activeElement.selectionEnd = endOfLinePos;
                }} else {{
                    // Move cursor to end of line
                    activeElement.selectionStart = endOfLinePos;
                    activeElement.selectionEnd = endOfLinePos;
                }}
            }}
        }}
    }}
}})();";

            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        public async Task SendHomeAsync(bool isCtrlPressed, bool isShiftPressed, bool isAltPressed)
        {
            string script = $@"
(function() {{
    const activeElement = document.activeElement;
    
    // Create and dispatch keydown event
    const keyEvent = new KeyboardEvent('keydown', {{
        key: 'Home',
        code: 'Home',
        keyCode: 36,
        which: 36,
        bubbles: true,
        cancelable: true,
        ctrlKey: {isCtrlPressed.ToString().ToLower()},
        shiftKey: {isShiftPressed.ToString().ToLower()},
        altKey: {isAltPressed.ToString().ToLower()}
    }});
    
    if (activeElement) {{
        activeElement.dispatchEvent(keyEvent);
    }} else {{
        document.dispatchEvent(keyEvent);
    }}
    
    // If it's an input element, handle cursor positioning based on modifiers
    if (activeElement && (activeElement.tagName === 'INPUT' || activeElement.tagName === 'TEXTAREA')) {{
        if ({isShiftPressed.ToString().ToLower()}) {{
            // When shift is pressed, we want to select from current position to the beginning
            const currentPos = activeElement.selectionStart;
            activeElement.selectionEnd = currentPos;
            activeElement.selectionStart = 0;
        }} else {{
            // Without shift, just move cursor to beginning
            activeElement.selectionStart = 0;
            activeElement.selectionEnd = 0;
        }}
        
        // Handle Ctrl+Home which goes to beginning of document, not just line
        if ({isCtrlPressed.ToString().ToLower()} && activeElement.tagName === 'TEXTAREA') {{
            if ({isShiftPressed.ToString().ToLower()}) {{
                // Select from current to beginning of document
                const currentPos = activeElement.selectionStart;
                activeElement.selectionEnd = currentPos;
                activeElement.selectionStart = 0;
            }} else {{
                // Move to beginning of document
                activeElement.selectionStart = 0;
                activeElement.selectionEnd = 0;
            }}
        }}
    }}
}})();";

            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
    }


}