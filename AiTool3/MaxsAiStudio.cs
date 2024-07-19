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

namespace AiTool3
{
    public partial class MaxsAiStudio : Form
    {
        private SnippetManager snippetManager = new SnippetManager();
        private FileAttachmentManager _fileAttachmentManager;

        private SearchManager _searchManager;

        public static readonly string ThreeTicks = new string('`', 3);

        public TemplateManager templateManager = new TemplateManager();
        public ConversationManager ConversationManager { get; set; } = new ConversationManager();
        public SettingsSet CurrentSettings { get; set; } = AiTool3.SettingsSet.Load()!;

        private CancellationTokenSource? _cts, _cts2;
        private WebViewManager? webViewManager = null;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private AudioRecorderManager audioRecorderManager = new AudioRecorderManager(GgmlType.SmallEn);

        public string selectedConversationGuid = "";
        public MaxsAiStudio()
        {
            InitializeComponent();

            splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

            webViewManager = new WebViewManager(ndcWeb);

            cbUseEmbeddings.Checked = CurrentSettings.UseEmbeddings;
            cbUseEmbeddings.CheckedChanged += CbUseEmbeddings_CheckedChanged;

            chatWebView.ChatWebViewSendMessageEvent += ChatWebView_ChatWebViewSendMessageEvent;
            chatWebView.ChatWebViewCancelEvent += ChatWebView_ChatWebViewCancelEvent;
            chatWebView.ChatWebViewCopyEvent += ChatWebView_ChatWebViewCopyEvent;
            chatWebView.ChatWebViewNewEvent += ChatWebView_ChatWebViewNewEvent;
            chatWebView.FileDropped += ChatWebView_FileDropped;

            splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

            audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

            ButtonIconHelper.SetButtonIcon(IconChar.Paperclip, buttonAttachImage);

            InitialiseApiList();

            splitContainer1.Paint += new PaintEventHandler(SplitContainer_Paint!);
            splitContainer5.Paint += new PaintEventHandler(SplitContainer_Paint!);

            DataGridViewHelper.InitialiseDataGridView(dgvConversations);

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Regenerate Summary", null, RegenerateSummary);

            contextMenu.Items.Add(new ToolStripSeparator());
            var noHighlightItem = new ToolStripMenuItem("Clear Highlight");


            // add menu items for the six pastel Colors you can mark a summary with
            //foreach (var colour in new Color[] { Color.LightBlue, Color.LightGreen, Color.LightPink, Color.LightYellow, C })
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

            dgvConversations.ContextMenuStrip = contextMenu;

            InitialiseMenus();

            updateTimer.Interval = 100;
            updateTimer.Tick += UpdateTimer_Tick!;

            Load += OnHandleCreated!;

            dgvConversations.MouseDown += DgvConversations_MouseDown;

            _searchManager = new SearchManager(dgvConversations);
            _fileAttachmentManager = new FileAttachmentManager(chatWebView, CurrentSettings);



        }
        private void InitialiseMenus()
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
                    await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);
                }
            };

            MenuHelper.CreateMenuItem("Set Embeddings File", ref editMenu).Click += (s, e) => EmbeddingsHelper.HandleSetEmbeddingsFileClick(CurrentSettings);
            MenuHelper.CreateMenuItem("Licenses", ref editMenu).Click += (s, e) => new LicensesForm(AssemblyHelper.GetEmbeddedAssembly("AiTool3.UI.Licenses.txt")).ShowDialog();
            MenuHelper.CreateSpecialsMenu(menuBar, CurrentSettings, (Model)cbSummaryEngine.SelectedItem!, chatWebView, snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager);
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
            await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);
        }

        private async void OnHandleCreated(object sender, EventArgs e)
        {
            Load -= OnHandleCreated!;

            await chatWebView.EnsureCoreWebView2Async(null);


           // chatWebView.ShowWorking();

            await CreateNewWebNdc(CurrentSettings.ShowDevTools);

            await BeginNewConversation();

            await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);
           // Task.Delay(5000).ContinueWith(t => chatWebView.HideWorking(), TaskScheduler.FromCurrentSynchronizationContext());
            if (CurrentSettings.RunWebServer)
            {
                await WebServerHelper.CreateWebServerAsync(chatWebView, FetchAiInputResponse);
            }



            // in 5 sec, HideWorking
            

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

        private async void RegenerateSummary(object sender, EventArgs e) => await ConversationManager.RegenerateSummary((Model)cbSummaryEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, selectedConversationGuid, CurrentSettings);

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
            => WebNdcRightClickLogic.ProcessWebNdcContextMenuOption(ConversationManager.GetParentNodeList(), e.MenuOption);


        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (stopwatch.IsRunning)
            {
                TimeSpan ts = stopwatch.Elapsed;
                tokenUsageLabel.Text = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            }
        }

        private async void AudioRecorderManager_AudioProcessed(object? sender, string e) => await chatWebView.SetUserPrompt(e);

        private void InitialiseApiList()
        {

            foreach (var model in CurrentSettings.ApiList!.SelectMany(x => x.Models))
            {
                cbEngine.Items.Add(model);
                cbSummaryEngine.Items.Add(model);
            }
            if (CurrentSettings.SelectedModel != "")
            {
                cbEngine.SelectedItem = cbEngine.Items.Cast<Model>().FirstOrDefault(m => m.ToString() == CurrentSettings.SelectedModel);
            }
            else cbEngine.SelectedItem = cbEngine.Items.Cast<Model>().FirstOrDefault(m => m.ServiceName.StartsWith("Local"));
            if (CurrentSettings.SelectedSummaryModel != "")
            {
                cbSummaryEngine.SelectedItem = cbSummaryEngine.Items.Cast<Model>().FirstOrDefault(m => m.ToString() == CurrentSettings.SelectedSummaryModel);
            }
            else cbSummaryEngine.SelectedItem = cbSummaryEngine.Items.Cast<Model>().FirstOrDefault(m => m.ServiceName.StartsWith("Local"));
        }

        private void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            SplitContainer sc = (sender as SplitContainer)!;

            Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

            using (SolidBrush brush = new SolidBrush(Color.PaleTurquoise))
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

            var parents = ConversationManager.GetParentNodeList();

            await chatWebView.AddMessages(parents);

            await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);
        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e) => await FetchAiInputResponse();



        private async Task<string> FetchAiInputResponse()
        {
            string retVal = "";
            try
            {
                PrepareForNewResponse();
                var model = (Model)cbEngine.SelectedItem!;
                var conversation = await ConversationManager.PrepareConversationData(model, await chatWebView.GetSystemPrompt(), await chatWebView.GetUserPrompt(), _fileAttachmentManager);
                var response = await FetchAndProcessAiResponse(conversation, model);
                retVal = response.ResponseText;
                await chatWebView.SetUserPrompt("");
                await chatWebView.DisableCancelButton();

                dgvConversations.Enabled = true;
                webViewManager.Enable();

                await chatWebView.EnableSendButton();
                await UpdateUi(response);
                await UpdateConversationSummary();
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
                stopwatch.Stop();
                updateTimer.Stop();

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



        private async Task<AiResponse> FetchAndProcessAiResponse(Conversation conversation, Model model)
        {
            var aiService = AiServiceResolver.GetAiService(model.ServiceName);
            aiService.StreamingTextReceived += AiService_StreamingTextReceived;
            aiService.StreamingComplete += (s, e) => { chatWebView.InvokeIfNeeded(() => chatWebView.ClearTemp()); };

            var response = await aiService!.FetchResponse(model, conversation, _fileAttachmentManager.Base64Image!, _fileAttachmentManager.Base64ImageType!, _cts.Token, CurrentSettings, mustNotUseEmbedding: false, CurrentSettings.StreamResponses);

            var modelUsageManager = new ModelUsageManager(model);

            modelUsageManager.AddTokensAndSave(response.TokenUsage);






            await ProcessAiResponse(response, model, conversation);

            return response;
        }

        private void AiService_StreamingTextReceived(object? sender, string e) => chatWebView.UpdateTemp(e);

        private async Task ProcessAiResponse(AiResponse response, Model model, Conversation conversation)
        {

            var inputText = await chatWebView.GetUserPrompt();
            var systemPrompt = await chatWebView.GetSystemPrompt();
            var elapsed = stopwatch.Elapsed;

            CompletionMessage completionInput, completionResponse;
            ConversationManager.AddInputAndResponseToConversation(response, model, conversation, inputText, systemPrompt, elapsed, out completionInput, out completionResponse);

            _fileAttachmentManager.ClearBase64();


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

            var model = (Model)cbEngine.SelectedItem!;
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
            };

            var rootMessage = ConversationManager.Conversation.GetRootNode();

            var userMessage = new CompletionMessage(CompletionRole.User)
            {
                Parent = rootMessage.Guid,
                Content = lastUserMessage.Content,
                Engine = lastUserMessage.Engine,
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

            await WebNdcDrawNetworkDiagram();

        }

        public async Task PopulateUiForTemplate(ConversationTemplate template)
        {
            await chatWebView.Clear(CurrentSettings);
            await chatWebView.UpdateSendButtonColor(CurrentSettings.UseEmbeddings);

            dgvConversations.Enabled = true;
            webViewManager.Enable();

            await chatWebView.UpdateSystemPrompt(template?.SystemPrompt ?? "");
            await chatWebView.SetUserPrompt(template?.InitialPrompt ?? "");
        }

        private async void buttonStartRecording_Click(object sender, EventArgs e)
        {
            if (!audioRecorderManager.IsRecording)
            {
                await audioRecorderManager.StartRecording();
                buttonStartRecording.BackColor = Color.Red;
                buttonStartRecording.Text = "Stop\r\nRecord";
            }
            else
            {
                await audioRecorderManager.StopRecording();
                buttonStartRecording.BackColor = Color.Black;
                buttonStartRecording.Text = "Start\r\nRecord";
            }
        }



        private async void buttonAttachImage_Click(object sender, EventArgs e)
        {
            await _fileAttachmentManager.HandleAttachment(chatWebView);
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

        private async void btnGenerateEmbeddings_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable the button while processing
                btnGenerateEmbeddings.Enabled = false;

                await EmbeddingsHelper.CreateEmbeddingsAsync(CurrentSettings.EmbeddingKey);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable the button
                btnGenerateEmbeddings.Enabled = true;
            }
        }

        private void cbEngine_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentSettings.SelectedModel = cbEngine.SelectedItem!.ToString();
            SettingsSet.Save(CurrentSettings);
        }

        private void chatWebView_DragDrop(object sender, DragEventArgs e)
        {
            // get the name of the dropped file

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        }

        private void cbSummaryEngine_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentSettings.SelectedSummaryModel = cbSummaryEngine.SelectedItem!.ToString();
            SettingsSet.Save(CurrentSettings);
        }

        private async void btnProjectHelper_Click(object sender, EventArgs e)
        {
            string fileTypes = ".cs, *.html, *.css, *.js";
            var form = new FileSearchForm(CurrentSettings.DefaultPath, fileTypes);
            form.AddFilesToInput += async (s, e) =>
            {
                // attach files as txt
                await _fileAttachmentManager.AttachTextFiles(e.ToArray());

            };
            form.Show();
        }
    }
}
