using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace AiTool3.UI
{
    [Designer(typeof(CustomWebView2ControlDesigner))]
    [ToolboxItem(true)]
    public class CustomWebView2Control : WebView2
    {


        public event EventHandler<WebNdcContextMenuOptionSelectedEventArgs2> WebNdcContextMenuOptionSelected;
        public event EventHandler<WebNdcNodeClickedEventArgs2> WebNdcNodeClicked;

        public CustomWebView2Control()
        {
            this.Dock = DockStyle.Fill;
            this.WebMessageReceived += WebView_WebMessageReceived;
        }

        private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);

            switch (message["type"])
            {
                case "nodeClicked":
                    string clickedNodeId = message["nodeId"];
                    WebNdcNodeClicked?.Invoke(this, new WebNdcNodeClickedEventArgs2(clickedNodeId));
                    break;

                case "getContextMenuOptions":
                    string nodeId = message["nodeId"];
                    string nodeLabel = message["nodeLabel"];

                    var options = new[] { "Option 1", "Option 2", "Option 3" };

                    string optionsJson = System.Text.Json.JsonSerializer.Serialize(options);
                    string script = $"updateContextMenuOptions({optionsJson});";
                    this.ExecuteScriptAsync(script);
                    break;

                case "contextMenuOptionSelected":
                    string selectedNodeId = message["nodeId"];
                    string selectedNodeLabel = message["nodeLabel"];
                    string selectedOption = message["option"];

                    Debug.WriteLine($"Node: {selectedNodeId} ({selectedNodeLabel}), Selected option: {selectedOption}");
                    WebNdcContextMenuOptionSelected?.Invoke(this, new WebNdcContextMenuOptionSelectedEventArgs2());
                    break;
            }
        }

        public async Task InitializeAsync()
        {
            await this.EnsureCoreWebView2Async(null);
        }

        public void NavigateToHtml(string html)
        {
            this.NavigateToString(html);
        }

        public async void CentreOnNode(string guid)
        {
            await this.CoreWebView2.ExecuteScriptAsync($"centerOnNode('{guid}')");
        }

        public async Task<bool> Clear()
        {
            await this.CoreWebView2.ExecuteScriptAsync($"clear('')");
            return true;
        }

        public async Task<string> EvaluateJavascriptAsync(string script)
        {
            return await this.CoreWebView2.ExecuteScriptAsync(script);
        }

        public async Task OpenWebViewWithJs(string result)
        {
            await InitializeAsync();
            await CoreWebView2.Profile.ClearBrowsingDataAsync();
            CoreWebView2.OpenDevToolsWindow();
            WebNdcContextMenuOptionSelected += Form_WebNdcContextMenuOptionSelected;
            await EvaluateJavascriptAsync(result);
            return;
        }

        public async Task OpenWebViewWithHtml(string result)
        {
            WebNdcContextMenuOptionSelected += Form_WebNdcContextMenuOptionSelected;
            await InitializeAsync();
            NavigateToHtml(result);
        }

        private static void Form_WebNdcContextMenuOptionSelected(object sender, WebNdcContextMenuOptionSelectedEventArgs2 e)
        {
            // Handle context menu option selected
        }
    }


    public class WebNdcContextMenuOptionSelectedEventArgs2 : EventArgs
    {
        // Add properties as needed
    }

    public class WebNdcNodeClickedEventArgs2 : EventArgs
    {
        public string NodeId { get; }

        public WebNdcNodeClickedEventArgs2(string nodeId)
        {
            NodeId = nodeId;
        }
    }

    public class CustomWebView2ControlDesigner : ControlDesigner
    {
        public override void Initialize(IComponent component)
        {
            base.Initialize(component);
            if (component is CustomWebView2Control control)
            {
                EnableDesignMode(control, "CustomWebView2Control");
            }
        }
    }
}