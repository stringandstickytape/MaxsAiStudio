using AiTool3.Conversations;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using static AiTool3.UI.NetworkDiagramControl;
using System.Windows.Forms;
using AiTool3.Helpers;
using AiTool3.Snippets;

namespace AiTool3.UI
{
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class ChatWebView : WebView2
    {
        public event EventHandler<ChatWebViewSendMessageEventArgs>? ChatWebViewSendMessageEvent;
        public event EventHandler<ChatWebViewCopyEventArgs>? ChatWebViewCopyEvent;
        public event EventHandler<ChatWebViewCancelEventArgs>? ChatWebViewCancelEvent;

        public ChatWebView() : base()
        {
            if (!IsDesignMode())
            {
                HandleCreated += OnHandleCreated!;
                WebMessageReceived += WebView_WebMessageReceived;
            }
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            
            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);
            var content = message["content"];
            
            switch (message?["type"])
            {
                case "send":
                    ChatWebViewSendMessageEvent?.Invoke(this, new ChatWebViewSendMessageEventArgs { Content = content });
                    break;
                case "cancel":
                    ChatWebViewCancelEvent?.Invoke(this, new ChatWebViewCancelEventArgs());
                    break;
                case "Copy":
                    var guid2 = message["guid"];
                    ChatWebViewCopyEvent?.Invoke(this, new ChatWebViewCopyEventArgs { Content = content, Guid = guid2 });
                    break;
                case "Save As":
                    var dataType = message["dataType"];
                    var type = SnippetManager.GetFileExtFromLanguage(dataType);
                    SaveFileDialog saveFileDialog = new SaveFileDialog();

                    saveFileDialog.Filter = $"{dataType} files (*.{type})|*.{type}|All files (*.*)|*.*";
                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, SnipperHelper.StripFirstAndLastLine(content));
                    }
                    break;
                case "WebView":

                    // create a new form of 256x256
                    var form = new Form();
                    form.Size = new Size(256, 256);
                    form.StartPosition = FormStartPosition.CenterScreen;

                    // create a WebView2 that fills the window
                    var wvForm = new WebviewForm(message["content"]);
                    wvForm.Show();
                    break;
            }
        }

        private async void OnHandleCreated(object sender, EventArgs e)
        {
            HandleCreated -= OnHandleCreated!;
            await InitializeAsync();
        }


        public async Task InitializeAsync()
        {
            if (IsDesignMode())
                return;

            await EnsureCoreWebView2Async(null);

            var html = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.html");
            var css = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.css");

            html = html.Replace(".magiccsstoken {}", css);

            NavigateToString(html);
        }

        internal async Task AddMessages(List<CompletionMessage> parents)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await ExecuteScriptAsync($"AddInitialMessages({JsonConvert.SerializeObject(parents)})");
        }

        internal async Task UpdateSystemPrompt(string systemPrompt)
        {
            await ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");
        }

        internal async Task DisableCancelButton() => await ExecuteScriptAsync("disableButton('cancelButton')");
        internal async Task EnableCancelButton() => await ExecuteScriptAsync("enableButton('cancelButton')");
        internal async Task DisableSendButton() => await ExecuteScriptAsync("disableButton('sendButton')");
        internal async Task EnableSendButton() => await ExecuteScriptAsync("enableButton('sendButton')");
        internal async Task AddMessage(CompletionMessage message)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"AddMessage({JsonConvert.SerializeObject(message)})");
        }

        internal async Task<string> GetSystemPrompt()
        {
            return JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getSystemPrompt()"));

        }

        internal async Task Clear()
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await DisableCancelButton();
            await EnableSendButton();
        }

        internal async Task SetUserPrompt(string content)
        {
            await ExecuteScriptAsync($"document.querySelector('#chatInput').value = {JsonConvert.SerializeObject(content)}");
        }//changeChatHeaderLabel

        internal async Task ChangeChatHeaderLabel(string content)
        {
            await ExecuteScriptAsync($"changeChatHeaderLabel({JsonConvert.SerializeObject(content)})");
        }

        private bool IsDesignMode()
        {
            return DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }
    }
}