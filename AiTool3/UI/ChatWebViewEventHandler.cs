using AiTool3.Audio;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.AiServices;
using AiTool3.Templates;
using AiTool3.Tools;
using AiTool3.Topics;
using AiTool3.UI.Forms;
using Newtonsoft.Json;
using SharedClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace AiTool3.UI
{
    public class ChatWebViewEventHandler
    {
        private readonly ChatWebView _chatWebView;
        private readonly ConversationManager _conversationManager;
        private readonly FileAttachmentManager _fileAttachmentManager;
        private readonly TemplateManager _templateManager;
        private readonly AiResponseHandler _aiResponseHandler;
        private readonly WebViewManager _webViewManager;
        private readonly DataGridView _dgvConversations;
        private SettingsSet _currentSettings;
        private readonly ToolStripStatusLabel _tokenUsageLabel;
        private readonly AudioRecorderManager _audioRecorderManager;
        private readonly MenuStrip _menuBar;
        private CancellationTokenSource? _cts;
        public readonly Stopwatch Stopwatch;
        private readonly System.Windows.Forms.Timer _updateTimer;
        private readonly MaxsAiStudio _maxsAiStudio;

        public ChatWebViewEventHandler(
            ChatWebView chatWebView,
            ConversationManager conversationManager,
            FileAttachmentManager fileAttachmentManager,
            TemplateManager templateManager,
            AiResponseHandler aiResponseHandler,
            WebViewManager webViewManager,
            DataGridView dgvConversations,
            SettingsSet currentSettings,
            ToolStripStatusLabel tokenUsageLabel,
            AudioRecorderManager audioRecorderManager,
            MenuStrip menuBar,
            MaxsAiStudio maxsAiStudio)
        {
            _chatWebView = chatWebView;
            _conversationManager = conversationManager;
            _fileAttachmentManager = fileAttachmentManager;
            _templateManager = templateManager;
            _aiResponseHandler = aiResponseHandler;
            _webViewManager = webViewManager;
            _dgvConversations = dgvConversations;
            _currentSettings = currentSettings;
            _tokenUsageLabel = tokenUsageLabel;
            _audioRecorderManager = audioRecorderManager;
            _menuBar = menuBar;
            Stopwatch = new Stopwatch();
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 100;
            _updateTimer.Tick += UpdateTimer_Tick!;

            _maxsAiStudio = maxsAiStudio;

            RegisterEvents();
        }

        public void UpdateSettings(SettingsSet newSettings)
        {
            _currentSettings = newSettings;
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (Stopwatch.IsRunning)
            {
                UpdateStatusBarTimer();
            }
        }

        private async void ChatWebView_FileDropped(object sender, string filename)
        {
            await _fileAttachmentManager.FileDropped(filename, _currentSettings);
        }


        private void UpdateStatusBarTimer()
        {
            TimeSpan ts = Stopwatch.Elapsed;
            _tokenUsageLabel.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        }


        private void RegisterEvents()
        {
            var eventMappings = new Dictionary<string, Delegate>
            {
               {"SendMessage", new EventHandler<ChatWebViewSendMessageEventArgs>(ChatWebView_ChatWebViewSendMessageEvent)},
               {"Cancel", new EventHandler<ChatWebViewCancelEventArgs>(ChatWebView_ChatWebViewCancelEvent)},
               {"Copy", new EventHandler<ChatWebViewCopyEventArgs>(ChatWebView_ChatWebViewCopyEvent)},
               {"New", new EventHandler<ChatWebViewNewEventArgs>(ChatWebView_ChatWebViewNewEvent)},
               {"AddBranch", new EventHandler<ChatWebViewAddBranchEventArgs>(ChatWebView_ChatWebViewAddBranchEvent)},
               {"JoinWithPrevious", new EventHandler<ChatWebViewJoinWithPreviousEventArgs>(ChatWebView_ChatWebViewJoinWithPreviousEvent)},
               {"DropdownChanged", new EventHandler<ChatWebViewDropdownChangedEventArgs>(ChatWebView_ChatWebDropdownChangedEvent)},
               {"Simple", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewSimpleEvent)},
               {"Continue", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewContinueEvent)},
               {"Ready", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewReadyEvent)}
            };

            foreach (var mapping in eventMappings)
            { var t = typeof(ChatWebView);
                var u = t.GetEvent($"ChatWebView{mapping.Key}Event");
                u.AddEventHandler(_chatWebView, mapping.Value);
            }

            _chatWebView.FileDropped += ChatWebView_FileDropped;
        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e)
        {

            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            Stopwatch.Restart();
            _updateTimer.Start();

            try
            {
                await _chatWebView.DisableSendButton();
                await _chatWebView.EnableCancelButton();
            }
            catch (Exception ex)
            {
            }

            _dgvConversations.Enabled = false;
            _webViewManager.Disable();


            await _aiResponseHandler.FetchAiInputResponse(_currentSettings, _cts.Token, e.SelectedTools, overrideUserPrompt: e.OverrideUserPrompt, sendSecondary: e.SendViaSecondaryAI, addEmbeddings: e.AddEmbeddings, prefill: e.Prefill,

                updateUiMethod: async (response) =>
                {
                    _updateTimer.Stop();

                    await UpdateUi(response);

                });

            EnableConversationsAndWebView();

        }

        private async void ChatWebView_ChatWebViewCancelEvent(object? sender, ChatWebViewCancelEventArgs e)
        {
            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            await _chatWebView.EnableSendButton();
            await _chatWebView.DisableCancelButton();
            _updateTimer.Stop();

            EnableConversationsAndWebView();
        }

        private async void ChatWebView_ChatWebDropdownChangedEvent(object? sender, ChatWebViewDropdownChangedEventArgs e)
        {
            _currentSettings.SetModelFromDropdownValue(e.Dropdown, e.ModelString);

            if (e.Dropdown == "mainAI")
            {
                await _chatWebView.UpdatePrefillUI(_currentSettings.GetModel().SupportsPrefill);
            }

        }

        private async void ChatWebView_ChatWebViewReadyEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await _chatWebView.Initialise(_currentSettings);

            _maxsAiStudio.Activate();
            _maxsAiStudio.BringToFront();
        }


        private async void ChatWebView_ChatWebViewContinueEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await _aiResponseHandler.FetchAiInputResponse(_currentSettings, _cts.Token, null, "Continue from PRECISELY THE CHARACTER where you left off.  Do not restart or repeat anything.  Demarcate your output with three backticks.",
                updateUiMethod: (response) =>
                {
                    UpdateUi(response);
                });

            _conversationManager.ContinueUnterminatedCodeBlock(e);

            await WebNdcDrawNetworkDiagram();

            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(e.Guid));
        }

        private async void ChatWebView_ChatWebViewSimpleEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            var apiModel = _currentSettings.GetSummaryModel() ?? _currentSettings.GetModel();
            
            var aiService = AiServiceResolver.GetAiService(ServiceProvider.GetProviderForGuid(_currentSettings.ServiceProviders, apiModel.ProviderGuid).ServiceName, null);

            switch (e.EventType)
            {
                case "ContinueExternalCompletion":
                    {

                        var cm2 = new ConversationManager();
                        cm2.InjectDepencencies(_dgvConversations);
                        cm2.LoadConversation(e.Guid);
                        var inputText2 = "I don't know.";
                        var systemPrompt2 = "";
                        cm2.MostRecentCompletion = cm2.Conversation.Messages.Last();
                        var conversation2 = await cm2.PrepareConversationData(apiModel, systemPrompt2, inputText2, _fileAttachmentManager);

                        var service = ServiceProvider.GetProviderForGuid(_currentSettings.ServiceProviders, apiModel.ProviderGuid);

                        var response2 = await aiService.FetchResponse(service.ApiKey, service.Url, apiModel.ModelName, conversation2, null, null, new CancellationToken(false), _currentSettings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);
                        cm2.AddInputAndResponseToConversation(response2, apiModel, conversation2, inputText2, systemPrompt2, out var completionInput2, out var completionResponse2);
                        cm2.SaveConversation();

                        await cm2.RegenerateSummary(_dgvConversations, cm2.Conversation.ConvGuid, _currentSettings, "(from external source)");
                        cm2.SaveConversation();
                        await _chatWebView.SendCompletionResultsToVsixAsync(response2, cm2.Conversation.ConvGuid);
                        break;
                    }
                case "RunExternalCompletion":
                    {
                        var cm = new ConversationManager();
                        cm.InjectDepencencies(_dgvConversations);
                        var inputText = JsonConvert.DeserializeObject<string>(e.Json);
                        var systemPrompt = "";
                        cm.BeginNewConversation();
                        var conversation = await cm.PrepareConversationData(apiModel, "", inputText, _fileAttachmentManager);

                        var service = ServiceProvider.GetProviderForGuid(_currentSettings.ServiceProviders, apiModel.ProviderGuid);

                        var response = await aiService.FetchResponse(service.ApiKey, service.Url, apiModel.ModelName, conversation, null, null, new CancellationToken(false), _currentSettings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);
                        cm.AddInputAndResponseToConversation(response, apiModel, conversation, inputText, systemPrompt, out var completionInput, out var completionResponse);


                        cm.SaveConversation();
                        _dgvConversations.InvokeIfNeeded(() => _dgvConversations.Rows.Insert(0, cm.Conversation.ConvGuid,
                                cm.Conversation.Messages[0].Content,
                                cm.Conversation.Messages[0].ModelGuid,
                                ""));


                        await cm.RegenerateSummary(_dgvConversations, cm.Conversation.ConvGuid, _currentSettings, "(from external source)");
                        cm.SaveConversation();
                        var guid = cm.Conversation.ConvGuid;

                        await _chatWebView.SendCompletionResultsToVsixAsync(response, guid);
                    }
                    break;
                case "RunMerge":
                    {


                        string responseText = "";

                        try
                        {
                            // AI merge: Apply this JSON changeset and give me the complete entire file verbatim as a single code block with no other output.  Do not include line numbers.  Do not omit any code.  NEVER "// ... (rest of ...) ..." nor similar.
                            // Gemini Flash 2 - or 1.5, but not 8b - seems to do well with the above prompt.  3.5 Haiku crapped out.


                            Conversation conversation = new Conversation(DateTime.Now);
                            conversation.systemprompt = "You are a coding expert who merges changes into original source files.";
                            conversation.messages = new List<ConversationMessage>
                            {
                                new ConversationMessage { role = "user", content = JsonConvert.DeserializeObject<string>(e.Json) }
                            };

                            var service = ServiceProvider.GetProviderForGuid(_currentSettings.ServiceProviders, apiModel.ProviderGuid);

                            var response = await aiService.FetchResponse(service.ApiKey, service.Url, apiModel.ModelName, conversation, null, null, new CancellationToken(false), _currentSettings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);
                            await _chatWebView.SendMergeResultsToVsixAsync(response);
                        }
                        catch (Exception e2)
                        {
                        }


                    }

                    break;
                case "vsButtons":
                    await _chatWebView.SendToVsixAsync(new VsixMessage { MessageType = "vsButtons", Content = JsonConvert.SerializeObject(_currentSettings.MessagePrompts) });
                    break;
                case "toggleModelStar":
                    // deser e.Json to dynamic
                    var json = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(e.Json);
                    var model = _currentSettings.GetModelByNameAndApi(json["modelName"].ToString());
                    if (model != null)
                    {
                        model.Starred = json["isStarred"].ToString() == "true";
                        SettingsSet.Save(_currentSettings);
                    }
                    break;
                case "importTemplate":
                    ImportTemplateAndRecreateMenus(e.Json);
                    break;
                case "allThemes":
                    // persist e.Json to settings subdirectory Themes.json
                    var themes = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(e.Json);
                    var themesPath = Path.Combine("Settings\\Themes.json");
                    File.WriteAllText(themesPath, themes.ToString());
                    break;
                case "selectTheme":
                    _currentSettings.SelectedTheme = e.Json;
                    SettingsSet.Save(_currentSettings);
                    break;
                case "attach":



                    await _fileAttachmentManager.HandleAttachment(_chatWebView, _maxsAiStudio, _currentSettings);

                    break;
                case "voice":
                    if (!_audioRecorderManager.IsRecording)
                    {
                        await _audioRecorderManager.StartRecording();
                        //buttonStartRecording.BackColor = Color.Red;
                        //buttonStartRecording.Text = "Stop\r\nRecord";
                    }
                    else
                    {
                        await _audioRecorderManager.StopRecording();
                        //buttonStartRecording.BackColor = Color.Black;
                        //buttonStartRecording.Text = "Start\r\nRecord";
                    }
                    break;
                case "project":
                    _maxsAiStudio.ShowWorking("Scanning files", _currentSettings.SoftwareToyMode);

                    FileSearchForm form = null;

                    try
                    {
                        form = new FileSearchForm(_currentSettings.DefaultPath, _currentSettings.ProjectHelperFileExtensions);
                        form.AddFilesToInput += async (s, e) =>
                        {
                            // attach files as txt
                            await _fileAttachmentManager.AttachTextFiles(e.ToArray());

                        };
                        form.Show();
                    }
                    finally
                    {

                    }
                    _maxsAiStudio.HideWorking();

                    break;
            }
        }
        private void ImportTemplateAndRecreateMenus(string jsonContent)
        {
            try
            {
                if (_templateManager.ImportTemplate(jsonContent))
                {
                    MenuHelper.RemoveOldTemplateMenus(_menuBar);
                    MenuHelper.CreateTemplatesMenu(_menuBar, _chatWebView, _templateManager, _currentSettings, _maxsAiStudio);
                    MessageBox.Show("Template imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ChatWebView_ChatWebViewJoinWithPreviousEvent(object? sender, ChatWebViewJoinWithPreviousEventArgs e)
        {
            _conversationManager.MergeWithPrevious(e.GuidValue);

            // update the webndc
            WebNdcDrawNetworkDiagram();

        }

        private async Task<bool> WebNdcDrawNetworkDiagram() => await _webViewManager.DrawNetworkDiagram(_conversationManager.Conversation.Messages, _currentSettings.ModelList);

        private void ChatWebView_ChatWebViewAddBranchEvent(object? sender, ChatWebViewAddBranchEventArgs e)
        {
            var newNodeGuid = _conversationManager.AddBranch(e);
            if (newNodeGuid == null) return;
            // update the webndc
            WebNdcDrawNetworkDiagram();

            // select the new node
            //webViewManager.CentreOnNode(newNodeGuid);

            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(newNodeGuid));
        }

        public async void WebViewNdc_WebNdcNodeClicked(object? sender, WebNdcNodeClickedEventArgs e)
        {
            var clickedCompletion = _conversationManager.Conversation!.Messages.FirstOrDefault(c => c.Guid == e.NodeId);
            if (clickedCompletion == null)
                return;
            _conversationManager.MostRecentCompletion = clickedCompletion;

            var parents = _conversationManager.GetParentNodeList();

            // if the clicked completion is a user message, remove it from parents
            if (clickedCompletion.Role == CompletionRole.User)
            {
                parents.Reverse();
                parents = parents.Skip(1).ToList();
                parents.Reverse();
            }

            await _chatWebView.AddMessages(parents);

            if (_conversationManager.MostRecentCompletion.Role == CompletionRole.User)
            {
                if (_conversationManager.MostRecentCompletion.Base64Type != null)
                {
                    _fileAttachmentManager.SetBase64(_conversationManager.MostRecentCompletion.Base64Image, _conversationManager.MostRecentCompletion.Base64Type);
                }

                await _chatWebView.SetUserPrompt(_conversationManager.MostRecentCompletion.Content!, _conversationManager.MostRecentCompletion.Base64Image, _conversationManager.MostRecentCompletion.Base64Type);

                _conversationManager.MostRecentCompletion = _conversationManager.Conversation.FindByGuid(_conversationManager.MostRecentCompletion.Parent!);
            }
            else
            {
                await _chatWebView.SetUserPrompt("");
            }

            await _chatWebView.UpdateSystemPrompt(_conversationManager.MostRecentCompletion.SystemPrompt!);
        }


        private async void ChatWebView_ChatWebViewNewEvent(object? sender, ChatWebViewNewEventArgs e)
        {
            switch (e.Type)
            {
                case ChatWebViewNewType.New:
                    await Clear();
                    break;
                case ChatWebViewNewType.NewWithPrompt:
                    await BeginNewConversationPreserveInputAndSystemPrompts();
                    break;
                case ChatWebViewNewType.NewWithContext:
                    await BeginNewConversationPreserveContext();
                    break;


            }
        }

        private async Task BeginNewConversationPreserveContext()
        {
            _conversationManager.GetConversationContext(out CompletionMessage lastAssistantMessage, out CompletionMessage lastUserMessage);

            await BeginNewConversationPreserveInputAndSystemPrompts();

            CompletionMessage assistantMessage, userMessage;

            _conversationManager.CreateNewConversationFromUserAssistantPair(lastAssistantMessage, lastUserMessage, out assistantMessage, out userMessage);

            await _chatWebView.AddMessage(userMessage);
            await _chatWebView.AddMessage(assistantMessage);

            await WebNdcDrawNetworkDiagram();
        }


        private async Task Clear()
        {
            await BeginNewConversation();
            await _chatWebView.SetUserPrompt("");
            await _maxsAiStudio.PopulateUiForTemplate(_templateManager.CurrentTemplate!);
        }


        private async Task BeginNewConversationPreserveInputAndSystemPrompts()
        {
            var currentPrompt = await _chatWebView.GetUserPrompt();
            var currentSystemPrompt = await _chatWebView.GetSystemPrompt();
            await BeginNewConversation();
            await _chatWebView.UpdateSystemPrompt(currentSystemPrompt);
            await _chatWebView.SetUserPrompt(currentPrompt);
        }

        public async Task BeginNewConversation()
        {
            await _chatWebView.Clear();
            _webViewManager.Enable();
            _fileAttachmentManager.ClearBase64();
            _conversationManager.BeginNewConversation();

            await WebNdcDrawNetworkDiagram();
        }

        private void ChatWebView_ChatWebViewCopyEvent(object? sender, ChatWebViewCopyEventArgs e) => Clipboard.SetText(e.Content);

        public void EnableConversationsAndWebView()
        {
            _dgvConversations.Enabled = true;
            _webViewManager.Enable();
        }

        private async Task UpdateUi(AiResponse response)
        {
            var cost = _currentSettings.GetModel().GetCost(response.TokenUsage);

            _tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out ";

            // response.TokenUsage.OutputTokens * 1000 / elapsed.Value.TotalMilliseconds to 2dp is 
            _tokenUsageLabel.Text += $" at {(response.TokenUsage.OutputTokens * 1000 / response.Duration.TotalMilliseconds).ToString("F2")} tokens per second";

            if (response.TokenUsage.CacheCreationInputTokens > 0)
            {
                _tokenUsageLabel.Text += $" ; {response.TokenUsage.CacheCreationInputTokens} cache creation tokens";
            }
            if (response.TokenUsage.CacheReadInputTokens > 0)
            {
                _tokenUsageLabel.Text += $" ; {response.TokenUsage.CacheReadInputTokens} cache read tokens";
            }

            if (response.TokenUsage.CacheCreationInputTokens > 0 || response.TokenUsage.CacheReadInputTokens > 0)
            {
                var actualInputTokens = response.TokenUsage.InputTokens + response.TokenUsage.CacheCreationInputTokens + response.TokenUsage.CacheReadInputTokens;
                var convertedCachedInputTokens = response.TokenUsage.InputTokens + response.TokenUsage.CacheCreationInputTokens * 1.25m + response.TokenUsage.CacheReadInputTokens * 0.1m;

                // "Used 33% more tokens than without caching"
                var percentage = (int)((convertedCachedInputTokens / actualInputTokens) * 100) - 100;


                _tokenUsageLabel.Text += $" ; this request used {percentage}% {(percentage > 0 ? "more" : "less")} tokens because of caching";

            }

            var row = _dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == _conversationManager.Conversation.ConvGuid);

            if (row == null)
            {
                _dgvConversations.Rows.Insert(0, _conversationManager.Conversation.ConvGuid, _conversationManager.Conversation.Messages[0].Content, _conversationManager.Conversation.Messages[0].ModelGuid, "");
            }
        }
    }
}