using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json;
using System.Data;
using Microsoft.CodeAnalysis;
using AiTool3.Audio;
using AiTool3.Snippets;
using Whisper.net.Ggml;
using AiTool3.Providers;
using AiTool3.Helpers;
using FontAwesome.Sharp;
using AiTool3.ExtensionMethods;
using System.Windows.Forms;
using AiTool3.Settings;
using System.Diagnostics;
using AiTool3.Tools;
using System.Text;

namespace AiTool3
{
    public partial class MaxsAiStudio : Form
    {
        public static MaxsAiStudio MaxRef;

        private SnippetManager snippetManager = new SnippetManager();
        private FileAttachmentManager _fileAttachmentManager;
        private ToolManager toolManager = new ToolManager();

        private SearchManager _searchManager;

        public static readonly string ThreeTicks = new string('`', 3);

        public TemplateManager templateManager = new TemplateManager();
        public ConversationManager ConversationManager { get; set; } = new ConversationManager();
        public SettingsSet CurrentSettings { get; set; } = AiTool3.SettingsSet.Load()!;

        private CancellationTokenSource? _cts, _cts2;
        private WebViewManager? webViewManager = null;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private AudioRecorderManager audioRecorderManager;

        public string selectedConversationGuid = "";
        public MaxsAiStudio()
        {
            MaxsAiStudio.MaxRef = this;

            InitializeComponent();

            

            DirectoryHelper.CreateSubdirectories();

            splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

            webViewManager = new WebViewManager(ndcWeb);

            cbUseEmbeddings.Checked = CurrentSettings.UseEmbeddings;
            cbUseEmbeddings.CheckedChanged += CbUseEmbeddings_CheckedChanged;

            chatWebView.ChatWebViewSendMessageEvent += ChatWebView_ChatWebViewSendMessageEvent;
            chatWebView.ChatWebViewCancelEvent += ChatWebView_ChatWebViewCancelEvent;
            chatWebView.ChatWebViewCopyEvent += ChatWebView_ChatWebViewCopyEvent;
            chatWebView.ChatWebViewNewEvent += ChatWebView_ChatWebViewNewEvent;
            chatWebView.ChatWebViewAddBranchEvent += ChatWebView_ChatWebViewAddBranchEvent;
            chatWebView.ChatWebViewJoinWithPreviousEvent += ChatWebView_ChatWebViewJoinWithPreviousEvent;
            chatWebView.ChatWebDropdownChangedEvent += ChatWebView_ChatWebDropdownChangedEvent;
            chatWebView.ChatWebViewSimpleEvent += ChatWebView_ChatWebViewSimpleEvent;
            chatWebView.ChatWebViewContinueEvent += ChatWebView_ChatWebViewContinueEvent;
            chatWebView.ChatWebViewReadyEvent += ChatWebView_ChatWebViewReadyEvent;
            chatWebView.FileDropped += ChatWebView_FileDropped;

            splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

            audioRecorderManager = new AudioRecorderManager(GgmlType.SmallEn, chatWebView);
            audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

            splitContainer1.Paint += new PaintEventHandler(SplitContainer_Paint!);
            splitContainer5.Paint += new PaintEventHandler(SplitContainer_Paint!);

            DataGridViewHelper.InitialiseDataGridView(dgvConversations);

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Regenerate Summary", null, RegenerateSummary);

            contextMenu.Items.Add(new ToolStripSeparator());
            var noHighlightItem = new ToolStripMenuItem("Clear Highlight");

            foreach (var colour in new Color[] { Color.LightBlue, Color.LightGreen, Color.LightPink, Color.LightYellow, Color.LightCoral, Color.LightCyan })
            {
                var item = new ToolStripMenuItem(colour.ToString().Replace("Color [", "Highlight in ").Replace("]", ""));

                // add a colour swatch to the item (!)
                var bmp = new System.Drawing.Bitmap(16, 16);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(colour);

                    // add 1px solid black border
                    g.DrawRectangle(System.Drawing.Pens.Black, 0, 0, bmp.Width - 1, bmp.Height - 1);

                }

                item.Image = bmp;



                item.Click += (s, e) =>
                {
                    var conv = BranchedConversation.LoadConversation(selectedConversationGuid);
                    conv.HighlightColour = colour;
                    conv.SaveConversation();

                    // find the dgv row
                    foreach (DataGridViewRow row in dgvConversations.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == selectedConversationGuid)
                        {
                            row.DefaultCellStyle.BackColor = colour;
                            row.DefaultCellStyle.ForeColor = Color.Black;
                            break;
                        }
                    }
                };
                contextMenu.Items.Add(item);

            }

            // add a split and no-highlight option which sets conv.highlightcolour to null and updates the row

            noHighlightItem.Click += (s, e) =>
            {
                var conv = BranchedConversation.LoadConversation(selectedConversationGuid);
                conv.HighlightColour = null;
                conv.SaveConversation();

                // find the dgv row
                foreach (DataGridViewRow row in dgvConversations.Rows)
                {
                    if (row.Cells[0].Value.ToString() == selectedConversationGuid)
                    {
                        row.DefaultCellStyle.BackColor = Color.Black;
                        row.DefaultCellStyle.ForeColor = Color.White;
                        break;
                    }
                }
            };

            contextMenu.Items.Add(new ToolStripSeparator());

            contextMenu.Items.Add(noHighlightItem);

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete conversation", null, DeleteConversation);

            dgvConversations.ContextMenuStrip = contextMenu;

            InitialiseMenus();

            updateTimer.Interval = 100;
            updateTimer.Tick += UpdateTimer_Tick!;

            Load += OnHandleCreated!;

            dgvConversations.MouseDown += DgvConversations_MouseDown;

            _searchManager = new SearchManager(dgvConversations);
            _fileAttachmentManager = new FileAttachmentManager(chatWebView, CurrentSettings);



        }

        private async void ChatWebView_ChatWebViewReadyEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await InitialiseApiList_New();

            // send color schemes to the chatwebview
            var themesPath = Path.Combine("Settings\\Themes.json");
            if (File.Exists(themesPath))
            {
                await chatWebView.SetThemes(File.ReadAllText(themesPath));

                if (!string.IsNullOrWhiteSpace(CurrentSettings.SelectedTheme))
                {
                    await chatWebView.SetTheme(CurrentSettings.SelectedTheme);
                }
            }

            // if there isn't a scratchpad file but there is a scratchpad bak file, rename the bak file to scratchpad.json
            if (File.Exists(Path.Combine("Settings", "Scratchpad.json.bak")) && !File.Exists(Path.Combine("Settings", "Scratchpad.json")))
            {
                File.Move(Path.Combine("Settings", "Scratchpad.json.bak"), Path.Combine("Settings", "Scratchpad.json"));
            }

            if (File.Exists(Path.Combine("Settings", "Scratchpad.json")))
            {
                var scratchpadContent = File.ReadAllText(Path.Combine("Settings", "Scratchpad.json"));

                //already JSON-encoded

                await chatWebView.ExecuteScriptAsync($"window.setScratchpadContentAndOpen({scratchpadContent})");


            }

            await chatWebView.SetTools(toolManager.Tools);
        }

        private async void ChatWebView_ChatWebViewContinueEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await FetchAiInputResponse(null, "Continue from where you left off.  Do not restart or repeat anything.  Demarcate your output with three backticks.");

            ConversationManager.ContinueUnterminatedCodeBlock(e);

            // update the webndc
            WebNdcDrawNetworkDiagram();

            // select the new node
            //webViewManager.CentreOnNode(newNodeGuid);

            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(e.Guid));
        }

        private async void ChatWebView_ChatWebViewSimpleEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            switch (e.EventType)
            {
                case "saveScratchpad":
                    // persist e.Json to settings subdirectory Scratchpad.json
                    if (e.Json == "")
                        return;
                    var scratchpadPath = Path.Combine("Settings\\Scratchpad.json");

                    // enocde string as json
                    var json = JsonConvert.SerializeObject(e.Json);

                    // if there's an existing bak file, delete it
                    if (File.Exists(scratchpadPath + ".bak"))
                    {
                        File.Delete(scratchpadPath + ".bak");
                    }

                    // if there's an existing file, rename it .json.bak
                    if (File.Exists(scratchpadPath))
                    {
                        File.Move(scratchpadPath, scratchpadPath + ".bak");
                    }

                    File.WriteAllText(scratchpadPath, json);
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
                    await _fileAttachmentManager.HandleAttachment(chatWebView);
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
                    var form = new FileSearchForm(CurrentSettings.DefaultPath, CurrentSettings.ProjectHelperFileExtensions);
                    form.AddFilesToInput += async (s, e) =>
                    {
                        // attach files as txt
                        await _fileAttachmentManager.AttachTextFiles(e.ToArray());

                    };
                    form.Show();
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

                // Delete the conversation
                BranchedConversation.DeleteConversation(selectedConversationGuid);

                foreach (DataGridViewRow row in dgvConversations.Rows)
                {
                    if (row.Cells[0].Value.ToString() == selectedConversationGuid)
                    {
                        dgvConversations.Rows.Remove(row);
                        break;
                    }
                }
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
                var settingsForm = new SettingsForm(CurrentSettings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    CurrentSettings = settingsForm.NewSettings;
                    SettingsSet.Save(CurrentSettings);
                    cbUseEmbeddings.Checked = CurrentSettings.UseEmbeddings;
                    await InitialiseApiList_New();
                }
            };

            MenuHelper.CreateMenuItem("Set Embeddings File", ref editMenu).Click += (s, e) => EmbeddingsHelper.HandleSetEmbeddingsFileClick(CurrentSettings);
            MenuHelper.CreateMenuItem("Licenses", ref editMenu).Click += (s, e) => new LicensesForm(AssemblyHelper.GetEmbeddedAssembly("AiTool3.UI.Licenses.txt")).ShowDialog();

            await MenuHelper.CreateSpecialsMenu(menuBar, CurrentSettings, chatWebView, snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager, this);
            await MenuHelper.CreateEmbeddingsMenu(this, menuBar, CurrentSettings, chatWebView, snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager);

            MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, templateManager, CurrentSettings, this);
        }
        private async void ChatWebView_FileDropped(object sender, string filename)
        {
            // convert file:/// uri to filepath and name

            // if it's an HTTP filename...
            if (filename.StartsWith("http"))
            {
                var textFromUrl = await HtmlTextExtractor.ExtractTextFromUrlAsync(filename);

                var quotedFile = HtmlTextExtractor.QuoteFile(filename, textFromUrl);

                // prepend to existing cwv user input
                var currentPrompt = await chatWebView.GetUserPrompt();
                await chatWebView.SetUserPrompt($"{quotedFile}{Environment.NewLine}{currentPrompt}");



                return;
            }


            var uri = new Uri(filename);
            filename = uri.LocalPath;

            try
            {

                var classification = FileTypeClassifier.GetFileClassification(Path.GetExtension(filename));

                switch (classification)
                {
                    case FileTypeClassifier.FileClassification.Video:
                    case FileTypeClassifier.FileClassification.Audio:
                        await _fileAttachmentManager.TranscribeMP4(filename, chatWebView);
                        break;
                    case FileTypeClassifier.FileClassification.Image:
                        await _fileAttachmentManager.AttachImage(filename);
                        break;
                    default:
                        await _fileAttachmentManager.AttachTextFiles(new string[] { filename });
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                    await NewKeepContext();
                    break;


            }
        }

        private async void CbUseEmbeddings_CheckedChanged(object? sender, EventArgs e)
        {
            CurrentSettings.UseEmbeddings = cbUseEmbeddings.Checked;
            SettingsSet.Save(CurrentSettings);
        }

        private async void OnHandleCreated(object sender, EventArgs e)
        {
            Load -= OnHandleCreated!;

            await chatWebView.EnsureCoreWebView2Async(null);

            await BeginNewConversation();

            await CreateNewWebNdc(CurrentSettings.ShowDevTools);

            // Create things in Ready instead...

        }

        private async Task InitialiseApiList_New()
        {


            await chatWebView.SetModels(CurrentSettings.ModelList);

            if (CurrentSettings.SelectedModel != "")
            {
                await chatWebView.SetDropdownValue("mainAI", CurrentSettings.SelectedModel.ToString());
            }
            else await chatWebView.SetDropdownValue("mainAI", CurrentSettings.ModelList.FirstOrDefault(m => m.ServiceName.StartsWith("Local")).ToString());

            if (CurrentSettings.SelectedSummaryModel != "")
            {
                await chatWebView.SetDropdownValue("summaryAI", CurrentSettings.SelectedSummaryModel.ToString());
            }
            else await chatWebView.SetDropdownValue("summaryAI", CurrentSettings.ModelList.FirstOrDefault(m => m.ServiceName.StartsWith("Local")).ToString());

        }

        private void DgvConversations_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hti = dgvConversations.HitTest(e.X, e.Y);
                if (hti.RowIndex >= 0)
                {
                    if (!ModifierKeys.HasFlag(Keys.Control))
                    {
                        dgvConversations.ClearSelection();
                    }
                    dgvConversations.Rows[hti.RowIndex].Selected = true;
                    selectedConversationGuid = dgvConversations.Rows[hti.RowIndex].Cells[0].Value.ToString();
                }
            }
        }

        private async void RegenerateSummary(object sender, EventArgs e) =>
            await ConversationManager.RegenerateSummary(await chatWebView.GetDropdownModel("summaryAI", CurrentSettings), dgvConversations, selectedConversationGuid, CurrentSettings);

        private async void ChatWebView_ChatWebViewCancelEvent(object? sender, ChatWebViewCancelEventArgs e)
        {
            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            await chatWebView.EnableSendButton();
            await chatWebView.DisableCancelButton();

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


        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (stopwatch.IsRunning)
            {
                TimeSpan ts = stopwatch.Elapsed;
                tokenUsageLabel.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            }
        }

        private async void AudioRecorderManager_AudioProcessed(object? sender, string e) => await chatWebView.ConcatenateUserPrompt(e);



        private void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            SplitContainer sc = (sender as SplitContainer)!;

            Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(200, 200, 200)))
            {
                e.Graphics.FillRectangle(brush, splitterRect);
            }
        }


        private async void WebViewNdc_WebNdcNodeClicked(object? sender, WebNdcNodeClickedEventArgs e)
        {
            var clickedCompletion = ConversationManager.Conversation!.Messages.FirstOrDefault(c => c.Guid == e.NodeId);
            if (clickedCompletion == null)
                return;
            ConversationManager.PreviousCompletion = clickedCompletion;

            var parents = ConversationManager.GetParentNodeList();

            // if the clicked completion is a user message, remove it from parents
            if (clickedCompletion.Role == CompletionRole.User)
            {
                parents.Reverse();
                parents = parents.Skip(1).ToList();
                parents.Reverse();
            }

            await chatWebView.AddMessages(parents);

            string systemPrompt = "";
            systemPrompt = ConversationManager.PreviousCompletion.SystemPrompt!;
            if (ConversationManager.PreviousCompletion.Role == CompletionRole.User)
            {
                await chatWebView.SetUserPrompt(ConversationManager.PreviousCompletion.Content!);
                ConversationManager.PreviousCompletion = ConversationManager.Conversation.FindByGuid(ConversationManager.PreviousCompletion.Parent!);
            }
            else
            {
                await chatWebView.SetUserPrompt("");
            }
            //await chatWebView.ChangeChatHeaderLabel(ConversationManager.PreviousCompletion.Engine);
            await chatWebView.UpdateSystemPrompt(systemPrompt);




        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e)
        {
            await FetchAiInputResponse(e.SelectedTools, toolManager: toolManager);
        }


        private async Task<string> FetchAiInputResponse(List<string> toolIDs = null, string? overrideUserPrompt = null, ToolManager toolManager = null)
        {
            toolIDs = toolIDs ?? new List<string>();
            string retVal = "";
            try
            {
                PrepareForNewResponse();

                var model = await chatWebView.GetDropdownModel("mainAI", CurrentSettings);

                var conversation = await ConversationManager.PrepareConversationData(model, await chatWebView.GetSystemPrompt(), overrideUserPrompt != null ? overrideUserPrompt : await chatWebView.GetUserPrompt(), _fileAttachmentManager);
                var response = await FetchAndProcessAiResponse(conversation, model, toolIDs, overrideUserPrompt, toolManager);
                retVal = response.ResponseText;
                await chatWebView.SetUserPrompt("");
                await chatWebView.DisableCancelButton();

                dgvConversations.Enabled = true;
                webViewManager.Enable();

                await chatWebView.EnableSendButton();

                if (overrideUserPrompt == null)
                {
                    stopwatch.Stop();
                    updateTimer.Stop();
                    await UpdateUi(response);
                    await UpdateConversationSummary();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex is OperationCanceledException ? "Operation was cancelled." : $"An error occurred: {ex.Message}");

                chatWebView.ClearTemp();
                _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            }
            finally
            {
                await chatWebView.DisableCancelButton();

                dgvConversations.Enabled = true;
                webViewManager.Enable();

                await chatWebView.EnableSendButton();


            }
            return retVal;
        }



        private async void PrepareForNewResponse()
        {
            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            stopwatch.Restart();
            updateTimer.Start();

            await chatWebView.DisableSendButton();
            await chatWebView.EnableCancelButton();

            dgvConversations.Enabled = false;
            webViewManager.Disable();
        }



        private async Task<AiResponse> FetchAndProcessAiResponse(Conversation conversation, Model model, List<string> toolIDs, string? overrideUserPrompt, ToolManager toolManager)
        {
            var aiService = AiServiceResolver.GetAiService(model.ServiceName);
            aiService.StreamingTextReceived += AiService_StreamingTextReceived;
            aiService.StreamingComplete += (s, e) => { chatWebView.InvokeIfNeeded(() => chatWebView.ClearTemp()); };

            toolIDs = toolIDs.Where(x => int.TryParse(x, out _)).ToList();

            var toolLabels = toolIDs.Select(t => toolManager.Tools[int.Parse(t)].Name).ToList();

            var response = await aiService!.FetchResponse(model, conversation, _fileAttachmentManager.Base64Image!, _fileAttachmentManager.Base64ImageType!, _cts.Token, CurrentSettings, mustNotUseEmbedding: false, toolNames: toolLabels, useStreaming: CurrentSettings.StreamResponses, toolManager);

            if (toolManager != null && toolIDs.Any())
            {
                var tool = toolManager.GetToolByLabel(toolLabels[0]);

                var sb = new StringBuilder($"{ThreeTicks}{tool.OutputFilename}\n");

                //if (model.ServiceName == "OpenAI")
                //{
                //    sb.Append( "{");
                //}

                sb.Append(response.ResponseText.Replace("\r", "").Replace("\n", " "));

                //if (model.ServiceName == "OpenAI")
                //{
                //    sb.Append("}");
                //}

                sb.Append($"\n{ThreeTicks}\n");

                response.ResponseText = sb.ToString();
            }

            var modelUsageManager = new ModelUsageManager(model);

            modelUsageManager.AddTokensAndSave(response.TokenUsage);

            // update the chatwebview, conversation manager, and webndc
            await ProcessAiResponse(response, model, conversation, overrideUserPrompt);

            return response;
        }

        private void AiService_StreamingTextReceived(object? sender, string e) => chatWebView.InvokeIfNeeded(() => chatWebView.UpdateTemp(e));

        private async Task ProcessAiResponse(AiResponse response, Model model, Conversation conversation, string? overrideUserPrompt)
        {

            var inputText = await chatWebView.GetUserPrompt();
            var systemPrompt = await chatWebView.GetSystemPrompt();
            var elapsed = stopwatch.Elapsed;

            CompletionMessage completionInput, completionResponse;
            ConversationManager.AddInputAndResponseToConversation(response, model, conversation, overrideUserPrompt == null ? inputText : overrideUserPrompt, systemPrompt, elapsed, out completionInput, out completionResponse);

            _fileAttachmentManager.ClearBase64();

            // don't bother updating the UI if we're overriding the user prompt, because we're doing an auto continue
            if (overrideUserPrompt != null)
            {
                return;
            }


            if (CurrentSettings.NarrateResponses)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => TtsHelper.ReadAloud(response.ResponseText));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            await chatWebView.AddMessage(completionInput);
            await chatWebView.AddMessage(completionResponse);
            await WebNdcDrawNetworkDiagram();
            webViewManager!.CentreOnNode(completionResponse.Guid);
        }


        private async Task UpdateUi(AiResponse response)
        {
            if (response.SuggestedNextPrompt != null)
            {
                await chatWebView.SetUserPrompt(response.SuggestedNextPrompt);
            }

            var model = await chatWebView.GetDropdownModel("mainAI", CurrentSettings);
            var cost = model.GetCost(response.TokenUsage);

            tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out";

            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.Conversation.ConvGuid);

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.Conversation.ConvGuid, ConversationManager.Conversation.Messages[0].Content, ConversationManager.Conversation.Messages[0].Engine, "");
            }
        }

        private async Task UpdateConversationSummary()
        {

            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.Conversation.ConvGuid);

            if (row != null && row.Cells[3].Value != null && string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
            {
                await ConversationManager.GenerateConversationSummary(CurrentSettings);
                row.Cells[3].Value = ConversationManager.Conversation.ToString();

            }

            ConversationManager.SaveConversation();
        }

        private async Task<bool> WebNdcDrawNetworkDiagram()
        {
            if (webViewManager == null || webViewManager.webView.CoreWebView2 == null) return false;

            var a = await webViewManager.Clear();

            var nodes = ConversationManager.Conversation!.Messages
                .Where(x => x.Role != CompletionRole.Root)
                .Select(m => new IdNodeRole { id = m.Guid!, label = m.Content!, role = m.Role.ToString(), colour = m.GetColorHexForEngine() }).ToList();

            var links2 = ConversationManager.Conversation.Messages
                .Where(x => x.Parent != null)
                .Select(x => new Link { source = x.Parent!, target = x.Guid! }).ToList();


            await webViewManager.EvaluateJavascriptAsync($"addNodes({JsonConvert.SerializeObject(nodes)});");
            await webViewManager.EvaluateJavascriptAsync($"addLinks({JsonConvert.SerializeObject(links2)});");
            return true;
        }




        private async Task Clear()
        {
            await BeginNewConversation();
            await chatWebView.SetUserPrompt("");
            await PopulateUiForTemplate(templateManager.CurrentTemplate!);
        }


        private async Task BeginNewConversationPreserveInputAndSystemPrompts()
        {
            var currentPrompt = await chatWebView.GetUserPrompt();
            var currentSystemPrompt = await chatWebView.GetSystemPrompt();
            await BeginNewConversation();
            await chatWebView.UpdateSystemPrompt(currentSystemPrompt);
            await chatWebView.SetUserPrompt(currentPrompt);
        }

        private async Task NewKeepContext()
        {
            var lastAssistantMessage = ConversationManager.PreviousCompletion;
            var lastUserMessage = ConversationManager.Conversation!.FindByGuid(lastAssistantMessage!.Parent!);
            if (lastUserMessage == null)
                return;
            if (lastAssistantMessage.Role == CompletionRole.User)
                lastAssistantMessage = ConversationManager.Conversation.FindByGuid(ConversationManager.PreviousCompletion!.Parent!);

            await BeginNewConversationPreserveInputAndSystemPrompts();

            var assistantMessage = new CompletionMessage(CompletionRole.Assistant)
            {
                Parent = null,
                Content = lastAssistantMessage.Content,
                Engine = lastAssistantMessage.Engine,

                CreatedAt = DateTime.Now,
            };

            var rootMessage = ConversationManager.Conversation.GetRootNode();

            var userMessage = new CompletionMessage(CompletionRole.User)
            {
                Parent = rootMessage.Guid,
                Content = lastUserMessage.Content,
                Engine = lastUserMessage.Engine,

                CreatedAt = DateTime.Now,
            };
            rootMessage.Children!.Add(userMessage.Guid);
            assistantMessage.Parent = userMessage.Guid;
            userMessage.Children.Add(assistantMessage.Guid);
            //await BeginNewConversation();
            ConversationManager.Conversation.Messages.AddRange(new[] { userMessage, assistantMessage });
            ConversationManager.PreviousCompletion = assistantMessage;

            // update ui
            await chatWebView.AddMessage(userMessage);
            await chatWebView.AddMessage(assistantMessage);

            await WebNdcDrawNetworkDiagram();
        }

        private async Task BeginNewConversation()
        {
            await chatWebView.Clear(CurrentSettings);

            dgvConversations.Enabled = true;
            webViewManager.Enable();

            ConversationManager.Conversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            ConversationManager.Conversation.AddNewRoot();
            ConversationManager.PreviousCompletion = ConversationManager.Conversation.Messages.First();

            await WebNdcDrawNetworkDiagram();
        }

        private async void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var clickedGuid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();

            ConversationManager.LoadConversation(clickedGuid!);

            if (ConversationManager.Conversation.GetRootNode() != null)
            {
                WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(ConversationManager.Conversation.GetRootNode()?.Guid));
            }

            await WebNdcDrawNetworkDiagram();

        }

        public async Task PopulateUiForTemplate(ConversationTemplate template)
        {
            await chatWebView.Clear(CurrentSettings);

            dgvConversations.Enabled = true;
            webViewManager.Enable();

            await chatWebView.UpdateSystemPrompt(template?.SystemPrompt ?? "");
            await chatWebView.SetUserPrompt(template?.InitialPrompt ?? "");
        }


        private async void tbSearch_TextChanged(object sender, EventArgs e)
        {
            await _searchManager.PerformSearch(tbSearch.Text);
        }

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

        private async Task CreateNewWebNdc(bool showDevTools)
        {
            await webViewManager.CreateNewWebNdc(showDevTools, WebViewNdc_WebNdcContextMenuOptionSelected, WebViewNdc_WebNdcNodeClicked);
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

        private void ChatWebView_ChatWebDropdownChangedEvent(object? sender, ChatWebDropdownChangedEventArgs e)
        {
            if (e.Dropdown == "mainAI")
            {
                CurrentSettings.SelectedModel = e.ModelString;
                SettingsSet.Save(CurrentSettings);
            }
            else if (e.Dropdown == "summaryAI")
            {
                CurrentSettings.SelectedSummaryModel = e.ModelString;
                SettingsSet.Save(CurrentSettings);
            }
        }

        private void chatWebView_DragDrop(object sender, DragEventArgs e)
        {
            // get the name of the dropped file

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        }

        private async void btnProjectHelper_Click(object sender, EventArgs e)
        {
            var form = new FileSearchForm(CurrentSettings.DefaultPath, CurrentSettings.ProjectHelperFileExtensions);
            form.AddFilesToInput += async (s, e) =>
            {
                // attach files as txt
                await _fileAttachmentManager.AttachTextFiles(e.ToArray());

            };
            form.Show();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            this.ShowWorking("button clicked", CurrentSettings.SoftwareToyMode);
            dgvConversations.ShowWorking("button clicked", CurrentSettings.SoftwareToyMode);
            await Task.Delay(10000); // 10000 milliseconds = 10 seconds
            this.HideWorking();
            dgvConversations.HideWorking();
        }
    }
}