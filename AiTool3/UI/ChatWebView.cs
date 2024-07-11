using AiTool3.Conversations;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System.ComponentModel;

namespace AiTool3.UI
{
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class ChatWebView : WebView2
    {
        public event EventHandler<ChatWebViewSendMessageEventArgs> ChatWebViewSendMessageEvent;
        public event EventHandler<ChatWebViewCopyEventArgs> ChatWebViewCopyEvent;

        public ChatWebView() : base()
        {
            // run initializeasync
            if (!DesignMode) HandleCreated += OnHandleCreated;

            WebMessageReceived += WebView_WebMessageReceived;
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);

            switch (message["type"])
            {
                case "send":
                    var content = message["content"];
                    ChatWebViewSendMessageEvent?.Invoke(this, new ChatWebViewSendMessageEventArgs { Content = content });
                    break;
                case "copy":
                    var content2 = message["content"];
                    var guid2 = message["guid"];
                    ChatWebViewCopyEvent?.Invoke(this, new ChatWebViewCopyEventArgs { Content = content2, Guid = guid2 });
                    break;
            }
        }

        private async void OnHandleCreated(object sender, EventArgs e)
        {
            HandleCreated -= OnHandleCreated;
            await InitializeAsync();
        }


        public async Task InitializeAsync()
        {
            await EnsureCoreWebView2Async(null);

            var html = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.html");
            var css = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.css");

            html = html.Replace("{magiccsstoken}", css);

            NavigateToString(html);
        }

        internal async Task AddMessages(List<CompletionMessage> parents)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await ExecuteScriptAsync($"AddInitialMessages({JsonConvert.SerializeObject(parents)})");
        }

        internal async Task AddMessage(CompletionMessage message)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"AddMessage({JsonConvert.SerializeObject(message)})");
        }
    }
}