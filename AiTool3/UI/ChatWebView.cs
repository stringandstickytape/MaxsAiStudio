using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.Helpers;
using AiTool3.Snippets;
using AiTool3.Tools;
using AiTool3.Topics;
using FFmpeg.AutoGen;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using SharedClasses.Helpers;
using SharedClasses.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

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
        public event EventHandler<ChatWebViewSimpleEventArgs>? ChatWebViewContinueEvent;
        public event EventHandler<ChatWebViewSimpleEventArgs>? ChatWebViewReadyEvent;
        public event EventHandler<ChatWebViewSimpleEventArgs>? ChatWebViewSimpleEvent;
        private ToolManager _toolManager;
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

        public void InjectDependencies(ToolManager toolManager)
        {
            _toolManager = toolManager;
        }

        protected virtual void OnFileDropped(string filename)
        {
            FileDropped?.Invoke(this, filename);
        }

        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {

            string jsonMessage = e.WebMessageAsJson;
            var message = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonMessage);
            var content = message.ContainsKey("content") ? message["content"] : null;
            var type = message?["type"];
            switch (type)
            {

                case "openUrl":
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {content.Replace("&", "^&")}") { CreateNoWindow = true });
                    break;
                case "importTemplate":
                case "saveScratchpad":
                    ChatWebViewSimpleEvent?.Invoke(this, new ChatWebViewSimpleEventArgs(type) { Json = message["content"] });
                    break;
                case "ready":
                    ChatWebViewReadyEvent?.Invoke(this, new ChatWebViewSimpleEventArgs("ready", ""));
                    break;
                case "continue":
                    var guid = message["guid"];
                    ChatWebViewContinueEvent?.Invoke(this, new ChatWebViewSimpleEventArgs("continue", guid));
                    break;
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
                case "ApplyFaRArray":
                    await FineAndReplaceProcessor.ApplyFindAndReplaceArray(JsonConvert.DeserializeObject<FindAndReplaceSet>(message["content"]), this);
                    break;
                case "attach":
                case "project":
                case "voice":
                    ChatWebViewSimpleEvent?.Invoke(this, new ChatWebViewSimpleEventArgs(type));
                    break;
                case "allThemes":
                    ChatWebViewSimpleEvent?.Invoke(this, new ChatWebViewSimpleEventArgs(type) { Json = message["content"] });
                    break;
                case "selectTheme":
                    ChatWebViewSimpleEvent?.Invoke(this, new ChatWebViewSimpleEventArgs(type) { Json = message["content"] });
                    break;
                case "toggleModelStar":
                    ChatWebViewSimpleEvent?.Invoke(this, new ChatWebViewSimpleEventArgs(type) { Json = jsonMessage });
                    break;
                case "send":
                    var selectedTools = message?["selectedTools"];
                    ChatWebViewSendMessageEvent?.Invoke(this, new ChatWebViewSendMessageEventArgs { Content = content, SelectedTools = selectedTools.Split(',').ToList(), SendViaSecondaryAI = false, AddEmbeddings = bool.Parse(message?["addEmbeddings"]) });
                    break;
                case "sendSecondary":
                    var selectedTools2 = message?["selectedTools"];
                    ChatWebViewSendMessageEvent?.Invoke(this, new ChatWebViewSendMessageEventArgs { Content = content, SelectedTools = selectedTools2.Split(',').ToList(), SendViaSecondaryAI = true, AddEmbeddings = bool.Parse(message?["addEmbeddings"]) });
                    break;
                case "applyFindAndReplace":
                    ChatWebViewAddBranchEvent?.Invoke(this, new ChatWebViewAddBranchEventArgs
                    {
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
                case "Save As":
                    var dataType = message["dataType"];

                    var inFileExt = dataType.Contains(".") ? dataType.Split('.').Last() : dataType;

                    var filext = SnippetManager.GetFileExtFromLanguage(inFileExt);
                    SaveFileDialog saveFileDialog = new SaveFileDialog();

                    saveFileDialog.Filter = $"{filext} files (*{filext})|*{filext}|All files (*.*)|*.*";

                    // if datatype contains \\ it's a full path and file.  Default teh save file dialog thus.
                    if (dataType.Contains(".") || dataType.Contains("\\"))
                    {

                        if (dataType.Contains("\\"))
                        {
                            saveFileDialog.FileName = Path.GetFileName(dataType);
                            saveFileDialog.InitialDirectory = Path.GetDirectoryName(dataType);
                        }
                        else saveFileDialog.FileName = dataType;
                    }

                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, content);
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

            foreach (var resource in AssemblyHelper.GetResourceDetails())
            {
                CoreWebView2.AddWebResourceRequestedFilter(resource.Uri, CoreWebView2WebResourceContext.All);
            }
            NavigateToString(AssemblyHelper.GetEmbeddedResource("SharedClasses", "SharedClasses.HTML.ChatWebView2.html"));

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
                await ExecuteScriptAsync(AssemblyHelper.GetEmbeddedResource("SharedClasses", resource));
            }
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
            var model = settings.ModelList.FirstOrDefault(m => $"{modelString.Split(' ')[0]}" == m.ModelName);
            return model;
        }

        // WebViewCallAndCallbackSystem

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
            var columnData = models.Select(x => new { inputCost = x.input1MTokenPrice.ToString("F2"), outputCost = x.output1MTokenPrice.ToString("F2"), starred = x.Starred });

            // window.setDropdownOptions('mainAI',

            foreach (var dropdown in new[] { "mainAI", "summaryAI" })
            {
                ExecuteScriptAsync($"setDropdownOptions('{dropdown}', {JsonConvert.SerializeObject(modelStrings)}, {JsonConvert.SerializeObject(columnData)});");
            }
        }
        internal async Task SetTools()
        {
            var toolStrings = _toolManager.Tools.Select(x => x.Name.ToString()).ToArray();
            var toolStringsJson = JsonConvert.SerializeObject(toolStrings);

            await ExecuteScriptAsync($"window.setTools({toolStringsJson})");
        }
        internal async Task UpdateSystemPrompt(string systemPrompt) => await ExecuteScriptAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");

        internal async Task AddMessage(CompletionMessage message) =>
            await ExecuteScriptAsync($"AddMessage({JsonConvert.SerializeObject(message)})");

        internal async Task<string> GetSystemPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getSystemPrompt()"));

        internal async Task<string> GetUserPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAsync("getUserPrompt()"));

        internal async Task SetUserPrompt(string content)
        {
            await this.InvokeIfNeeded(async () =>
            {
                await ExecuteScriptAsync($"setUserPrompt({JsonConvert.SerializeObject(content)})");
            });

        }

        internal async Task ConcatenateUserPrompt(string content)
        {
            await this.InvokeIfNeeded(async () =>
            {
                await ExecuteScriptAsync($"setUserPrompt(getUserPrompt()+ ' ' + {JsonConvert.SerializeObject(content)})");
            });

        }

        internal async Task Clear()
        {
                await ExecuteScriptAsync($"ClearMessages()");
                await DisableCancelButton();
                await EnableSendButton();



                
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

        internal async Task UpdateSendButtonColor(bool embeddingsEnabled)
        {
            // an orange version of #4a7c4c would be not #8a5c8c, but #8a7c4c
            var js = embeddingsEnabled ? "window.setSendButtonAlternate(\"Send with Embeddings\", \"#ca8611\");" : "window.setSendButtonAlternate(\"Send\", \"#4a7c4c\");";
            await ExecuteScriptAsync(js);
        }

        internal async void UpdateTemp(string e) => await ExecuteScriptAsync($"appendMessageText('temp-ai-msg', {JsonConvert.SerializeObject(e)}, 1)");

        internal async void ClearTemp() => await ExecuteScriptAsync($"removeMessageByGuid(\"temp-ai-msg\");");

        internal async Task ChangeChatHeaderLabel(string content) => await ExecuteScriptAsync($"changeChatHeaderLabel({JsonConvert.SerializeObject(content)})");

        // end webview interface methods

        private bool IsDesignMode() => DesignMode || LicenseManager.UsageMode == LicenseUsageMode.Designtime;


        private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            ReturnCoreWebView2Request(e, CoreWebView2);
        }

        private static void ReturnCoreWebView2Request(CoreWebView2WebResourceRequestedEventArgs e, CoreWebView2 coreWebView2)
        {
            var rd = AssemblyHelper.GetResourceDetails();
            var matching = rd.Where(x => e.Request.Uri == x.Uri).ToList();


            AssemblyHelper.GetResourceDetails().Where(x => e.Request.Uri.Equals(x.Uri, StringComparison.OrdinalIgnoreCase)).ToList().ForEach
                // (x => ReturnResourceToWebView(e, x.ResourceName, x.MimeType));
                (x =>
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    // if resourcename doesn't exist in that assembly...
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
                                var response = coreWebView2.Environment.CreateWebResourceResponse(memoryStream, 200, "OK", $"Content-Type: {x.MimeType}");
                                e.Response = response;
                                e.Response.Headers.AppendHeader("Access-Control-Allow-Origin", "*");
                                return;
                            }
                        }
                        throw new Exception("Probably forgot to embed the resource :(");
                    }
                }
                );
        }

        internal async Task SetThemes(string themesJson) => await ExecuteScriptAsync($"window.setAllColorSchemes({themesJson})");

        internal async Task SetTheme(string selectedTheme) => await ExecuteScriptAsync($"window.selectColorScheme({selectedTheme})");

        internal async void SetIndicator(string Label, string Colour) => await ExecuteScriptAsync($"addIndicator('{Label}','{Colour}')");

        internal async void ClearIndicator(string Label) => await ExecuteScriptAsync($"clearIndicator('{Label}')");

        internal async Task OpenTemplate(ConversationTemplate template)
        {
            await Clear();
            await UpdateSystemPrompt(template?.SystemPrompt ?? "");
            await SetUserPrompt(template?.InitialPrompt ?? "");
        }

        internal async Task InitialiseApiList(SettingsSet settings)
        {
            await SetModels(settings.ModelList);

            await SetModelForDropdown("mainAI", settings.SelectedModel, settings, m => m.SelectedModel);
            await SetModelForDropdown("summaryAI", settings.SelectedSummaryModel, settings, m => m.SelectedSummaryModel);
        }

        private async Task SetModelForDropdown(string dropdownId, string selectedModel, SettingsSet settings, Expression<Func<SettingsSet, string>> propertySelector)
        {
            if (!string.IsNullOrEmpty(selectedModel))
            {
                var matchingModel = settings.ModelList.FirstOrDefault(m => m.ModelName == selectedModel.Split(' ')[0]);
                await SetDropdownValue(dropdownId, matchingModel.ToString());
            }
            else
            {
                var defaultModel = settings.ModelList.FirstOrDefault(m => m.ModelName.Contains("llama3"));
                await SetDropdownValue(dropdownId, defaultModel.ToString());

                var property = (PropertyInfo)((MemberExpression)propertySelector.Body).Member;
                property.SetValue(settings, defaultModel.ToString());

                SettingsSet.Save(settings);
            }
        }

        internal async Task Initialise(SettingsSet settings, ScratchpadManager scratchpadManager)
        {
            // send color schemes to the chatwebview
            var themesPath = Path.Combine("Settings\\Themes.json");
            if (File.Exists(themesPath))
            {
                await SetThemes(File.ReadAllText(themesPath));
                await SetTheme(settings.SelectedTheme);
            }
            else
            {
                var themesJson = AssemblyHelper.GetEmbeddedResource("AiTool3.Defaults.themes.json");
                await SetThemes(themesJson);
                File.WriteAllText(themesPath, themesJson);
                settings.SelectedTheme = "Serene";
                SettingsSet.Save(settings);
            }

            var scratchpadContent = scratchpadManager.LoadScratchpad();
            if (!string.IsNullOrEmpty(scratchpadContent))
            {
                await ExecuteScriptAsync($"window.setScratchpadContentAndOpen({scratchpadContent})");
            }

            await SetTools();

            await InitialiseApiList(settings);
        }

        internal async Task<string> GetMessagesPaneContent()
        {
            var content = await ExecuteScriptAsync("document.querySelector('.main-content').outerHTML;");

            // decode \u003 etc
            content = System.Text.RegularExpressions.Regex.Unescape(content);

            return content;
        }
    }
}