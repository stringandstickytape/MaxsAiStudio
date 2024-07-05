using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.UI
{
    public class WebViewTestForm : Form
    {
        private WebView2 webView;

        public WebViewTestForm()
        {
            this.Size = new System.Drawing.Size(800, 600);
            this.Text = "Node Diagram WebView";

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            webView.WebMessageReceived += (sender, args) =>
            {
                if (args.TryGetWebMessageAsString() == "TriggerCSharpFunction")
                {
                }
            };

        }

        public async Task InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
        }

        public void NavigateToHtml(string html)
        {
            webView.NavigateToString(html);
        }

        public async Task<string> EvaluateJavascriptAsync(string html)
        {
            return await webView.CoreWebView2.ExecuteScriptAsync(html);
        }

        public async void bork()
        {
            
        }

        public void Callback()
        {
        }

        /*
        public static async Task<bool> OpenWebViewWithHtml()
        {
            string html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>WebView2 Communication</title>
</head>
<body>
    <div id=""output""></div>
    <button id=""triggerCSharpButton"">Trigger C# Code</button>

    <script>
        let input = '';
        const outputDiv = document.getElementById('output');
        const triggerButton = document.getElementById('triggerCSharpButton');

        // Create a proxy to watch the input variable
        const inputProxy = new Proxy({input}, {
            set: function(target, key, value) {
                target[key] = value;
                outputDiv.textContent = value;
                return true;
            }
        });

        // Function to update the input (can be called from C#)
        function setInput(value) {
            inputProxy.input = value;
        }

        // Function to trigger C# code
        async function triggerCSharp() {
            await chrome.webview.postMessage('TriggerCSharpFunction');
        }

        // Add click event listener to the button
        triggerButton.addEventListener('click', triggerCSharp);

        // Expose the setInput function to be callable from C#
        window.setInput = setInput;
    </script>
</body>
</html>";

            var form = new WebViewTestForm();
            await form.InitializeAsync();
            form.NavigateToHtml(html);
            
            // wait til the html is loaded
            await Task.Delay(1000);
            
            //form.EvaluateJavascript(" let node2 = document.querySelectorAll('.node');\r\nfor(let i = 0; i < node2.length; i++) {\r\n    node2[i].textContent = \"LOL\";\r\n}");
            form.EvaluateJavascript("setInput(\"fun!\")");
            form.Show();
            return true;
        }*/

        internal static async Task<WebViewTestForm> OpenWebViewWithJs(string result)
        {
            var form = new WebViewTestForm();
            await form.InitializeAsync();
            await form.EvaluateJavascriptAsync(result);
            form.Show();
            return form ;
        }//<insertscripthere/>

        internal static async Task<WebViewTestForm> OpenWebViewWithHtml(string result)
        {
            var form = new WebViewTestForm();
            await form.InitializeAsync();
            form.NavigateToHtml(result);
            form.Show(); // returns instantly
            return form;
        }//<insertscripthere/>
    }
}
