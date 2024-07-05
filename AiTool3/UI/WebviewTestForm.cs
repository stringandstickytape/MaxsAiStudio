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
            
            webView.WebMessageReceived += WebView_WebMessageReceived;
        }

        private void WebView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);

            switch (message["type"])
            {
                case "nodeClicked":
                    // Handle node click (existing code)
                    break;

                case "getContextMenuOptions":
                    string nodeId = message["nodeId"];
                    string nodeLabel = message["nodeLabel"];

                    // Define your context menu options here
                    var options = new[] { "Option 1", "Option 2", "Option 3" };

                    // Send options back to JavaScript to update the menu
                    string optionsJson = System.Text.Json.JsonSerializer.Serialize(options);
                    string script = $"updateContextMenuOptions({optionsJson});";
                    webView.ExecuteScriptAsync(script);
                    break;

                case "contextMenuOptionSelected":
                    string selectedNodeId = message["nodeId"];
                    string selectedNodeLabel = message["nodeLabel"];
                    string selectedOption = message["option"];

                    // Handle the selected option
                    Debug.WriteLine($"Node: {selectedNodeId} ({selectedNodeLabel}), Selected option: {selectedOption}");
                    // Perform actions based on the selected option
                    break;
            }
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
