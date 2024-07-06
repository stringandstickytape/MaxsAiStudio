using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AiTool3.UI.NetworkDiagramControl;

namespace AiTool3.UI
{
    public class WebViewTestForm : Form
    {
        public WebView2 webView;

        public WebViewTestForm()
        {
            this.Size = new System.Drawing.Size(800, 600);
            this.Text = "Node Diagram WebView";

            webView = new WebView2();




            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);
            
            webView.WebMessageReceived += WebView_WebMessageReceived;
        }

        // event to raise when a menu option is selected
        public event EventHandler<WebNdcContextMenuOptionSelectedEventArgs> WebNdcContextMenuOptionSelected;
        public event EventHandler<WebNdcNodeClickedEventArgs> WebNdcNodeClicked;

        private void WebView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);

            switch (message["type"])
            {
                case "nodeClicked":
                    string clickedNodeId = message["nodeId"];
                    string clickedNodeLabel = message["nodeLabel"];
                    WebNdcNodeClicked?.Invoke(this, new WebNdcNodeClickedEventArgs(clickedNodeId, clickedNodeLabel));
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
                    // raise event
                    WebNdcContextMenuOptionSelected?.Invoke(this, new WebNdcContextMenuOptionSelectedEventArgs());
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

        public async void CentreOnNode(string guid)
        {
            await webView.CoreWebView2.ExecuteScriptAsync($"centerOnNode('{guid}')");
        }

        public async Task<bool> Clear()
        {
            await webView.CoreWebView2.ExecuteScriptAsync($"clear('')");
            return true;
        }

        public async Task<string> EvaluateJavascriptAsync(string html)
        {
            var a = await webView.CoreWebView2.ExecuteScriptAsync(html);
            return a;
        }

        internal static async Task<WebViewTestForm> OpenWebViewWithJs(string result)
        {


            var form = new WebViewTestForm();
            await form.webView.EnsureCoreWebView2Async(null);
            await form.webView.CoreWebView2.Profile.ClearBrowsingDataAsync();
            // open dev tools
            form.webView.CoreWebView2.OpenDevToolsWindow();
            form.WebNdcContextMenuOptionSelected += Form_WebNdcContextMenuOptionSelected;
            await form.InitializeAsync();
            await form.EvaluateJavascriptAsync(result);
            form.Show();
            return form ;
        }//<insertscripthere/>

        private static void Form_WebNdcContextMenuOptionSelected(object? sender, WebNdcContextMenuOptionSelectedEventArgs e)
        {
            
        }

        internal static async Task<WebViewTestForm> OpenWebViewWithHtml(string result)
        {
            var form = new WebViewTestForm();
            form.WebNdcContextMenuOptionSelected += Form_WebNdcContextMenuOptionSelected;
            await form.InitializeAsync();
            form.NavigateToHtml(result);
            form.Show(); // returns instantly
            return form;
        }//<insertscripthere/>
    }

    public class WebNdcContextMenuOptionSelectedEventArgs
    {
    }

    public class WebNdcNodeClickedEventArgs
    {
        public string NodeId { get; }
        public string NodeLabel { get; }

        public WebNdcNodeClickedEventArgs(string nodeId, string nodeLabel)
        {
            NodeId = nodeId;
            NodeLabel = nodeLabel;
        }
    }
}
