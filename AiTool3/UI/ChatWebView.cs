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
using AiTool3.ApiManagement;

namespace AiTool3.UI
{
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    public class ChatWebView : WebView2
    {
        public string GuidValue { get; private set; }
        public event EventHandler<ChatWebViewSendMessageEventArgs>? ChatWebViewSendMessageEvent;
        public event EventHandler<ChatWebViewCopyEventArgs>? ChatWebViewCopyEvent;
        public event EventHandler<ChatWebViewCancelEventArgs>? ChatWebViewCancelEvent;
        public event EventHandler<ChatWebViewNewEventArgs>? ChatWebViewNewEvent;
        public event EventHandler<ChatWebViewAddBranchEventArgs>? ChatWebViewAddBranchEvent;
        public event EventHandler<ChatWebViewJoinWithPreviousEventArgs>? ChatWebViewJoinWithPreviousEvent;
        public event EventHandler<ChatWebDropdownChangedEventArgs>? ChatWebDropdownChangedEvent;
        public event EventHandler<ChatWebViewSimpleEventArgs>? ChatWebViewSimpleEvent;
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
            var content = message.ContainsKey("content") ? message["content"] : null;
            var type = message?["type"];
            switch (type)
            {
                case "dropdownChanged":
                    ChatWebDropdownChangedEvent?.Invoke(this, new ChatWebDropdownChangedEventArgs() { Dropdown = message["id"], ModelString = content });
                    break;
                case "joinWithPrevious":
                    ChatWebViewJoinWithPreviousEvent?.Invoke(this, new ChatWebViewJoinWithPreviousEventArgs(GuidValue = message["guid"]));
                    break;

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
                case "attach":
                case "project":
                case "voice":
                    ChatWebViewSimpleEvent?.Invoke(this, new ChatWebViewSimpleEventArgs(type));
                    break;
                case "send":
                    var selectedTools = message?["selectedTools"];
                    ChatWebViewSendMessageEvent?.Invoke(this, new ChatWebViewSendMessageEventArgs { Content = content, SelectedTools = selectedTools.Split(',').ToList() });
                    break;
                case "applyFindAndReplace":
                    ChatWebViewAddBranchEvent?.Invoke(this, new ChatWebViewAddBranchEventArgs {
                        CodeBlockIndex = int.Parse(message["codeBlockIndex"]),
                        Content = content,
                        DataType = message["dataType"],
                        Guid = message["guid"],
                        Type = message["type"],
                        FindAndReplacesJson = message["findAndReplaces"],
                        SelectedMessageGuid = message["selectedMessageGuid"]
                    });
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
                case "View PlantUML Diagram":
                    PlantUMLViewer.View(content);
                    break;
                case "View DOT Diagram":
                    DotViewer.View(content);
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

            foreach (var resource in GetResourceDetails())
            {
                CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
            }

            // ask the user whetehr to open cwv or cwv2
            //var mb = MessageBox.Show("Use CWV2?", "CWV2", MessageBoxButtons.YesNo);
            string html;
            //if(mb == DialogResult.Yes)
                html = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView2.html");
            //else
            //    html = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.html");

            var css = AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.ChatWebView.css");

            // find the first style tag
            var styleTagIndex = html.IndexOf("<style>", StringComparison.Ordinal) + "<style>".Length;

            // insert the css
            html = html.Insert(styleTagIndex, css);

            NavigateToString(html);

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.JsonViewer.js"));

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.SvgViewer.js"));

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.MermaidViewer.js"));

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.DotViewer.js"));

            ExecuteScriptAsync(AssemblyHelper.GetEmbeddedAssembly("AiTool3.JavaScript.FindAndReplacer.js"));

            
            
        }

        // begin webview interface methods

        // implemented in chatwebview2.html
        internal async Task AddMessages(List<CompletionMessage> parents)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await ExecuteScriptAsync($"AddInitialMessages({JsonConvert.SerializeObject(parents)})");
        }

        public async Task<Model> GetDropdownModel(string str, SettingsSet settings)
        {
            var modelString = JsonConvert.DeserializeObject<string>(await GetDropdownValue(str));
            var model = settings.GetAllModels().FirstOrDefault(m => m.ToString() == modelString);
            return model;
        }

        // WebViewCallAndCallbackSystem

        #region implemented in chatwebview2.html


        internal async Task SetDropdownValue(string v1, string v2)
        {
            ExecuteScriptAsync($"setDropdownValue('{v1}', '{v2}')");
        }
        internal async Task<string> GetDropdownValue(string v)
        {
            return await ExecuteScriptAsync($"getDropdownValue('{v}')");
        }


        internal async Task SetModels(List<Model> models)
        {
            var modelStrings = models.Select(x => x.ToString());

            // window.setDropdownOptions('mainAI',

            foreach(var dropdown in new[] { "mainAI", "summaryAI" })
            {
                ExecuteScriptAsync($"setDropdownOptions('{dropdown}', {JsonConvert.SerializeObject(modelStrings)})");
            }
        }

        internal async Task UpdateSystemPrompt(string systemPrompt) => await ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");

        internal async Task AddMessage(CompletionMessage message) => await ExecuteScriptAsync($"AddMessage({JsonConvert.SerializeObject(message)})");

        internal async Task<string> GetSystemPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getSystemPrompt()"));

        internal async Task<string> GetUserPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getUserPrompt()"));

        internal async Task SetUserPrompt(string content)
        {
            await ExecuteScriptAsync($"document.querySelector('#chatInput').value = {JsonConvert.SerializeObject(content)}");//changeChatHeaderLabel
            await ExecuteScriptAsync($"setUserPrompt({JsonConvert.SerializeObject(content)})");//changeChatHeaderLabel
        }

        internal async Task Clear(SettingsSet currentSettings)
        {
            // run "addMessages" js function
            await ExecuteScriptAsync($"ClearMessages()");
            await DisableCancelButton();
            await EnableSendButton();
            await UpdateSendButtonColor(currentSettings.UseEmbeddings);
        }

        internal async Task DisableCancelButton()
        {

            await ExecuteScriptAsync("disableButton('cancelButton')");
            await ExecuteScriptAsync("disableCancelButton()");
        }
        internal async Task EnableCancelButton()
        {
            await ExecuteScriptAsync("enableButton('cancelButton')");
            await ExecuteScriptAsync("enableCancelButton()");
        }
        internal async Task DisableSendButton()
        {
            await ExecuteScriptAsync("disableButton('sendButton')");
            await ExecuteScriptAsync("disableSendButton()");
        }
        internal async Task EnableSendButton()
        {
            await ExecuteScriptAsync("enableButton('sendButton')");
            await ExecuteScriptAsync("enableSendButton()");
        }

        #endregion implemented in chatwebview2.html

        internal async Task UpdateSendButtonColor(bool embeddingsEnabled) => await ExecuteScriptAsync($"updateSendButtonColor({embeddingsEnabled.ToString().ToLower()})");

        internal async void UpdateTemp(string e) => await ExecuteScriptAsync($"updateTemp({JsonConvert.SerializeObject(e)})");

        internal async void ClearTemp() => await ExecuteScriptAsync($"clearTemp()");

        internal async Task ChangeChatHeaderLabel(string content) => await ExecuteScriptAsync($"changeChatHeaderLabel({JsonConvert.SerializeObject(content)})");

        // end webview interface methods

        private bool IsDesignMode() => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;


        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            var rd = GetResourceDetails();
            var matching = rd.Where(x => e.Request.Uri == x.Uri).ToList();


            GetResourceDetails().Where(x => e.Request.Uri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x => ReturnResourceToWebView(e, x.ResourceName, x.MimeType));
        }

        private static List<ResourceDetails> GetResourceDetails()
        {

            // create a new resourcedetail for each resource in namespace AiTool3.JavaScript.Components
            var resources = new List<ResourceDetails>();
            foreach (var resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (resourceName.StartsWith("AiTool3.JavaScript.Components"))
                {
                    // find the index of the penultimate dot in resource name
                    var penultimateDotIndex = resourceName.LastIndexOf(".", resourceName.LastIndexOf(".") - 1);
                    // get the filename using that
                    var filename = resourceName.Substring(penultimateDotIndex + 1);

                    resources.Add(new ResourceDetails
                    {
                        Uri = $"http://localhost/{filename}",
                        ResourceName = resourceName,
                        MimeType = "text/babel"
                    });
                }
            }

            resources.AddRange(new List<ResourceDetails>
            {
                new ResourceDetails
                {
                    Uri = "https://cdn.jsdelivr.net/npm/mermaid@10.2.3/dist/mermaid.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.mermaid.min.js",
                    MimeType = "application/javascript"
                },


                new ResourceDetails
                {
                    Uri = "https://cdn.jsdelivr.net/npm/mermaid@10.2.3/dist/mermaid.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.mermaid.min.js",
                    MimeType = "application/javascript"
                },

                new ResourceDetails
                {
                    Uri = "https://cdn.jsdelivr.net/npm/svg-pan-zoom@3.6.1/dist/svg-pan-zoom.min.js",
                    ResourceName = "AiTool3.ThirdPartyJavascript.svg-pan-zoom.min.js",
                    MimeType = "application/javascript"
                },

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
            });

            return resources;
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

                        // need to add a Access-Control-Allow-Origin header to the response
                        e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", "*");


                        return;
                    }
                }
                throw new Exception("Probably forgot to embed the resource :(");
            }
        }

    }
}