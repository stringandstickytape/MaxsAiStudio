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
using AiTool3.MegaBar.Items;
using Whisper.net.Ggml;
using AiTool3.Providers;
using AiTool3.Helpers;
using System.Text;
using FontAwesome.Sharp;
using AiTool3.ExtensionMethods;

namespace AiTool3
{
    public partial class Form2 : Form
    {
        public static readonly string ThreeTicks = new string('`', 3);

        public ConversationManager ConversationManager { get; set; } = new ConversationManager();
        public Settings.Settings CurrentSettings { get; set; } = AiTool3.Settings.Settings.Load()!;

        public TopicSet TopicSet { get; set; }

        private AudioRecorderManager audioRecorderManager = new AudioRecorderManager(GgmlType.TinyEn);

        public string? Base64Image { get; set; }
        public string? Base64ImageType { get; set; }

        private CancellationTokenSource? _cts, _cts2;

        private WebViewManager? webViewManager = null;

        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();


        public Form2()
        {
            InitializeComponent();

            webViewManager = new WebViewManager(ndcWeb);
            chatWebView.ChatWebViewSendMessageEvent += ChatWebView_ChatWebViewSendMessageEvent;
            chatWebView.ChatWebViewCopyEvent += ChatWebView_ChatWebViewCopyEvent;

            rtbSystemPrompt.SetOverlayText("System Prompt");
            rtbInput.SetOverlayText("User Input");
            rtbOutput.SetOverlayText("AI Response");

            audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

            SetButtonIcon(IconChar.Paperclip, buttonAttachImage);
            SetButtonIcon(IconChar.CircleXmark, btnCancel);
            SetButtonIcon(IconChar.SquarePlus, buttonNewKeepAll);
            SetButtonIcon(IconChar.SquarePlus, btnRestart);
            SetButtonIcon(IconChar.SquarePlus, btnClear);

            // not converted
            //ndcConversation.SetContextMenuOptions(new[] { "Save this branch as TXT", "Save this branch as HTML", "Disable", "Option 3" });
            // not converted
            //ndcConversation.MenuOptionSelected += MenuOptionSelected();
            // converted
            //ndcConversation.NodeClicked += NdcConversation_NodeClicked;

            // if topics.json exists, load it
            TopicSet = TopicSet.Load();

            foreach (var topic in TopicSet.Topics)
            {
                cbCategories.Items.Add(topic.Name);
            }

            InitialiseApiList();

            SetSplitContainerEvents();

            DataGridViewHelper.InitialiseDataGridView(dgvConversations);

            InitialiseMenus();

            updateTimer.Interval = 100; // Update every 100 milliseconds
            updateTimer.Tick += UpdateTimer_Tick!;

            Load += OnHandleCreated!;

        }

        private async void OnHandleCreated(object sender, EventArgs e)
        {
            Load -= OnHandleCreated!;

            await chatWebView.EnsureCoreWebView2Async(null);

            await CreateNewWebNdc(CurrentSettings.ShowDevTools);

            await BeginNewConversation();
        }

        private void ChatWebView_ChatWebViewCopyEvent(object? sender, ChatWebViewCopyEventArgs e) =>  Clipboard.SetText(e.Content);

        private void AutoSuggestStringSelected(string selectedString)
        {
            rtbInput.InvokeIfNeeded(() =>
            {
                rtbInput.Text = selectedString;
            });
        }

        private static void SetButtonIcon(IconChar iconChar, Button button)
        {
            button.ImageAlign = ContentAlignment.TopCenter;
            button.TextImageRelation = TextImageRelation.ImageAboveText;
            button.Image = iconChar.ToBitmap(Color.White, 48);
            //button.Text = "";
        }

        private void WebViewNdc_WebNdcContextMenuOptionSelected(object? sender, WebNdcContextMenuOptionSelectedEventArgs e)
        {
            var nodes = ConversationManager.GetParentNodeList();
            var json = JsonConvert.SerializeObject(nodes);

            var option = e.MenuOption;

            WebNdcRightClickLogic.ProcessWebNdcContextMenuOption(nodes, option);
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

        private void AudioRecorderManager_AudioProcessed(object? sender, string e)
        {
            rtbInput.InvokeIfNeeded(() =>
            {
                rtbInput.Text = e;
            });
        }


        private EventHandler<MenuOptionSelectedEventArgs> MenuOptionSelected()
        {
            return (sender, e) =>
            {
                if (e.SelectedOption == "Save this branch as TXT")
                {

                }
                if (e.SelectedOption == "Save this branch as HTML")
                {

                }



                else if (e.SelectedOption == "Disable")
                {
                    var selectedGuid = e.SelectedNode.Guid;
                    var selectedMessage = ConversationManager.CurrentConversation!.FindByGuid(selectedGuid);
                    selectedMessage.Omit = !selectedMessage.Omit;
                    e.SelectedNode.IsDisabled = selectedMessage.Omit;

                    var a = WebNdcDrawNetworkDiagram().Result;
                }
                else if (e.SelectedOption == "Option 3")
                {
                    // do nothing
                }
            };
        }

        private void InitialiseMenus()
        {
            // add menu bar with file -> quit
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.BackColor = Color.Black;
            fileMenu.ForeColor = Color.White;
            var quitMenuItem = new ToolStripMenuItem("Quit");
            quitMenuItem.ForeColor = Color.White;
            quitMenuItem.BackColor = Color.Black;
            quitMenuItem.Click += (s, e) =>
            {
                Application.Exit();
            };

            // add an edit menu
            var editMenu = new ToolStripMenuItem("Edit");
            editMenu.BackColor = Color.Black;
            editMenu.ForeColor = Color.White;
            var clearMenuItem = new ToolStripMenuItem("Clear");
            clearMenuItem.ForeColor = Color.White;
            clearMenuItem.BackColor = Color.Black;
            clearMenuItem.Click += (s, e) =>
            {
                btnClear_Click(null!, null!);
            };

            // add settings option.  When chosen, invokes SettingsForm modally
            var settingsMenuItem = new ToolStripMenuItem("Settings");
            settingsMenuItem.ForeColor = Color.White;
            settingsMenuItem.BackColor = Color.Black;
            settingsMenuItem.Click += (s, e) =>
            {
                var settingsForm = new SettingsForm(CurrentSettings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    CurrentSettings = settingsForm.NewSettings;
                    AiTool3.Settings.Settings.Save(CurrentSettings);
                }
            };

            fileMenu.DropDownItems.Add(quitMenuItem);
            editMenu.DropDownItems.Add(clearMenuItem);
            editMenu.DropDownItems.Add(settingsMenuItem);
            menuBar.Items.Add(fileMenu);
            menuBar.Items.Add(editMenu);

            // add a specials menu
            CreateSpecialsMenu();
        }


        private static void AddSpecial(ToolStripMenuItem specialsMenu, string l, EventHandler q)
        {
            var reviewCodeMenuItem = new ToolStripMenuItem(l);
            reviewCodeMenuItem.ForeColor = Color.White;
            reviewCodeMenuItem.BackColor = Color.Black;
            reviewCodeMenuItem.Click += q;

            specialsMenu.DropDownItems.Add(reviewCodeMenuItem);
        }

        private void InitialiseApiList()
        {
            foreach (var model in CurrentSettings.ApiList!.SelectMany(x => x.Models))
            {
                cbEngine.Items.Add(model);
            }

            // preselect the first Local api
            cbEngine.SelectedItem = cbEngine.Items.Cast<Model>().FirstOrDefault(m => m.ServiceName.StartsWith("Local"));
        }



        private void SetSplitContainerEvents()
        {
            // for each split container incl in child items
            splitContainer1.Paint += new PaintEventHandler(SplitContainer_Paint!);
            splitContainer2.Paint += new PaintEventHandler(SplitContainer_Paint!);
            splitContainer3.Paint += new PaintEventHandler(SplitContainer_Paint!);
            splitContainer5.Paint += new PaintEventHandler(SplitContainer_Paint!);
        }

        private void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            SplitContainer sc = (sender as SplitContainer)!;

            Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

            using (SolidBrush brush = new SolidBrush(Color.Gray))
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

            rtbInput.Clear();
            if (ConversationManager.PreviousCompletion.Role == CompletionRole.User)
            {
                rtbInput.Text = ConversationManager.PreviousCompletion.Content!;
                await chatWebView.SetUserPrompt(ConversationManager.PreviousCompletion.Content!);
                ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion.Parent!);
            }
            else
            {
                await chatWebView.SetUserPrompt("");
            }
            if (ConversationManager.PreviousCompletion?.SystemPrompt != null)
            {
                rtbSystemPrompt.Text = ConversationManager.PreviousCompletion.SystemPrompt;
            }
            else rtbSystemPrompt.Text = "";
            MarkUpSnippets(rtbOutput, RtbFunctions.GetFormattedContent(ConversationManager.PreviousCompletion?.Content ?? ""), clickedCompletion.Guid!, ConversationManager.CurrentConversation.Messages);

            var parents = ConversationManager.GetParentNodeList();

            await chatWebView.AddMessages(parents);
            
        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e)
        {
            rtbInput.Text = e.Content;
            //await ConversationManager.FetchAiInputResponse(rtbInput.Text, rtbSystemPrompt.Text, (Model)cbEngine.SelectedItem!, _cts);
            await FetchAiInputResponse();
        }

        private SnippetManager snippetManager = new SnippetManager();

        public List<Snippet> MarkUpSnippets(ButtonedRichTextBox richTextBox, string text, string messageGuid, List<CompletionMessage> messages)
        {
            richTextBox.Clear();
            richTextBox.Text = text;
            var snippets = snippetManager.FindSnippets(text);

            // Apply UI formatting
            foreach (var snippet in snippets.Snippets)
            {   // snippet.Type == "html"?

                // find the end of the line
                var endOfFirstLine = text.IndexOf('\n', snippet.StartIndex);

                // find the length of the first line
                var lengthOfFirstLine = endOfFirstLine - snippet.StartIndex;

                richTextBox.Select(endOfFirstLine + 1, snippet.Code.Length - 4 - lengthOfFirstLine);
                richTextBox.SelectionColor = Color.Orange;

                if (snippet.Type == ".html" || snippet.Type == ".htm")
                {
                    HtmlHighlighter.HighlightHtml(richTextBox, endOfFirstLine + 1, snippet.Code.Length - 4 - lengthOfFirstLine);
                }
                else if (snippet.Type == ".cs")
                {
                    CSharpHighlighter.HighlightCSharp(richTextBox, endOfFirstLine + 1, snippet.Code.Length - 4 - lengthOfFirstLine);
                }



                richTextBox.SelectionFont = new Font("Courier New", richTextBox.SelectionFont?.Size ?? 10);

                var itemsForThisSnippet = MegaBarItemFactory.CreateItems(snippet.Type, snippet.Code, !string.IsNullOrEmpty(snippets.UnterminatedSnippet), messageGuid, messages);
                richTextBox.AddMegaBar(endOfFirstLine, itemsForThisSnippet.ToArray());

            }

            richTextBox.DeselectAll();

            // scroll to top
            richTextBox.SelectionStart = 0;
            return snippets.Snippets;
        }



        private async Task FetchAiInputResponse()
        {
            // Cancel any ongoing operation
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            stopwatch.Restart();
            updateTimer.Start();

            CompletionMessage? completionResponse = null;
            AiResponse? response = null;
            Model? model = null;

            try
            {
                btnCancel.Visible = true; 
                model = (Model)cbEngine.SelectedItem!;

                var aiService = AiServiceResolver.GetAiService(model.ServiceName);

                var conversation = new Conversation
                {
                    systemprompt = rtbSystemPrompt.Text,
                    messages = new List<ConversationMessage>()
                };

                List<CompletionMessage> nodes = ConversationManager.GetParentNodeList();

                Debug.WriteLine(nodes);

                foreach (var node in nodes)
                {
                    if (node.Role == CompletionRole.Root || node.Omit)
                        continue;

                    conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = node.Content! });
                }
                conversation.messages.Add(new ConversationMessage { role = "user", content = rtbInput.Text });

                var previousCompletionGuidBeforeAwait = ConversationManager.PreviousCompletion?.Guid;
                var inputText = rtbInput.Text;
                // fetch the response from the api
                response = await aiService!.FetchResponse(model, conversation, Base64Image!, Base64ImageType!, _cts.Token, rtbOutput, CurrentSettings.StreamResponses);


                if (response.SuggestedNextPrompt != null)
                {
                    rtbInput.Text = RtbFunctions.GetFormattedContent(response.SuggestedNextPrompt);
                }

                // create a completion message for the user input
                var completionInput = new CompletionMessage
                {
                    Role = CompletionRole.User,
                    Content = inputText,
                    Parent = previousCompletionGuidBeforeAwait,
                    Engine = model.ModelName,
                    Guid = System.Guid.NewGuid().ToString(),
                    Children = new List<string>(),
                    SystemPrompt = conversation.systemprompt,
                    InputTokens = response.TokenUsage.InputTokens,
                    OutputTokens = 0,
                    CreatedAt = DateTime.Now,
                };

                if (response == null)
                {
                    MessageBox.Show("Response is null");
                    //btnGo.Enabled = true;
                    return;
                }
                var pc = ConversationManager.CurrentConversation!.FindByGuid(previousCompletionGuidBeforeAwait!);

                if (pc != null)
                {
                    pc.Children!.Add(completionInput.Guid);
                }

                ConversationManager.CurrentConversation!.Messages.Add(completionInput);

                // Create a new completion object to store the response in
                completionResponse = new CompletionMessage
                {
                    Role = CompletionRole.Assistant,
                    Content = response.ResponseText,
                    Parent = completionInput.Guid,
                    Engine = model.ModelName,
                    Guid = System.Guid.NewGuid().ToString(),
                    Children = new List<string>(),
                    SystemPrompt = conversation.systemprompt,
                    InputTokens = 0,
                    OutputTokens = response.TokenUsage.OutputTokens,
                    TimeTaken = stopwatch.Elapsed,
                    CreatedAt = DateTime.Now,
                };

                // add it to the current conversation
                ConversationManager.CurrentConversation.Messages.Add(completionResponse);

                await chatWebView.AddMessage(completionInput);
                await chatWebView.AddMessage(completionResponse);
                // and display the results in the output box
                MarkUpSnippets(rtbOutput, RtbFunctions.GetFormattedContent(string.Join("\r\n", response.ResponseText)), completionResponse.Guid, ConversationManager.CurrentConversation.Messages);

                if (CurrentSettings.NarrateResponses)
                {
                    // do this but in a new thread:                 TtsHelper.ReadAloud(rtbOutput.Text);
                    var text = rtbOutput.Text;

#pragma warning disable CS4014 // We want this to go off on its own...
                    Task.Run(() => TtsHelper.ReadAloud(text));
#pragma warning restore CS4014 // 

                }

                completionInput.Children.Add(completionResponse.Guid);

                ConversationManager.PreviousCompletion = completionResponse;

                Base64Image = null;
                Base64ImageType = null;

                // draw the network diagram
                var a = await WebNdcDrawNetworkDiagram();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex is OperationCanceledException ? "Operation was cancelled." : $"An error occurred: {ex.Message}");
                return;
            }
            finally
            {
                stopwatch.Stop();
                updateTimer.Stop();
                TimeSpan ts = stopwatch.Elapsed;
                btnCancel.Visible = false; 
            }

            webViewManager!.CentreOnNode(completionResponse.Guid);

            var summaryModel = CurrentSettings.ApiList!.First(x => x.ApiName.StartsWith("Ollama")).Models.First();

            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.CurrentConversation.ConvGuid);

            ConversationManager.SaveConversation();

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.CurrentConversation.ConvGuid, ConversationManager.CurrentConversation.Messages[0].Content, ConversationManager.CurrentConversation.Messages[0].Engine, "");

                row = dgvConversations.Rows[0];
            }

            var cost = model.GetCost(response.TokenUsage);

            tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out";

            if (row != null && row.Cells[3].Value != null && string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
            {
                row.Cells[3].Value = await ConversationManager.GenerateConversationSummary(summaryModel, CurrentSettings.GenerateSummariesUsingLocalAi);
            }
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
                .Select(x => new Link { source = x.Parent!, target = x.Guid !}).ToList();


            await webViewManager.EvaluateJavascriptAsync($"addNodes({JsonConvert.SerializeObject(nodes)});");
            await webViewManager.EvaluateJavascriptAsync($"addLinks({JsonConvert.SerializeObject(links2)});");
            return true;
        }


        private async void btnClear_Click(object sender, EventArgs e)
        {
            await BeginNewConversation();
            await PopulateUiForTemplate(GetCurrentlySelectedTemplate()!);
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            BeginNewConversationPreserveInputAndSystemPrompts();
        }

        private async void BeginNewConversationPreserveInputAndSystemPrompts()
        {
            var currentPrompt = rtbInput.Text;
            var currentSystemPrompt = rtbSystemPrompt.Text;
            await BeginNewConversation();
            rtbInput.Text = currentPrompt;
            rtbSystemPrompt.Text = currentSystemPrompt;
        }

        private async void buttonNewKeepAll_Click(object sender, EventArgs e)
        {
            var lastAssistantMessage = ConversationManager.PreviousCompletion;
            var lastUserMessage = ConversationManager.CurrentConversation!.FindByGuid(lastAssistantMessage!.Parent!);
            if (lastUserMessage == null)
                return;
            if (lastAssistantMessage.Role == CompletionRole.User)
                lastAssistantMessage = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion!.Parent!);

            

            BeginNewConversationPreserveInputAndSystemPrompts();

            // create new messages out of the two

            var assistantMessage = new CompletionMessage
            {
                Parent = null,
                Role = CompletionRole.Assistant,
                Content = lastAssistantMessage.Content,
                Engine = lastAssistantMessage.Engine,
                Guid = Guid.NewGuid().ToString(),
                Children = new List<string>(),
                CreatedAt = DateTime.Now,
            };

            var rootMessage = ConversationManager.CurrentConversation.GetRootNode();

            var userMessage = new CompletionMessage
            {
                Parent = rootMessage.Guid,
                Role = CompletionRole.User,
                Content = lastUserMessage.Content,
                Engine = lastUserMessage.Engine,
                Guid = Guid.NewGuid().ToString(),
                Children = new List<string>(),
                CreatedAt = DateTime.Now,
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
            rtbInput.Clear();
            rtbSystemPrompt.Clear();
            rtbOutput.Clear();

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
            // populate the cbTemplates with the templates for the selected category
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
            await PopulateUiForTemplate(GetCurrentlySelectedTemplate()!);
        }

        private ConversationTemplate? GetCurrentlySelectedTemplate()
        {
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null)
            {
                return null;
            }
            return TopicSet.Topics.First(t => t.Name == cbCategories.SelectedItem.ToString()).Templates.First(t => t.TemplateName == cbTemplates.SelectedItem.ToString());
        }

        private async Task PopulateUiForTemplate(ConversationTemplate template)
        {
            rtbInput.Clear();
            rtbSystemPrompt.Clear();

            await chatWebView.Clear();

            if (template != null)
            {
                rtbInput.Text = template.InitialPrompt;
                rtbSystemPrompt.Text = template.SystemPrompt;
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



        private void buttonAttachImage_Click(object sender, EventArgs e)
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
                        rtbInput.Text = $"{sb.ToString()}{rtbInput.Text}";

                        CurrentSettings.SetDefaultPath(Path.GetDirectoryName(attachTextFilesDialog.FileName)!);
                    }
                    break;
                case DialogResult.Cancel:
                    break;
            }
        }

        private async void tbSearch_TextChanged(object sender, EventArgs e)
        {
            // Cancel the previous operation if it's still running
            _cts2?.Cancel();

            _cts2 = new CancellationTokenSource();

            try
            {
                await Task.Run(() =>
                {
                    foreach (DataGridViewRow row in dgvConversations.Rows)
                    {
                        _cts2.Token.ThrowIfCancellationRequested();

                        if (row.Cells[0].Value == null) continue;

                        var guid = row.Cells[0].Value.ToString();

                        var conv = BranchedConversation.LoadConversation(guid!);

                        var allMessages = conv.Messages.Select(m => m.Content).ToList();

                        bool isVisible = false;
                        foreach (string? message in allMessages)
                        {
                            if (message!.IndexOf(tbSearch.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
                            {
                                isVisible = true;
                                break;
                            }
                            _cts2.Token.ThrowIfCancellationRequested();
                        }

                        this.Invoke((System.Windows.Forms.MethodInvoker)delegate {
                            row.Visible = isVisible;
                        });
                        // sleep 1000
                        _cts2.Token.ThrowIfCancellationRequested();
                    }
                }, _cts2.Token);

            }
            catch (Exception ex)
            {
                if(!(ex is OperationCanceledException)) MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void btnClearSearch_Click(object sender, EventArgs e) => tbSearch.Clear();

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            btnCancel.Visible = false;
        }
        private async Task CreateNewWebNdc(bool showDevTools)
        {
            await webViewManager.CreateNewWebNdc(showDevTools);

            webViewManager.WebNdcContextMenuOptionSelected += WebViewNdc_WebNdcContextMenuOptionSelected;
            webViewManager.WebNdcNodeClicked += WebViewNdc_WebNdcNodeClicked;
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            webViewManager!.webView.Dispose();
        }
    }

}
