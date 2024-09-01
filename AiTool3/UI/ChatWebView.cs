using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.Snippets;
using AiTool3.Tools;
using AiTool3.Topics;
using AiTool3.UI.Forms;
using AITool3;
using FFmpeg.AutoGen;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using SharedClasses;
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
        private FileAttachmentManager _fileAttachmentManager;
        private SimpleServer _simpleServer;
        //private TcpCommsManager _tcpCommsManager;

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

        public static readonly MessagePrompt[] MessagePrompts = new[]
{
            // Code Analysis and Explanation
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Explain Code", MessageType = "explainCode", Prompt = "Provide a detailed explanation of what this code does:" },
            new MessagePrompt { Category = "Code Analysis", ButtonLabel = "Identify Potential Bugs", MessageType = "identifyBugs", Prompt = "Analyze this code for potential bugs or edge cases that might cause issues:" },

            // Code Improvement and Refactoring
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Extract Method", MessageType = "extractMethod", Prompt = "Perform an extract method on this:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Extract Static Method", MessageType = "extractStaticMethod", Prompt = "Perform an extract static method on this:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "DRY This", MessageType = "dryThis", Prompt = "Suggest some clever ways, with examples, to DRY this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "General Refactor", MessageType = "generalRefactor", Prompt = "Suggest some clever ways, with examples, to generally refactor this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Improve Performance", MessageType = "improvePerformance", Prompt = "Analyse and, if possible, suggest some clever ways with examples, to improve the performance of this code:" },
            new MessagePrompt { Category = "Refactoring 1", ButtonLabel = "Simplify Logic", MessageType = "simplifyLogic", Prompt = "Analyze and suggest ways to simplify the logic in this code without changing its functionality:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "Convert to LINQ", MessageType = "convertToLinq", Prompt = "Convert this code to use LINQ expressions where appropriate:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "Extract Best Class", MessageType = "extractBestClass", Prompt = "Analyze this code and identify the single best class that could be extracted to improve general Object-Oriented Programming (OOP) principles. Describe the proposed class, its properties, methods, and how it would enhance the overall design:" },
            new MessagePrompt { Category = "Refactoring 2", ButtonLabel = "String Interpolation", MessageType = "stringInterpolation", Prompt = "Rewrite this to use string interpolation:" },
            
            // Code Enhancement
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Error Handling", MessageType = "addErrorHandling", Prompt = "Suggest appropriate error handling mechanisms for this code:" },
            new MessagePrompt { Category = "Enhancement", ButtonLabel = "Add Logging", MessageType = "addLogging", Prompt = "Suggest appropriate logging statements to add to this code for better debugging and monitoring:" },

            // Naming and Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Suggest Name", MessageType = "suggestName", Prompt = "Suggest a concise and descriptive name for this code element:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Commit Message", MessageType = "commitMsg", Prompt = "Give me a short, high-quality, bulleted, tersely-phrased summary for this diff.  Break the changes down by project and category.  Demarcate the summary as a single code block. Do not mention unused categories or insignficiant changes." },

            // Code Generation and Extension
            new MessagePrompt { Category = "Generation", ButtonLabel = "Autocomplete at //! marker", MessageType = "autocompleteThis", Prompt = "Autocomplete this code where you see the marker //! . Give only the inserted text and no other output, demarcated with three ticks before and after." },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Extend Series", MessageType = "addToSeries", Prompt = "Extend the series you see in this code:" },
            new MessagePrompt { Category = "Generation", ButtonLabel = "Create Unit Tests", MessageType = "createUnitTests", Prompt = "Generate unit tests for this code:" },

            // Code Readability
            new MessagePrompt { Category = "Readability", ButtonLabel = "Add Comments", MessageType = "addComments", Prompt = "Add appropriate comments to this code to improve its readability:" },
            new MessagePrompt { Category = "Readability", ButtonLabel = "Remove Comments", MessageType = "removeComments", Prompt = "Remove all comments from this code:" },

            // User Documentation
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Generate README", MessageType = "generateReadme", Prompt = "Generate a comprehensive README.md file for this project based on the code provided:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "Create User Guide", MessageType = "createUserGuide", Prompt = "Create a user guide explaining how to use the functionality implemented in this code:" },
            new MessagePrompt { Category = "Documentation", ButtonLabel = "API Documentation", MessageType = "generateApiDocs", Prompt = "Generate API documentation for the public methods and classes in this code:" },

            new MessagePrompt
{
    Category = "Documentation",
    ButtonLabel = "C# XML Comments",
    MessageType = "generateCSharpXmlComments",
    Prompt = @"You are an AI assistant specialized in creating standards-compliant XML documentation comments for C# code. Analyze the given C# code and generate appropriate XML comments for classes, methods, properties, and other code elements.

Follow these guidelines:
1. Use XML documentation comments starting with ///.
2. Include a <summary> tag for each element, providing a brief description.
3. Use <param> tags for method parameters, describing each parameter.
4. Use <returns> tags for methods that return a value, describing the return value.
5. Use <exception> tags to document exceptions that may be thrown.
6. Use <remarks> tags for additional information when necessary.
7. Use <example> tags to provide usage examples for complex methods or classes.
8. Use <see> and <seealso> tags to reference related code elements.
9. Follow Microsoft's C# documentation style guide for consistency.
10. Ensure that comments are clear, concise, and add value to the code.

Analyze the above C# code and provide appropriate XML documentation comments for the code elements. Do not modify or repeat the original code; give only the comments to go above the relevant method or class. "
},

        };


        public void InjectDependencies(ToolManager toolManager, FileAttachmentManager fileAttachmentManager)
        {
            _toolManager = toolManager;
            _fileAttachmentManager = fileAttachmentManager;
            _simpleServer = new SimpleServer();
            _simpleServer.LineReceived += SimpleServer_LineReceived;
            _simpleServer.StartServer();
        }

        private async void SimpleServer_LineReceived(object? sender, string e)
        {
            var vsixMessage = JsonConvert.DeserializeObject<VsixMessage>(e);

            if(vsixMessage.MessageType == "vsRequestButtons")
            {
                await SendToVsixAsync(new VsixMessage { MessageType = "vsButtons", Content = JsonConvert.SerializeObject(MessagePrompts) });
                return;
            } else if (vsixMessage.MessageType == "setUserPrompt")
            {
                await SetUserPrompt(JsonConvert.DeserializeObject<string>(vsixMessage.Content));
                return;
            }
            else if (vsixMessage.MessageType == "vsShowFileSelector")
            {
                // in maxsaistudio we can do form = new FileSearchForm
                var files = JsonConvert.DeserializeObject<List<string>>(vsixMessage.Content);
                FileSearchForm form = null;

                await Task.Run(async () =>
                {
                    try
                    {
                        form = new FileSearchForm(files);
                        form.AddFilesToInput += async (s, e) =>
                        {
                            // attach files as txt
                            await _fileAttachmentManager.AttachTextFiles(e.ToArray());
                        };
                    }
                    finally
                    {

                    }
                });
                form.Show();

            }
            else await HandleWebReceivedJsonMessageAsync(vsixMessage.Content);
        }

        protected virtual void OnFileDropped(string filename)
        {
            FileDropped?.Invoke(this, filename);
        }

        public async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {

            string jsonMessage = e.WebMessageAsJson;
            await HandleWebReceivedJsonMessageAsync(jsonMessage);
        }

        public async Task HandleWebReceivedJsonMessageAsync(string jsonMessage)
        {
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
                await ExecuteScriptAndSendToVsixAsync(AssemblyHelper.GetEmbeddedResource("SharedClasses", resource));
            }
        }

        public async Task<string> ExecuteScriptAndSendToVsixAsync(string script)
        {
            var obj  = new VsixMessage { MessageType = "webviewJsCall", Content = script };
            // serialize obj
            await _simpleServer.BroadcastLineAsync(JsonConvert.SerializeObject(obj));
            //_tcpCommsManager.EnqueueMessage(new VsixMessage { MessageType = "webviewJsCall", Content = script });
            return await base.ExecuteScriptAsync(script);
        }

        // begin webview interface methods

        // implemented in chatwebview2.html
        internal async Task AddMessages(List<CompletionMessage> parents)
        {
            // run "addMessages" js function
            await ExecuteScriptAndSendToVsixAsync($"ClearMessages()");
            await ExecuteScriptAndSendToVsixAsync($"AddInitialMessages({JsonConvert.SerializeObject(parents)})");
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
            ExecuteScriptAndSendToVsixAsync($"setDropdownValue('{v1}', '{v2}')");
        }
        internal async Task<string> GetDropdownValue(string v)
        {
            return await ExecuteScriptAndSendToVsixAsync($"getDropdownValue('{v}')");
        }


        internal async Task SetModels(List<Model> models)
        {
            var modelStrings = models.Select(x => x.ToString());
            var columnData = models.Select(x => new { inputCost = x.input1MTokenPrice.ToString("F2"), outputCost = x.output1MTokenPrice.ToString("F2"), starred = x.Starred });

            // window.setDropdownOptions('mainAI',

            foreach (var dropdown in new[] { "mainAI", "summaryAI" })
            {
                ExecuteScriptAndSendToVsixAsync($"setDropdownOptions('{dropdown}', {JsonConvert.SerializeObject(modelStrings)}, {JsonConvert.SerializeObject(columnData)});");
            }
        }
        internal async Task SetTools()
        {
            var toolStrings = _toolManager.Tools.Select(x => x.Name.ToString()).ToArray();
            var toolStringsJson = JsonConvert.SerializeObject(toolStrings);

            await ExecuteScriptAndSendToVsixAsync($"window.setTools({toolStringsJson})");
        }
        internal async Task UpdateSystemPrompt(string systemPrompt) => await ExecuteScriptAndSendToVsixAsync($"updateSystemPrompt({JsonConvert.SerializeObject(systemPrompt)})");

        internal async Task AddMessage(CompletionMessage message) =>
            await ExecuteScriptAndSendToVsixAsync($"AddMessage({JsonConvert.SerializeObject(message)})");

        internal async Task<string> GetSystemPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAndSendToVsixAsync("getSystemPrompt()"));

        internal async Task<string> GetUserPrompt() => JsonConvert.DeserializeObject<string>(await ExecuteScriptAndSendToVsixAsync("getUserPrompt()"));

        internal async Task SetUserPrompt(string content)
        {
            await this.InvokeIfNeeded(async () =>
            {
                await ExecuteScriptAndSendToVsixAsync($"setUserPrompt({JsonConvert.SerializeObject(content)})");
            });

        }

        internal async Task ConcatenateUserPrompt(string content)
        {
            await this.InvokeIfNeeded(async () =>
            {
                await ExecuteScriptAndSendToVsixAsync($"setUserPrompt(getUserPrompt()+ ' ' + {JsonConvert.SerializeObject(content)})");
            });

        }

        internal async Task Clear()
        {
                await ExecuteScriptAndSendToVsixAsync($"ClearMessages()");
                await DisableCancelButton();
                await EnableSendButton();



                
        }

        internal async Task DisableCancelButton()
        {

            await ExecuteScriptAndSendToVsixAsync("disableButton('cancelButton')");
            await ExecuteScriptAndSendToVsixAsync("disableCancelButton()");
        }
        internal async Task EnableCancelButton()
        {
            await ExecuteScriptAndSendToVsixAsync("enableButton('cancelButton')");
            await ExecuteScriptAndSendToVsixAsync("enableCancelButton()");
        }
        internal async Task DisableSendButton()
        {
            await ExecuteScriptAndSendToVsixAsync("disableButton('sendButton')");
            await ExecuteScriptAndSendToVsixAsync("disableSendButton()");
        }
        internal async Task EnableSendButton()
        {
            await ExecuteScriptAndSendToVsixAsync("enableButton('sendButton')");
            await ExecuteScriptAndSendToVsixAsync("enableSendButton()");
        }

        internal async Task UpdateSendButtonColor(bool embeddingsEnabled)
        {
            // an orange version of #4a7c4c would be not #8a5c8c, but #8a7c4c
            var js = embeddingsEnabled ? "window.setSendButtonAlternate(\"Send with Embeddings\", \"#ca8611\");" : "window.setSendButtonAlternate(\"Send\", \"#4a7c4c\");";
            await ExecuteScriptAndSendToVsixAsync(js);
        }

        internal async void UpdateTemp(string e) => await ExecuteScriptAndSendToVsixAsync($"appendMessageText('temp-ai-msg', {JsonConvert.SerializeObject(e)}, 1)");

        internal async void ClearTemp() => await ExecuteScriptAndSendToVsixAsync($"removeMessageByGuid(\"temp-ai-msg\");");

        internal async Task ChangeChatHeaderLabel(string content) => await ExecuteScriptAndSendToVsixAsync($"changeChatHeaderLabel({JsonConvert.SerializeObject(content)})");

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

        internal async Task SetThemes(string themesJson) => await ExecuteScriptAndSendToVsixAsync($"window.setAllColorSchemes({themesJson})");

        internal async Task SetTheme(string selectedTheme) => await ExecuteScriptAndSendToVsixAsync($"window.selectColorScheme({selectedTheme})");

        internal async void SetIndicator(string Label, string Colour) => await ExecuteScriptAndSendToVsixAsync($"addIndicator('{Label}','{Colour}')");

        internal async void ClearIndicator(string Label) => await ExecuteScriptAndSendToVsixAsync($"clearIndicator('{Label}')");

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
                await ExecuteScriptAndSendToVsixAsync($"window.setScratchpadContentAndOpen({scratchpadContent})");
            }

            await SetTools();

            await InitialiseApiList(settings);
        }

        internal async Task<string> GetMessagesPaneContent()
        {
            var content = await ExecuteScriptAndSendToVsixAsync("document.querySelector('.main-content').outerHTML;");

            // decode \u003 etc
            content = System.Text.RegularExpressions.Regex.Unescape(content);

            return content;
        }

        internal async Task SendToVsixAsync(VsixMessage vsixMessage)
        {
            await _simpleServer.BroadcastLineAsync(JsonConvert.SerializeObject(vsixMessage));
        }
    }
}