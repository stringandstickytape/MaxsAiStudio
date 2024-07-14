using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Settings;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using AiTool3.Audio;
using AiTool3.Snippets;
using Whisper.net.Ggml;
using AiTool3.Providers;
using AiTool3.Helpers;
using System.Text;
using FontAwesome.Sharp;
using AiTool3.ExtensionMethods;
using System.Windows.Forms;
using System.Net;
using System.Reflection.Metadata.Ecma335;

namespace AiTool3
{
    public partial class Form2 : Form
    {
        private SnippetManager snippetManager = new SnippetManager();

        public static readonly string ThreeTicks = new string('`', 3);

        public ConversationManager ConversationManager { get; set; } = new ConversationManager();
        public Settings.Settings CurrentSettings { get; set; } = AiTool3.Settings.Settings.Load()!;

        public TopicSet TopicSet { get; set; }

        public string? Base64Image { get; set; }
        public string? Base64ImageType { get; set; }

        private CancellationTokenSource? _cts, _cts2;
        private WebViewManager? webViewManager = null;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private AudioRecorderManager audioRecorderManager = new AudioRecorderManager(GgmlType.TinyEn);

        public string selectedConversationGuid = "";
        public Form2()
        {
            InitializeComponent();

            webViewManager = new WebViewManager(ndcWeb);
            chatWebView.ChatWebViewSendMessageEvent += ChatWebView_ChatWebViewSendMessageEvent;
            chatWebView.ChatWebViewCancelEvent += ChatWebView_ChatWebViewCancelEvent;
            chatWebView.ChatWebViewCopyEvent += ChatWebView_ChatWebViewCopyEvent;

            splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

            audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

            ButtonIconHelper.SetButtonIcon(IconChar.Paperclip, buttonAttachImage);
            ButtonIconHelper.SetButtonIcon(IconChar.SquarePlus, buttonNewKeepAll);
            ButtonIconHelper.SetButtonIcon(IconChar.SquarePlus, btnRestart);
            ButtonIconHelper.SetButtonIcon(IconChar.SquarePlus, btnClear);

            // if topics.json exists, load it
            TopicSet = TopicSet.Load();

            foreach (var topic in TopicSet.Topics)
            {
                cbCategories.Items.Add(topic.Name);
            }

            InitialiseApiList();

            splitContainer1.Paint += new PaintEventHandler(SplitContainer_Paint!);
            splitContainer5.Paint += new PaintEventHandler(SplitContainer_Paint!);

            DataGridViewHelper.InitialiseDataGridView(dgvConversations);
            
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Regenerate Summary", null, Option1_Click);

            dgvConversations.ContextMenuStrip = contextMenu;

            InitialiseMenus();

            updateTimer.Interval = 100;
            updateTimer.Tick += UpdateTimer_Tick!;

            Load += OnHandleCreated!;
            
            dgvConversations.MouseDown  += DgvConversations_MouseDown;
        }

        private async void OnHandleCreated(object sender, EventArgs e)
        {
            Load -= OnHandleCreated!;

            await chatWebView.EnsureCoreWebView2Async(null);

            await CreateNewWebNdc(CurrentSettings.ShowDevTools);

            await BeginNewConversation();

            if (CurrentSettings.RunWebServer)
            {
                await WebServerHelper.CreateWebServerAsync(chatWebView, FetchAiInputResponse);
            }
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

        private async void Option1_Click(object sender, EventArgs e)
        {
            await ConversationManager.RegenerateSummary((Model)cbEngine.SelectedItem!, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, selectedConversationGuid, CurrentSettings);
        }

        private async void ChatWebView_ChatWebViewCancelEvent(object? sender, ChatWebViewCancelEventArgs e)
        {
            _cts = Form2.ResetCancellationtoken(_cts);
            await chatWebView.DisableSendButton();
            await chatWebView.EnableCancelButton();
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
            }

            cbEngine.SelectedItem = cbEngine.Items.Cast<Model>().FirstOrDefault(m => m.ServiceName.StartsWith("Local"));
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
            var clickedCompletion = ConversationManager.CurrentConversation!.Messages.FirstOrDefault(c => c.Guid == e.NodeId);
            if (clickedCompletion == null)
                return;
            ConversationManager.PreviousCompletion = clickedCompletion;

            string systemPrompt = "";
            systemPrompt = ConversationManager.PreviousCompletion.SystemPrompt!;
            if (ConversationManager.PreviousCompletion.Role == CompletionRole.User)
            {
                await chatWebView.SetUserPrompt(ConversationManager.PreviousCompletion.Content!);
                ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion.Parent!);
            }
            else
            {
                await chatWebView.SetUserPrompt("");
            }
            //await chatWebView.ChangeChatHeaderLabel(ConversationManager.PreviousCompletion.Engine);
            await chatWebView.UpdateSystemPrompt(systemPrompt);

            var parents = ConversationManager.GetParentNodeList();

            await chatWebView.AddMessages(parents);

        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e) => await FetchAiInputResponse();



        private async Task<string> FetchAiInputResponse()
        {
            string retVal = "";
            try
            {
                PrepareForNewResponse();
                var (conversation, model) = await PrepareConversationData();
                var response = await FetchResponseFromAi(conversation, model);
                await ProcessAiResponse(response, model);
                retVal = response.ResponseText;
                await chatWebView.SetUserPrompt("");
                await chatWebView.DisableCancelButton();
                await chatWebView.EnableSendButton();
                await UpdateUi(response);
                await UpdateConversationSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex is OperationCanceledException ? "Operation was cancelled." : $"An error occurred: {ex.Message}");

                chatWebView.ClearTemp();
                _cts = Form2.ResetCancellationtoken(_cts);
            }
            finally
            {
                await chatWebView.DisableCancelButton();
                await chatWebView.EnableSendButton();
                stopwatch.Stop();
                updateTimer.Stop();
                
            }
            return retVal;
        }

        private async void PrepareForNewResponse()
        {
            _cts = Form2.ResetCancellationtoken(_cts);
            stopwatch.Restart();
            updateTimer.Start();

            await chatWebView.DisableSendButton();
            await chatWebView.EnableCancelButton();
        }

        private async Task<ConversationModelPair> PrepareConversationData()
        {
            var model = (Model)cbEngine.SelectedItem!;
            var conversation = new Conversation
            {
                systemprompt = await chatWebView.GetSystemPrompt(),
                messages = new List<ConversationMessage>()
            };

            List<CompletionMessage> nodes = ConversationManager.GetParentNodeList();

            foreach (var node in nodes)
            {
                if (node.Role == CompletionRole.Root || node.Omit)
                    continue;

                conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = node.Content! });
            }
            conversation.messages.Add(new ConversationMessage { role = "user", content = await chatWebView.GetUserPrompt() });

            return new ConversationModelPair(conversation, model);
        }

        private async Task<AiResponse> FetchResponseFromAi(Conversation conversation, Model model)
        {
            var aiService = AiServiceResolver.GetAiService(model.ServiceName);
            aiService.StreamingTextReceived += AiService_StreamingTextReceived;
            aiService.StreamingComplete += (s, e) => { chatWebView.InvokeIfNeeded(() => chatWebView.ClearTemp()); };

            return await aiService!.FetchResponse(model, conversation, Base64Image!, Base64ImageType!, _cts.Token, CurrentSettings, CurrentSettings.StreamResponses);
        }

        private void AiService_StreamingTextReceived(object? sender, string e)
        {
            chatWebView.UpdateTemp(e);
        }

        private async Task ProcessAiResponse(AiResponse response, Model model)
        {
            var previousCompletionGuidBeforeAwait = ConversationManager.PreviousCompletion?.Guid;
            var inputText = await chatWebView.GetUserPrompt();
            var systemPrompt = await chatWebView.GetSystemPrompt();


            var completionInput = new CompletionMessage(CompletionRole.User)
            {
                Content = inputText,
                Parent = previousCompletionGuidBeforeAwait,
                Engine = model.ModelName,
                SystemPrompt = systemPrompt,
                InputTokens = response.TokenUsage.InputTokens,
                OutputTokens = 0,
            };

            var pc = ConversationManager.CurrentConversation!.FindByGuid(previousCompletionGuidBeforeAwait!);
            if (pc != null)
            {
                pc.Children!.Add(completionInput.Guid);
            }

            ConversationManager.CurrentConversation!.Messages.Add(completionInput);

            var completionResponse = new CompletionMessage(CompletionRole.Assistant)
            {
                Content = response.ResponseText,
                Parent = completionInput.Guid,
                Engine = model.ModelName,
                SystemPrompt = systemPrompt,
                InputTokens = 0,
                OutputTokens = response.TokenUsage.OutputTokens,
                TimeTaken = stopwatch.Elapsed,
            };

            ConversationManager.CurrentConversation.Messages.Add(completionResponse);

            await chatWebView.AddMessage(completionInput);
            await chatWebView.AddMessage(completionResponse);

            if (CurrentSettings.NarrateResponses)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => TtsHelper.ReadAloud(response.ResponseText));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            completionInput.Children.Add(completionResponse.Guid);
            ConversationManager.PreviousCompletion = completionResponse;

            ConversationManager.SaveConversation();

            Base64Image = null;
            Base64ImageType = null;

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

            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.CurrentConversation.ConvGuid);

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.CurrentConversation.ConvGuid, ConversationManager.CurrentConversation.Messages[0].Content, ConversationManager.CurrentConversation.Messages[0].Engine, "");
            }
        }

        private async Task UpdateConversationSummary()
        {
            var summaryModel = CurrentSettings.ApiList!.First(x => x.ApiName.StartsWith("Ollama")).Models.First();
            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.CurrentConversation.ConvGuid);

            if (row != null && row.Cells[3].Value != null && string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
            {
                row.Cells[3].Value = await ConversationManager.GenerateConversationSummary(summaryModel, CurrentSettings.GenerateSummariesUsingLocalAi, CurrentSettings);
            }

            ConversationManager.SaveConversation();
        }

        private async Task<bool> WebNdcDrawNetworkDiagram()
        {
            if (webViewManager == null || webViewManager.webView.CoreWebView2 == null) return false;

            var a = await webViewManager.Clear();

            var nodes = ConversationManager.CurrentConversation!.Messages
                .Where(x => x.Role != CompletionRole.Root)
                .Select(m => new IdNodeRole { id = m.Guid!, label = m.Content!, role = m.Role.ToString(), colour = m.GetColorHexForEngine() }).ToList();

            var links2 = ConversationManager.CurrentConversation.Messages
                .Where(x => x.Parent != null)
                .Select(x => new Link { source = x.Parent!, target = x.Guid! }).ToList();


            await webViewManager.EvaluateJavascriptAsync($"addNodes({JsonConvert.SerializeObject(nodes)});");
            await webViewManager.EvaluateJavascriptAsync($"addLinks({JsonConvert.SerializeObject(links2)});");
            return true;
        }


        private async void btnClear_Click(object sender, EventArgs e)
        {
            await BeginNewConversation();
            await chatWebView.SetUserPrompt("");
            await PopulateUiForTemplate(selectedTemplate!);
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            BeginNewConversationPreserveInputAndSystemPrompts();
        }

        private async void BeginNewConversationPreserveInputAndSystemPrompts()
        {
            var currentPrompt = await chatWebView.GetUserPrompt();
            var currentSystemPrompt = await chatWebView.GetSystemPrompt();
            await BeginNewConversation();
            await chatWebView.UpdateSystemPrompt(currentSystemPrompt);
        }

        private async void buttonNewKeepContext_Click(object sender, EventArgs e)
        {
            var lastAssistantMessage = ConversationManager.PreviousCompletion;
            var lastUserMessage = ConversationManager.CurrentConversation!.FindByGuid(lastAssistantMessage!.Parent!);
            if (lastUserMessage == null)
                return;
            if (lastAssistantMessage.Role == CompletionRole.User)
                lastAssistantMessage = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion!.Parent!);

            BeginNewConversationPreserveInputAndSystemPrompts();

            var assistantMessage = new CompletionMessage(CompletionRole.Assistant)
            {
                Parent = null,
                Content = lastAssistantMessage.Content,
                Engine = lastAssistantMessage.Engine,
            };

            var rootMessage = ConversationManager.CurrentConversation.GetRootNode();

            var userMessage = new CompletionMessage(CompletionRole.User)
            {
                Parent = rootMessage.Guid,
                Content = lastUserMessage.Content,
                Engine = lastUserMessage.Engine,
            };
            rootMessage.Children!.Add(userMessage.Guid);
            assistantMessage.Parent = userMessage.Guid;
            userMessage.Children.Add(assistantMessage.Guid);

            ConversationManager.CurrentConversation.Messages.AddRange(new[] { assistantMessage, userMessage });
            ConversationManager.PreviousCompletion = assistantMessage;

            await WebNdcDrawNetworkDiagram();
        }

        private async Task BeginNewConversation()
        {
            await chatWebView.Clear();

            ConversationManager.CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            ConversationManager.CurrentConversation.AddNewRoot();
            ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.Messages.First();

            await WebNdcDrawNetworkDiagram();
        }

        private async void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var clickedGuid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();

            ConversationManager.LoadConversation(clickedGuid!);

            await WebNdcDrawNetworkDiagram();

        }

        private void cbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = cbCategories.SelectedItem!.ToString();

            var topics = TopicSet.Topics.Where(t => t.Name == selected).ToList();

            var templates = topics.SelectMany(t => t.Templates).Where(x => x.SystemPrompt != null).ToList();

            cbTemplates.Items.Clear();
            cbTemplates.Items.AddRange(templates.Select(t => t.TemplateName).ToArray());
            cbTemplates.DroppedDown = true;
        }

        private async void cbTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnClear_Click(null!, null!);
            await PopulateUiForTemplate(selectedTemplate!);
        }

        ConversationTemplate? selectedTemplate = null;

        private async Task SelectTemplate(string categoryName, string templateName)
        {
            selectedTemplate = TopicSet.Topics.First(t => t.Name == categoryName).Templates.First(t => t.TemplateName == templateName);
            await PopulateUiForTemplate(selectedTemplate!);
        }

        private async Task PopulateUiForTemplate(ConversationTemplate template)
        {
            await chatWebView.Clear();

            if (template != null)
            {
                await chatWebView.UpdateSystemPrompt(template.SystemPrompt);
                await chatWebView.SetUserPrompt(template.InitialPrompt);
            }
        }

        private void buttonEditTemplate_Click(object sender, EventArgs e)
        {
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null) return;

            EditAndSaveTemplate(GetCurrentTemplate()!);
        }

        private ConversationTemplate? GetCurrentTemplate()
        {
            ConversationTemplate template;
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null)
            {
                return null;
            }
            var category = cbCategories.SelectedItem.ToString();
            var templateName = cbTemplates.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(templateName))
                return null;

            template = TopicSet.Topics.First(t => t.Name == category).Templates.First(t => t.TemplateName == templateName);
            return template;
        }

        private void EditAndSaveTemplate(ConversationTemplate template, bool add = false, string? category = null)
        {
            TemplatesHelper.UpdateTemplates(template, add, category, new Form(), TopicSet, cbCategories, cbTemplates);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbCategories.Text)) return;

            var template = new ConversationTemplate("System Prompt", "Initial Prompt");

            EditAndSaveTemplate(template, true, cbCategories.Text);
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
            // pop a mb asking attach image or text with image and text buttons
            var r = SimpleDialogsHelper.ShowAttachmentDialog();

            switch (r)
            {
                case DialogResult.Yes:
                    OpenFileDialog openFileDialog = ImageHelpers.ShowAttachImageFileDialog(CurrentSettings.DefaultPath);

                    Base64Image = openFileDialog.FileName != "" ? ImageHelpers.ImageToBase64(openFileDialog.FileName) : "";
                    Base64ImageType = openFileDialog.FileName != "" ? ImageHelpers.GetImageType(openFileDialog.FileName) : "";
                    break;
                case DialogResult.No:
                    OpenFileDialog attachTextFilesDialog = ImageHelpers.ShowAttachTextFilesDialog(CurrentSettings.DefaultPath);

                    if (attachTextFilesDialog.FileNames.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var file in attachTextFilesDialog.FileNames)
                        {
                            sb.AppendMany(ThreeTicks,
                                Path.GetFileName(file),
                                Environment.NewLine,
                                File.ReadAllText(file),
                                Environment.NewLine,
                                ThreeTicks,
                                Environment.NewLine,
                                Environment.NewLine);
                        }

                        var existingPrompt = await chatWebView.GetUserPrompt();

                        await chatWebView.SetUserPrompt($"{sb.ToString()}{existingPrompt}");

                        CurrentSettings.SetDefaultPath(Path.GetDirectoryName(attachTextFilesDialog.FileName)!);
                    }
                    break;
                case DialogResult.Cancel:
                    break;
            }
        }

        private async void tbSearch_TextChanged(object sender, EventArgs e)
        {
            _cts2 = ResetCancellationtoken(_cts2);

            try
            {
                foreach (DataGridViewRow row in dgvConversations.Rows)
                {
                    _cts2.Token.ThrowIfCancellationRequested();

                    var guid = row.Cells[0].Value?.ToString();

                    if (guid != null)
                    {
                        bool isVisible = await IsConversationVisible(guid, tbSearch.Text, _cts2.Token);

                        this.InvokeIfNeeded(() =>
                        {
                            row.Visible = isVisible;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException)) MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        public static CancellationTokenSource ResetCancellationtoken(CancellationTokenSource? cts)
        {
            cts?.Cancel();
            return new CancellationTokenSource();
        }

        private static async Task<bool> IsConversationVisible(string guid, string searchText, CancellationToken cancellationToken)
        {
            var conv = BranchedConversation.LoadConversation(guid);
            var allMessages = conv.Messages.Select(m => m.Content).ToList();

            foreach (string? message in allMessages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (message!.IndexOf(searchText, StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void btnClearSearch_Click(object sender, EventArgs e) => tbSearch.Clear();

        private async Task CreateNewWebNdc(bool showDevTools)
        {
            await webViewManager.CreateNewWebNdc(showDevTools);

            webViewManager.WebNdcContextMenuOptionSelected += WebViewNdc_WebNdcContextMenuOptionSelected;
            webViewManager.WebNdcNodeClicked += WebViewNdc_WebNdcNodeClicked;
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e) => webViewManager!.webView.Dispose();

        private void button1_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !splitContainer1.Panel1Collapsed;

            button1.Text = splitContainer1.Panel1Collapsed ? @">
>
>" : @"<
<
<";

        }
    }
}
