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
using System.Reflection;
using System.Text;
using System.Resources;
using System.Runtime.InteropServices;

namespace AiTool3.UI
{
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class ChatWebView : WebView2
    {
        public event EventHandler<ChatWebViewSendMessageEventArgs>? ChatWebViewSendMessageEvent;
        public event EventHandler<ChatWebViewCopyEventArgs>? ChatWebViewCopyEvent;
        public event EventHandler<ChatWebViewCancelEventArgs>? ChatWebViewCancelEvent;
        public event EventHandler<ChatWebViewNewEventArgs>? ChatWebViewNewEvent;
        public event EventHandler<string> FileDropped;

        public ChatWebView() : base()
        {
            if (!IsDesignMode())
            {
                HandleCreated += OnHandleCreated!;
                WebMessageReceived += WebView_WebMessageReceived;
                
            }
            AllowExternalDrop = false;
            
        }

   

        protected virtual void OnFileDropped(string filename)
        {
            FileDropped?.Invoke(this, filename);
        }

        private async void  WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            
            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);
            var content = message["content"];
            var type = message?["type"];
            switch (type)
            {
                case "fileDropped":
                    OnFileDropped(content);
                    break;
                case "new":
                    ChatWebViewNewEvent?.Invoke(this, new ChatWebViewNewEventArgs(ChatWebViewNewType.New));
                    break;
                case "newWithContext":
                    ChatWebViewNewEvent?.Invoke(this, new ChatWebViewNewEventArgs(ChatWebViewNewType.NewWithContext));
                    break;
                case "newWithPrompt":
                    ChatWebViewNewEvent?.Invoke(this, new ChatWebViewNewEventArgs(ChatWebViewNewType.NewWithPrompt));
                    break;
                case "send":
                    ChatWebViewSendMessageEvent?.Invoke(this, new ChatWebViewSendMessageEventArgs { Content = content });
                    break;
                case "cancel":
                    ChatWebViewCancelEvent?.Invoke(this, new ChatWebViewCancelEventArgs());
                    break;
                case "Copy":
                    ChatWebViewCopyEvent?.Invoke(this, new ChatWebViewCopyEventArgs { Content = content, Guid = null });
                    break;
                case "Run Python Script":
                    PythonHelper.LaunchPythonScript(content);
                    break;
                case "Run PowerShell Script":
                    await LaunchHelpers.LaunchPowerShell(content);
                    break;
                case "Launch STL":
                    StlHelper.LaunchStlFile(content);

                    break;
                case "View JSON String Array":

                    // parse content as json string array, don't crash if it isn't tho
                    List<string> suggestions;

                    try
                    {
                        suggestions = JsonConvert.DeserializeObject<List<string>>(content);
                    }
                    catch (Exception)
                    {
                        suggestions = new List<string> { content };
                    }

                    new AutoSuggestForm(suggestions.ToArray()).Show();

                    break;
                case "Save As":
                    var dataType = message["dataType"];
                    var filext = SnippetManager.GetFileExtFromLanguage(dataType);
                    SaveFileDialog saveFileDialog = new SaveFileDialog();

                    saveFileDialog.Filter = $"{dataType} files (*.{filext})|*.{filext}|All files (*.*)|*.*";
                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, SnippetHelper.StripFirstAndLastLine(content));
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
            CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
            var resources = GetResourceDetails();

            foreach (var resource in resources)
            {
                CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
            }

            var html = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.html");
            var css = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.css");

            // find the first style tag
            var styleTagIndex = html.IndexOf("<style>", StringComparison.Ordinal) + "<style>".Length;

            // insert the css
            html = html.Insert(styleTagIndex, css);

            NavigateToString(html);

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.JsonViewer.js"));

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.MermaidViewer.js"));

            CoreWebView2.NewWindowRequested += (sender2, e2) => {
                String _fileurl = e2.Uri.ToString();
                e2.Handled = true;
                FileDropped?.Invoke(this, _fileurl);
            };
        }

        internal async Task AddMessages(List<CompletionMessage> parents)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await ExecuteScriptAsync($"AddInitialMessages({JsonConvert.SerializeObject(parents)})");
        }

        // WebViewCallAndCallbackSystem
        internal async Task UpdateSendButtonColor(bool embeddingsEnabled) => await ExecuteScriptAsync($"updateSendButtonColor({embeddingsEnabled.ToString().ToLower()})");
        
        internal async Task UpdateSystemPrompt(string systemPrompt) => await ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");

        internal async Task DisableCancelButton() => await ExecuteScriptAsync("disableButton('cancelButton')");
        internal async Task EnableCancelButton() => await ExecuteScriptAsync("enableButton('cancelButton')");
        internal async Task DisableSendButton() => await ExecuteScriptAsync("disableButton('sendButton')");
        internal async Task EnableSendButton() => await ExecuteScriptAsync("enableButton('sendButton')");

        internal async Task AddMessage(CompletionMessage message) => await ExecuteScriptAsync($"AddMessage({JsonConvert.SerializeObject(message)})");

        internal async Task<string> GetSystemPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getSystemPrompt()"));

        internal async Task<string> GetUserPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getUserPrompt()"));

        internal async Task Clear(SettingsSet currentSettings)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await DisableCancelButton();
            await EnableSendButton();
            await UpdateSendButtonColor(currentSettings.UseEmbeddings);
        }

        internal async Task SetUserPrompt(string content) => await ExecuteScriptAsync($"document.querySelector('#chatInput').value = {JsonConvert.SerializeObject(content)}");//changeChatHeaderLabel

        internal async Task ChangeChatHeaderLabel(string content) => await ExecuteScriptAsync($"changeChatHeaderLabel({JsonConvert.SerializeObject(content)})");

        private bool IsDesignMode() => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;

        internal async void UpdateTemp(string e) => await ExecuteScriptAsync($"updateTemp({JsonConvert.SerializeObject(e)})");

        internal async void ClearTemp() => await ExecuteScriptAsync($"clearTemp()");




        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            List<ResourceDetails> resources = GetResourceDetails();

            resources.Where(x => e.Request.Uri == x.Uri).ToList().ForEach(x => ReturnResourceToWebView(e, x.ResourceName, x.MimeType));
        }

        private static List<ResourceDetails> GetResourceDetails()
        {
            return new List<ResourceDetails>
            {
                new ResourceDetails
                {
                    Uri = "https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.jsoneditor.min.js",
                    MimeType = "application/javascript"
                },

                new ResourceDetails
                {
                    Uri = "https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor.min.css",
                    ResourceName = "AiTool3.ThirdPartyJavascript.jsoneditor.min.css",
                    MimeType = "text/css"
                },//

                new ResourceDetails
                {
                    Uri = "https://cdnjs.cloudflare.com/ajax/libs/jsoneditor/9.9.2/jsoneditor-icons.svg",
                    ResourceName = "AiTool3.ThirdPartyJavascript.jsoneditor-icons.svg",
                    MimeType = "image/svg+xml"
                },
                new ResourceDetails
                {
                    Uri = "https://cdnjs.cloudflare.com/ajax/libs/cytoscape/3.21.1/cytoscape.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.cytoscape.min.js",
                    MimeType = "application/javascript"
                },
                new ResourceDetails
                {
                    Uri = "https://cdnjs.cloudflare.com/ajax/libs/dagre/0.8.5/dagre.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.dagre.min.js",
                    MimeType = "application/javascript"
                },
                new ResourceDetails
                {
                    Uri = "https://cdn.jsdelivr.net/npm/cytoscape-cxtmenu@3.4.0/cytoscape-cxtmenu.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.cytoscape-cxtmenu.min.js",
                    MimeType = "application/javascript"
                },
                new ResourceDetails
                {
                    Uri = "https://cdn.jsdelivr.net/npm/cytoscape-dagre@2.3.2/cytoscape-dagre.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.cytoscape-dagre.min.js",
                    MimeType = "application/javascript"
                },
            };
        }

        private void ReturnResourceToWebView(CoreWebView2WebResourceRequestedEventArgs e, string resourceName, string mimeType)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    // Read the embedded resource
                    using (var reader = new StreamReader(stream))
                    {
                        string content = reader.ReadToEnd();

                        // Create a memory stream from the content
                        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));

                        // Create a custom response
                        var response = CoreWebView2.Environment.CreateWebResourceResponse(memoryStream, 200, "OK", $"Content-Type: {mimeType}");

                        // Set the response
                        e.Response = response;

                        return;
                    }
                }
                throw new NotImplementedException();
            }
        }
    }
}