using AiTool3.Audio;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.Snippets;
using AiTool3.Templates;
using AiTool3.Tools;
using AiTool3.Topics;
using AiTool3.UI;
using AiTool3.UI.Forms;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using SharedClasses;
using SharedClasses.Helpers;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Whisper.net.Ggml;

namespace AiTool3
{
    public partial class MaxsAiStudio : Form
    {
        // injected dependencies
        private ToolManager _toolManager;
        private SnippetManager _snippetManager;
        private SearchManager _searchManager;
        private FileAttachmentManager _fileAttachmentManager;
        private TemplateManager _templateManager;
        private ScratchpadManager _scratchpadManager;
        private AiResponseHandler _aiResponseHandler;
        public static readonly decimal Version = 0.3m;

        public static readonly string ThreeTicks = new string('`', 3);

        public ConversationManager ConversationManager;
        public SettingsSet CurrentSettings
        {
            get;
            set;
        }

        private CancellationTokenSource? _cts, _cts2;
        private WebViewManager? webViewManager = null;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private AudioRecorderManager audioRecorderManager;

        public string selectedConversationGuid = "";

        public MaxsAiStudio(ToolManager toolManager,
                            SnippetManager snippetManager,
                            SearchManager searchManager,
                            FileAttachmentManager fileAttachmentManager,
                            ConversationManager conversationManager,
                            TemplateManager templateManager,
                            ScratchpadManager scratchpadManager,
                            AiResponseHandler aiResponseHandler)
        {
            SplashManager splashManager = new SplashManager();
            splashManager.ShowSplash();

            try
            {
                

                DirectoryHelper.CreateSubdirectories();

                _templateManager = templateManager;

                CurrentSettings = AiTool3.SettingsSet.LoadOrPromptOnFirstRun();

                InitializeComponent();

                UIThreadHelper.Initialize();

                _toolManager = toolManager;
                _snippetManager = snippetManager;
                _searchManager = searchManager;
                _searchManager.SetDgv(dgvConversations);
                _fileAttachmentManager = fileAttachmentManager;
                _fileAttachmentManager.InjectDependencies(chatWebView);
                ConversationManager = conversationManager;
                ConversationManager.InjectDepencencies(dgvConversations);
                chatWebView.InjectDependencies(toolManager, fileAttachmentManager);
                _scratchpadManager = scratchpadManager;
                _aiResponseHandler = aiResponseHandler;
                webViewManager = new WebViewManager(ndcWeb);
                _aiResponseHandler.InjectDependencies(chatWebView, webViewManager);

                splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

                RegisterChatWebViewEvents();

                splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

                audioRecorderManager = new AudioRecorderManager(GgmlType.SmallEn, chatWebView);
                audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

                splitContainer1.Paint += new PaintEventHandler(SplitContainerHelper.SplitContainer_Paint!);
                splitContainer5.Paint += new PaintEventHandler(SplitContainerHelper.SplitContainer_Paint!);

                dgvConversations.InitialiseDataGridView(RegenerateSummary, DeleteConversation);

                InitialiseMenus();

                updateTimer.Interval = 100;
                updateTimer.Tick += UpdateTimer_Tick!;

                Load += OnLoad!;

                dgvConversations.MouseDown += DgvConversations_MouseDown;

            }
            finally
            {
                splashManager.CloseSplash();
            }
        }

        private async void ChatWebView_ChatWebViewReadyEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await chatWebView.Initialise(CurrentSettings, _scratchpadManager);

            this.Activate();
            this.BringToFront();
        }


        private void RegisterChatWebViewEvents()
        {
            var eventMappings = new Dictionary<string, Delegate>
            {
                {"SendMessage", new EventHandler<ChatWebViewSendMessageEventArgs>(ChatWebView_ChatWebViewSendMessageEvent)},
                {"Cancel", new EventHandler<ChatWebViewCancelEventArgs>(ChatWebView_ChatWebViewCancelEvent)},
                {"Copy", new EventHandler<ChatWebViewCopyEventArgs>(ChatWebView_ChatWebViewCopyEvent)},
                {"New", new EventHandler<ChatWebViewNewEventArgs>(ChatWebView_ChatWebViewNewEvent)},
                {"AddBranch", new EventHandler<ChatWebViewAddBranchEventArgs>(ChatWebView_ChatWebViewAddBranchEvent)},
                {"JoinWithPrevious", new EventHandler<ChatWebViewJoinWithPreviousEventArgs>(ChatWebView_ChatWebViewJoinWithPreviousEvent)},
                {"DropdownChanged", new EventHandler<ChatWebDropdownChangedEventArgs>(ChatWebView_ChatWebDropdownChangedEvent)},
                {"Simple", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewSimpleEvent)},
                {"Continue", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewContinueEvent)},
                {"Ready", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewReadyEvent)}
            };
            
            foreach(var mapping in eventMappings)
            {
                typeof(ChatWebView).GetEvent($"ChatWebView{mapping.Key}Event")
                    .AddEventHandler(chatWebView, mapping.Value);
            }

            // Handle FileDropped separately since it doesn't follow the same naming pattern
            chatWebView.FileDropped += ChatWebView_FileDropped;
        }


        private async void ChatWebView_ChatWebViewContinueEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await _aiResponseHandler.FetchAiInputResponse(CurrentSettings, _cts.Token, null, "Continue from PRECISELY THE CHARACTER where you left off.  Do not restart or repeat anything.  Demarcate your output with three backticks.",
                updateUiMethod: (response) =>
                {
                    UpdateUi(response);
                });

            ConversationManager.ContinueUnterminatedCodeBlock(e);

            await WebNdcDrawNetworkDiagram();

            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(e.Guid));
        }

        private void ImportTemplateAndRecreateMenus(string jsonContent)
        {
            try
            {
                if (_templateManager.ImportTemplate(jsonContent))
                {
                    MenuHelper.RemoveOldTemplateMenus(menuBar);
                    MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, _templateManager, CurrentSettings, this);
                    MessageBox.Show("Template imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing template: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private async void ChatWebView_ChatWebViewSimpleEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            switch (e.EventType)
            {
                case "vsButtons":
                    await chatWebView.SendToVsixAsync(new VsixMessage { MessageType = "vsButtons", Content = JsonConvert.SerializeObject(CurrentSettings.MessagePrompts) });
                    break;
                case "toggleModelStar":
                    // deser e.Json to dynamic
                    var json = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(e.Json);
                    var model = CurrentSettings.GetModelByNameAndApi(json["modelName"].ToString());
                    if(model != null)
                    {
                        model.Starred = json["isStarred"].ToString() == "true";
                        SettingsSet.Save(CurrentSettings);
                    }
                    break;
                case "importTemplate":
                    ImportTemplateAndRecreateMenus(e.Json);
                    break;
                case "saveScratchpad":
                    _scratchpadManager.SaveScratchpad(e.Json);
                    break;
                case "allThemes":
                    // persist e.Json to settings subdirectory Themes.json
                    var themes = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(e.Json);
                    var themesPath = Path.Combine("Settings\\Themes.json");
                    File.WriteAllText(themesPath, themes.ToString());
                    break;
                case "selectTheme":
                    CurrentSettings.SelectedTheme = e.Json;
                    SettingsSet.Save(CurrentSettings);
                    break;
                case "attach":



                    await _fileAttachmentManager.HandleAttachment(chatWebView, this, CurrentSettings);

                    break;
                case "voice":
                    if (!audioRecorderManager.IsRecording)
                    {
                        await audioRecorderManager.StartRecording();
                        //buttonStartRecording.BackColor = Color.Red;
                        //buttonStartRecording.Text = "Stop\r\nRecord";
                    }
                    else
                    {
                        await audioRecorderManager.StopRecording();
                        //buttonStartRecording.BackColor = Color.Black;
                        //buttonStartRecording.Text = "Start\r\nRecord";
                    }
                    break;
                case "project":
                    this.ShowWorking("Scanning files", CurrentSettings.SoftwareToyMode);

                    FileSearchForm form = null;

                        try
                        {
                            form = new FileSearchForm(CurrentSettings.DefaultPath, CurrentSettings.ProjectHelperFileExtensions);
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
                    this.HideWorking();
                    
                    break;
            }
        }



        private void ChatWebView_ChatWebViewJoinWithPreviousEvent(object? sender, ChatWebViewJoinWithPreviousEventArgs e)
        {
            ConversationManager.MergeWithPrevious(e.GuidValue);

            // update the webndc
            WebNdcDrawNetworkDiagram();

        }

        private void ChatWebView_ChatWebViewAddBranchEvent(object? sender, ChatWebViewAddBranchEventArgs e)
        {
            var newNodeGuid = ConversationManager.AddBranch(e);
            if (newNodeGuid == null) return;
            // update the webndc
            WebNdcDrawNetworkDiagram();

            // select the new node
            //webViewManager.CentreOnNode(newNodeGuid);

            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(newNodeGuid));
        }

        private async void DeleteConversation(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete this conversation?", "Delete Conversation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // are we deleting the current conversation? if so, start a new one.
                if (selectedConversationGuid == ConversationManager.Conversation.ConvGuid)
                {
                    await BeginNewConversation();
                }

                BranchedConversation.DeleteConversation(selectedConversationGuid);

                dgvConversations.RemoveConversation(selectedConversationGuid);
            }
        }

        private async Task InitialiseMenus()
        {
            var fileMenu = MenuHelper.CreateMenu("File");
            var editMenu = MenuHelper.CreateMenu("Edit");

            new List<ToolStripMenuItem> { fileMenu, editMenu }.ForEach(menu => menuBar.Items.Add(menu));

            MenuHelper.CreateMenuItem("Quit", ref fileMenu).Click += (s, e) => Application.Exit();

            MenuHelper.CreateMenuItem("Settings", ref editMenu).Click += async (s, e) =>
            {
                CurrentSettings = await SettingsSet.OpenSettingsForm(chatWebView, CurrentSettings);
            };

            MenuHelper.CreateMenuItem("Licenses", ref editMenu).Click += (s, e) => new LicensesForm(AssemblyHelper.GetEmbeddedResource("AiTool3.UI.Licenses.txt")).ShowDialog();

            await MenuHelper.CreateSpecialsMenu(menuBar, CurrentSettings, chatWebView, _snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager, this);
            await MenuHelper.CreateEmbeddingsMenu(this, menuBar, CurrentSettings, chatWebView, _snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager);

            MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, _templateManager, CurrentSettings, this);

            await VersionHelper.CheckForUpdate(menuBar);
        }



        private async void ChatWebView_FileDropped(object sender, string filename)
        {
            await _fileAttachmentManager.FileDropped(filename, CurrentSettings);
        }

        private async Task FileDropped(string filename)
        {

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


        private async void OnLoad(object sender, EventArgs e)
        {
            Load -= OnLoad!;

            await chatWebView.EnsureCoreWebView2Async(null);

            await BeginNewConversation();

            await webViewManager.CreateNewWebNdc(false, WebViewNdc_WebNdcContextMenuOptionSelected, WebViewNdc_WebNdcNodeClicked);

            this.BringToFront();

            // Create things in Ready instead...

        }

        private void DgvConversations_MouseDown(object? sender, MouseEventArgs e)
        {
            dgvConversations.SetConversationForDgvClick(ref selectedConversationGuid, e);
        }



        private async void RegenerateSummary(object sender, EventArgs e) =>
            await ConversationManager.RegenerateSummary(await chatWebView.GetDropdownModel("summaryAI", CurrentSettings), dgvConversations, selectedConversationGuid, CurrentSettings);

        private async void ChatWebView_ChatWebViewCancelEvent(object? sender, ChatWebViewCancelEventArgs e)
        {
            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            await chatWebView.EnableSendButton();
            await chatWebView.DisableCancelButton();
            updateTimer.Stop();

            EnableConversationsAndWebView();
        }

        private void EnableConversationsAndWebView()
        {
            dgvConversations.Enabled = true;
            webViewManager.Enable();
        }

        private void ChatWebView_ChatWebViewCopyEvent(object? sender, ChatWebViewCopyEventArgs e) => Clipboard.SetText(e.Content);

        private async void AutoSuggestStringSelected(string selectedString) => await chatWebView.SetUserPrompt(selectedString);

        private void WebViewNdc_WebNdcContextMenuOptionSelected(object? sender, WebNdcContextMenuOptionSelectedEventArgs e)
        {
            if (e.MenuOption == "editRaw")
            {
                var messageGuid = e.Guid;
                var message = ConversationManager.Conversation.Messages.FirstOrDefault(m => m.Guid == messageGuid);
                if (message != null)
                {
                    using (var form = new EditRawMessageForm(message.Content))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            message.Content = form.EditedContent;
                            ConversationManager.SaveConversation();
                            WebNdcDrawNetworkDiagram();
                            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(messageGuid));
                        }
                    }
                }
            }
            else
            {
                WebNdcRightClickLogic.ProcessWebNdcContextMenuOption(ConversationManager.GetParentNodeList(), e.MenuOption);
            }
        }


        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (stopwatch.IsRunning)
            {
                UpdateStatusBarTimer();
            }
        }

        private void UpdateStatusBarTimer()
        {
            TimeSpan ts = stopwatch.Elapsed;
            tokenUsageLabel.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        }

        private async void AudioRecorderManager_AudioProcessed(object? sender, string e) => await chatWebView.ConcatenateUserPrompt(e);




        private async void WebViewNdc_WebNdcNodeClicked(object? sender, WebNdcNodeClickedEventArgs e)
        {
            var clickedCompletion = ConversationManager.Conversation!.Messages.FirstOrDefault(c => c.Guid == e.NodeId);
            if (clickedCompletion == null)
                return;
            ConversationManager.MostRecentCompletion = clickedCompletion;

            var parents = ConversationManager.GetParentNodeList();

            // if the clicked completion is a user message, remove it from parents
            if (clickedCompletion.Role == CompletionRole.User)
            {
                parents.Reverse();
                parents = parents.Skip(1).ToList();
                parents.Reverse();
            }

            await chatWebView.AddMessages(parents);

            if (ConversationManager.MostRecentCompletion.Role == CompletionRole.User)
            {
                if (ConversationManager.MostRecentCompletion.Base64Type != null)
                {
                    _fileAttachmentManager.SetBase64(ConversationManager.MostRecentCompletion.Base64Image, ConversationManager.MostRecentCompletion.Base64Type);
                }

                await chatWebView.SetUserPrompt(ConversationManager.MostRecentCompletion.Content!, ConversationManager.MostRecentCompletion.Base64Image, ConversationManager.MostRecentCompletion.Base64Type);

                ConversationManager.MostRecentCompletion = ConversationManager.Conversation.FindByGuid(ConversationManager.MostRecentCompletion.Parent!);
            }
            else
            {
                await chatWebView.SetUserPrompt("");
            }

            await chatWebView.UpdateSystemPrompt(ConversationManager.MostRecentCompletion.SystemPrompt!);
        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e)
        {

            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            stopwatch.Restart();
            updateTimer.Start();

            try
            {
                await chatWebView.DisableSendButton();
                await chatWebView.EnableCancelButton();
            }
            catch (Exception ex)
            {
            }

            dgvConversations.Enabled = false;
            webViewManager.Disable();


            await _aiResponseHandler.FetchAiInputResponse(CurrentSettings, _cts.Token, e.SelectedTools, overrideUserPrompt: e.OverrideUserPrompt, sendSecondary: e.SendViaSecondaryAI, addEmbeddings: e.AddEmbeddings, prefill: e.Prefill,
                    
                updateUiMethod: async (response) =>
                {
                    updateTimer.Stop();
                        
                    await UpdateUi(response);

                });

            EnableConversationsAndWebView();
            
        }

        private async Task UpdateUi(AiResponse response)
        {
            var cost = CurrentSettings.GetModel().GetCost(response.TokenUsage);

            tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out ";

                // response.TokenUsage.OutputTokens * 1000 / elapsed.Value.TotalMilliseconds to 2dp is 
                tokenUsageLabel.Text += $" at {(response.TokenUsage.OutputTokens * 1000 / response.Duration.TotalMilliseconds).ToString("F2")} tokens per second";

            if(response.TokenUsage.CacheCreationInputTokens > 0)
            {
                tokenUsageLabel.Text += $" ; {response.TokenUsage.CacheCreationInputTokens} cache creation tokens";
            }
            if(response.TokenUsage.CacheReadInputTokens > 0)
            {
                tokenUsageLabel.Text += $" ; {response.TokenUsage.CacheReadInputTokens} cache read tokens";
            }

            if(response.TokenUsage.CacheCreationInputTokens > 0 || response.TokenUsage.CacheReadInputTokens > 0)
            {
                var actualInputTokens = response.TokenUsage.InputTokens + response.TokenUsage.CacheCreationInputTokens + response.TokenUsage.CacheReadInputTokens;
                var convertedCachedInputTokens = response.TokenUsage.InputTokens + response.TokenUsage.CacheCreationInputTokens * 1.25m + response.TokenUsage.CacheReadInputTokens * 0.1m;

                // "Used 33% more tokens than without caching"
                var percentage = (int)((convertedCachedInputTokens / actualInputTokens) * 100)-100;


                tokenUsageLabel.Text += $" ; this request used {percentage}% {(percentage>0 ? "more" : "less")} tokens because of caching";

            }

            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.Conversation.ConvGuid);

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.Conversation.ConvGuid, ConversationManager.Conversation.Messages[0].Content, ConversationManager.Conversation.Messages[0].Engine, "");
            }
        }

        private async Task<bool> WebNdcDrawNetworkDiagram() => await webViewManager.DrawNetworkDiagram(ConversationManager.Conversation.Messages);



        private async Task Clear()
        {
            await BeginNewConversation();
            await chatWebView.SetUserPrompt("");
            await PopulateUiForTemplate(_templateManager.CurrentTemplate!);
        }


        private async Task BeginNewConversationPreserveInputAndSystemPrompts()
        {
            var currentPrompt = await chatWebView.GetUserPrompt();
            var currentSystemPrompt = await chatWebView.GetSystemPrompt();
            await BeginNewConversation();
            await chatWebView.UpdateSystemPrompt(currentSystemPrompt);
            await chatWebView.SetUserPrompt(currentPrompt);
        }


        private async Task BeginNewConversationPreserveContext()
        {
            ConversationManager.GetConversationContext(out CompletionMessage lastAssistantMessage, out CompletionMessage lastUserMessage);

            await BeginNewConversationPreserveInputAndSystemPrompts();

            CompletionMessage assistantMessage, userMessage;

            ConversationManager.CreateNewConversationFromUserAssistantPair(lastAssistantMessage, lastUserMessage, out assistantMessage, out userMessage);

            await chatWebView.AddMessage(userMessage);
            await chatWebView.AddMessage(assistantMessage);

            await WebNdcDrawNetworkDiagram();
        }

        private async Task BeginNewConversation()
        {
            await chatWebView.Clear();
            webViewManager.Enable();
            _fileAttachmentManager.ClearBase64();
            ConversationManager.BeginNewConversation();

            await WebNdcDrawNetworkDiagram();
        }

        private async void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var clickedGuid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();

                ConversationManager.LoadConversation(clickedGuid!);

                if (ConversationManager.Conversation.GetRootNode() != null)
                {
                    WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(ConversationManager.Conversation.GetRootNode()?.Guid));
                }

                await WebNdcDrawNetworkDiagram();
            }
            catch
            {
                return;
            }
        }

        public async Task PopulateUiForTemplate(ConversationTemplate template)
        {
            EnableConversationsAndWebView();

            await chatWebView.OpenTemplate(template);
        }


        private async void tbSearch_TextChanged(object sender, EventArgs e) => await _searchManager.PerformSearch(tbSearch.Text);

        public static CancellationTokenSource ResetCancellationtoken(CancellationTokenSource? cts)
        {
            cts?.Cancel();
            return new CancellationTokenSource();
        }

        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            tbSearch.Clear();
            _searchManager.ClearSearch();
        }

        private void MaxsAiStudio_FormClosing(object sender, FormClosingEventArgs e) => webViewManager!.webView.Dispose();

        private void button1_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;

            CurrentSettings.CollapseConversationPane = splitContainer1.Panel1Collapsed;
            SettingsSet.Save(CurrentSettings);
            button1.Text = splitContainer1.Panel1Collapsed ? @">
>
>" : @"<
<
<";
        }

        private async void ChatWebView_ChatWebDropdownChangedEvent(object? sender, ChatWebDropdownChangedEventArgs e)
        {
            CurrentSettings.SetModelFromDropdownValue(e.Dropdown, e.ModelString);

            if(e.Dropdown == "mainAI")
            {
                await chatWebView.UpdatePrefillUI(CurrentSettings.GetModel().SupportsPrefill);
            }

        }



        private void chatWebView_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        }
    }
}