using AiTool3.Conversations;
using AiTool3.Settings;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using static AiTool3.UI.NetworkDiagramControl;
using Microsoft.CodeAnalysis;
using AiTool3.Audio;
using AiTool3.Snippets;
using AiTool3.MegaBar.Items;
using Whisper.net.Ggml;
using AiTool3.Providers;
using AiTool3.Helpers;
using AiTool3.ApiManagement;

namespace AiTool3
{
    public partial class Form2 : Form
    {
        public ConversationManager ConversationManager { get; set; } = new ConversationManager();
        public Settings.Settings Settings { get; set; } = AiTool3.Settings.Settings.ReadFromJson();
        public TopicSet TopicSet { get; set; }
        public string Base64Image { get; set; }
        public string Base64ImageType { get; set; }

        private readonly AudioRecorderManager audioRecorderManager;
        private readonly SnippetManager snippetManager = new SnippetManager();

        public Form2()
        {
            InitializeComponent();
            audioRecorderManager = new AudioRecorderManager(GgmlType.TinyEn);
            audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;
            InitializeComponents();
            LoadTopics();
            InitialiseApiList();
            SetupEventHandlers();
            DataGridViewHelper.InitialiseDataGridView(dgvConversations);
            InitialiseMenus();
        }

        private void InitializeComponents()
        {
            ndcConversation.SetContextMenuOptions(new[] { "Save conversation to here as TXT", "Option 2", "Option 3" });
            ndcConversation.MenuOptionSelected += MenuOptionSelected();
        }

        private void LoadTopics()
        {
            TopicSet = TopicSet.Load();
            foreach (var topic in TopicSet.Topics)
            {
                cbCategories.Items.Add(topic.Name);
            }
        }

        private void SetupEventHandlers()
        {
            ndcConversation.NodeClicked += NdcConversation_NodeClicked;
            SetSplitContainerEvents();
            rtbInput.KeyDown += (s, e) => CheckForCtrlReturn(e);
            this.KeyDown += (s, e) => CheckForCtrlReturn(e);
        }

        private void AudioRecorderManager_AudioProcessed(object sender, string e)
        {
            if (rtbInput.InvokeRequired)
            {
                rtbInput.Invoke(new Action(() => rtbInput.Text += e));
            }
            else
            {
                rtbInput.Text += e;
            }
        }

        private EventHandler<MenuOptionSelectedEventArgs> MenuOptionSelected()
        {
            return (sender, e) =>
            {
                if (e.SelectedOption == "Save conversation to here as TXT")
                {
                    SaveConversationToFile();
                }
            };
        }

        private void SaveConversationToFile()
        {
            var nodes = ConversationManager.GetParentNodeList();
            string conversation = string.Join("\n\n", nodes.Select(node => $"{node.Role}: {node.Content}"));

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, conversation);
                Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
            }
        }

        private void InitialiseMenus()
        {
            var fileMenu = CreateMenu("File", new (string, Action<object, EventArgs>)[] { ("Quit", (s, e) => Application.Exit()) });
            var editMenu = CreateMenu("Edit", new (string, Action<object, EventArgs>)[] { ("Clear", (s, e) => btnClear_Click(null, null)), ("Settings", OpenSettingsForm) });
            var specialsMenu = CreateMenu("Specials", new (string, Action<object, EventArgs>)[]
            {
                ("Pull Readme and update from latest diff", PullReadmeAndUpdate),
                ("Review Code", ReviewCode)
            });

            menuBar.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, specialsMenu });
        }

        private ToolStripMenuItem CreateMenu(string name, (string, Action<object, EventArgs>)[] items)
        {
            var menu = new ToolStripMenuItem(name) { BackColor = Color.Black, ForeColor = Color.White };
            foreach (var (itemName, action) in items)
            {
                var item = new ToolStripMenuItem(itemName) { ForeColor = Color.White, BackColor = Color.Black };
                item.Click += new EventHandler(action);
                menu.DropDownItems.Add(item);
            }
            return menu;
        }

        private void OpenSettingsForm(object sender, EventArgs e)
        {
            var settingsForm = new SettingsForm(Settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                Settings = settingsForm.NewSettings;
                AiTool3.Settings.Settings.WriteToJson(Settings);
            }
        }

        private void PullReadmeAndUpdate(object sender, EventArgs e)
        {
            AiResponse response, response2;
            SpecialsHelper.GetReadmeResponses((Model)cbEngine.SelectedItem, out response, out response2);
            var snippets = FindSnippets(rtbOutput, $"{response.ResponseText}{Environment.NewLine}{response2.ResponseText}", null, null);

            try
            {
                var code = SnipperHelper.StripFirstAndLastLine(snippets.First().Code);
                File.WriteAllText(@"C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\README.md", code);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing to file: {ex.Message}");
            }
        }

        private void ReviewCode(object sender, EventArgs e)
        {
            SpecialsHelper.ReviewCode((Model)cbEngine.SelectedItem, out string userMessage);
            rtbInput.Text = userMessage;
        }

        private void InitialiseApiList()
        {
            cbEngine.Items.AddRange(Settings.ApiList.SelectMany(x => x.Models).ToArray());
            cbEngine.SelectedItem = cbEngine.Items.Cast<Model>().FirstOrDefault(m => m.ServiceName.StartsWith("Local"));
        }

        private void CheckForCtrlReturn(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Return)
            {
                btnGo_Click(null, null);
                e.SuppressKeyPress = true;
            }
        }

        private void SetSplitContainerEvents()
        {
            Action<SplitContainer> setSplitterPaint = sc => sc.Paint += SplitContainer_Paint;
            setSplitterPaint(splitContainer1);
            setSplitterPaint(splitContainer2);
            setSplitterPaint(splitContainer3);
            setSplitterPaint(splitContainer5);
        }

        private void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            if (sender is SplitContainer sc)
            {
                Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                    ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                    : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

                using (SolidBrush brush = new SolidBrush(Color.Gray))
                {
                    e.Graphics.FillRectangle(brush, splitterRect);
                }
            }
        }

        private void NdcConversation_NodeClicked(object sender, NodeClickEventArgs e)
        {
            var clickedCompletion = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Guid == e.ClickedNode.Guid);
            ConversationManager.PreviousCompletion = clickedCompletion;

            UpdateInputAndSystemPrompt(clickedCompletion);
            FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(ConversationManager.PreviousCompletion?.Content ?? ""), clickedCompletion.Guid, ConversationManager.CurrentConversation.Messages);
        }

        private void UpdateInputAndSystemPrompt(CompletionMessage clickedCompletion)
        {
            rtbInput.Clear();
            if (clickedCompletion.Role == CompletionRole.User)
            {
                rtbInput.Text = clickedCompletion.Content;
                ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.FindByGuid(clickedCompletion.Parent);
            }
            rtbSystemPrompt.Text = ConversationManager.PreviousCompletion?.SystemPrompt ?? "";
        }

        public List<Snippet> FindSnippets(ButtonedRichTextBox richTextBox, string text, string messageGuid, List<CompletionMessage> messages)
        {
            richTextBox.Clear();
            richTextBox.Text = text;
            var snippets = snippetManager.FindSnippets(text);

            foreach (var snippet in snippets.Snippets)
            {
                ApplySnippetFormatting(richTextBox, text, snippet, messageGuid, messages);
            }

            richTextBox.DeselectAll();
            richTextBox.SelectionStart = 0;
            return snippets.Snippets;
        }

        private void ApplySnippetFormatting(ButtonedRichTextBox richTextBox, string text, Snippet snippet, string messageGuid, List<CompletionMessage> messages)
        {
            int endOfFirstLine = text.IndexOf('\n', snippet.StartIndex);
            int lengthOfFirstLine = endOfFirstLine - snippet.StartIndex;

            richTextBox.Select(endOfFirstLine + 1, snippet.Code.Length - 4 - lengthOfFirstLine);
            richTextBox.SelectionColor = Color.Yellow;
            richTextBox.SelectionFont = new Font("Courier New", richTextBox.SelectionFont?.Size ?? 10);

            var itemsForThisSnippet = MegaBarItemFactory.CreateItems(snippet.Type, snippet.Code, false, messageGuid, messages);
            richTextBox.AddMegaBar(endOfFirstLine, itemsForThisSnippet.ToArray());
        }

        private async void btnGo_Click(object sender, EventArgs e)
        {
            btnGo.Enabled = false;
            var model = (Model)cbEngine.SelectedItem;
            var aiService = AiServiceResolver.GetAiService(model.ServiceName);

            var conversation = CreateConversation();
            var response = await aiService.FetchResponse(model, conversation, Base64Image, Base64ImageType);

            if (response == null)
            {
                MessageBox.Show("Response is null");
                btnGo.Enabled = true;
                return;
            }

            ProcessResponse(model, response);

            DrawNetworkDiagram();
            UpdateUI(response);

            btnGo.Enabled = true;
        }

        private Conversation CreateConversation()
        {
            var conversation = new Conversation
            {
                systemprompt = rtbSystemPrompt.Text,
                messages = ConversationManager.GetParentNodeList()
                    .Where(node => node.Role != CompletionRole.Root)
                    .Select(node => new ConversationMessage
                    {
                        role = node.Role == CompletionRole.User ? "user" : "assistant",
                        content = node.Content
                    })
                    .ToList()
            };
            conversation.messages.Add(new ConversationMessage { role = "user", content = rtbInput.Text });
            return conversation;
        }

        private void ProcessResponse(Model model, AiResponse response)
        {
            var completionInput = CreateCompletionMessage(CompletionRole.User, rtbInput.Text, model, response.TokenUsage.InputTokens);
            var completionResponse = CreateCompletionMessage(CompletionRole.Assistant, response.ResponseText, model, response.TokenUsage.OutputTokens);

            UpdateConversation(completionInput, completionResponse);

            FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(response.ResponseText), completionResponse.Guid, ConversationManager.CurrentConversation.Messages);

            if (Settings.NarrateResponses)
            {
                Task.Run(() => TtsHelper.ReadAloud(rtbOutput.Text));
            }

            ConversationManager.PreviousCompletion = completionResponse;
            Base64Image = null;
            Base64ImageType = null;
        }

        private CompletionMessage CreateCompletionMessage(CompletionRole role, string content, Model model, int tokens)
        {
            return new CompletionMessage
            {
                Role = role,
                Content = content,
                Parent = role == CompletionRole.User ? ConversationManager.PreviousCompletion?.Guid : null,
                Engine = model.ModelName,
                Guid = System.Guid.NewGuid().ToString(),
                Children = new List<string>(),
                SystemPrompt = rtbSystemPrompt.Text,
                InputTokens = role == CompletionRole.User ? tokens : 0,
                OutputTokens = role == CompletionRole.Assistant ? tokens : 0
            };
        }

        private void UpdateConversation(CompletionMessage input, CompletionMessage response)
        {
            if (ConversationManager.PreviousCompletion != null)
            {
                ConversationManager.PreviousCompletion.Children.Add(input.Guid);
            }

            ConversationManager.CurrentConversation.Messages.Add(input);
            ConversationManager.CurrentConversation.Messages.Add(response);

            input.Children.Add(response.Guid);
        }

        private void UpdateUI(AiResponse response)
        {
            var cost = ((Model)cbEngine.SelectedItem).GetCost(response.TokenUsage);
            UpdateConversationDataGridView();
            tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out";
        }

        private async void UpdateConversationDataGridView()
        {
            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.CurrentConversation.ConvGuid);

            ConversationManager.SaveConversation();

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.CurrentConversation.ConvGuid, ConversationManager.CurrentConversation.Messages[0].Content, ConversationManager.CurrentConversation.Messages[0].Engine, "");
                row = dgvConversations.Rows[0];
            }

            if (row != null && row.Cells[3].Value != null && string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
            {
                var summaryModel = Settings.ApiList.First(x => x.ApiName.StartsWith("Ollama")).Models.First();
                row.Cells[3].Value = await ConversationManager.GenerateConversationSummary(summaryModel, Settings.GenerateSummariesUsingLocalAi);
            }
        }


        private void DrawNetworkDiagram()
        {
            ndcConversation.Clear();

            var root = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Parent == null);
            if (root == null) return;

            var y = 100;
            var rootNode = CreateNode(root, new Point(300, y));
            ndcConversation.AddNode(rootNode);

            DrawChildren(root, rootNode, 400, ref y);
        }

        private Node CreateNode(CompletionMessage message, Point location)
        {
            var node = new Node(message.Content, location, message.Guid, message.InfoLabel)
            {
                BackColor = message.GetColorForEngine()
            };
            return node;
        }

        private void DrawChildren(CompletionMessage parent, Node parentNode, int x, ref int y)
        {
            foreach (var childGuid in parent.Children)
            {
                y += 100;
                var childMsg = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Guid == childGuid);
                var childNode = CreateNode(childMsg, new Point(x, y));
                ndcConversation.AddNode(childNode);
                ndcConversation.AddConnection(parentNode, childNode);
                DrawChildren(childMsg, childNode, x + 100, ref y);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearAllFields();
            ResetConversation();
            DrawNetworkDiagram();
        }

        private void ClearAllFields()
        {
            rtbInput.Clear();
            rtbSystemPrompt.Clear();
            rtbOutput.Clear();
        }

        private void ResetConversation()
        {
            ConversationManager.CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            ConversationManager.CurrentConversation.AddNewRoot();
            ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.Messages.First();
        }

        private void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var clickedGuid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();
            ConversationManager.LoadConversation(clickedGuid);
            DrawNetworkDiagram();
            ndcConversation.FitAll();
        }

        private void cbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTemplatesComboBox();
        }

        private void UpdateTemplatesComboBox()
        {
            var selected = cbCategories.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            var templates = TopicSet.Topics
                .Where(t => t.Name == selected)
                .SelectMany(t => t.Templates)
                .Where(x => x.SystemPrompt != null)
                .Select(t => t.TemplateName)
                .ToArray();

            cbTemplates.Items.Clear();
            cbTemplates.Items.AddRange(templates);
            cbTemplates.DroppedDown = true;
        }

        private void cbTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySelectedTemplate();
        }

        private void ApplySelectedTemplate()
        {
            var template = GetCurrentTemplate();
            if (template == null) return;

            btnClear_Click(null, null);
            rtbInput.Text = template.InitialPrompt;
            rtbSystemPrompt.Text = template.SystemPrompt;
        }

        private void buttonEditTemplate_Click(object sender, EventArgs e)
        {
            var template = GetCurrentTemplate();
            if (template != null)
            {
                EditAndSaveTemplate(template);
            }
        }

        private ConversationTemplate GetCurrentTemplate()
        {
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null) return null;

            var category = cbCategories.SelectedItem.ToString();
            var templateName = cbTemplates.SelectedItem.ToString();
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(templateName)) return null;

            return TopicSet.Topics
                .First(t => t.Name == category)
                .Templates
                .First(t => t.TemplateName == templateName);
        }

        private void EditAndSaveTemplate(ConversationTemplate template, bool add = false, string category = null)
        {
            TemplatesHelper.UpdateTemplates(template, add, category, new Form(), TopicSet, cbCategories, cbTemplates);
        }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cbCategories.Text)) return;

            var template = new ConversationTemplate("System Prompt", "Initial Prompt");
            EditAndSaveTemplate(template, true, cbCategories.Text);
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            rtbOutput.Clear();
            ConversationManager.CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            ConversationManager.PreviousCompletion = null;
            DrawNetworkDiagram();
        }

        private async void buttonStartRecording_Click(object sender, EventArgs e)
        {
            if (!audioRecorderManager.IsRecording)
            {
                await StartRecording();
            }
            else
            {
                await StopRecording();
            }
        }

        private async Task StartRecording()
        {
            await audioRecorderManager.StartRecording();
            buttonStartRecording.BackColor = Color.Red;
            buttonStartRecording.Text = "Stop\r\nRecord";
        }

        private async Task StopRecording()
        {
            await audioRecorderManager.StopRecording();
            buttonStartRecording.BackColor = Color.Black;
            buttonStartRecording.Text = "Start\r\nRecord";
        }

        private void buttonNewKeepAll_Click(object sender, EventArgs e)
        {
            var lastAssistantMessage = GetLastAssistantMessage();
            var lastUserMessage = ConversationManager.CurrentConversation.FindByGuid(lastAssistantMessage.Parent);

            CreateNewConversationWithLastMessages(lastAssistantMessage, lastUserMessage);
            DrawNetworkDiagram();
        }

        private CompletionMessage GetLastAssistantMessage()
        {
            var lastMessage = ConversationManager.PreviousCompletion;
            return lastMessage.Role == CompletionRole.User
                ? ConversationManager.CurrentConversation.FindByGuid(lastMessage.Parent)
                : lastMessage;
        }

        private void CreateNewConversationWithLastMessages(CompletionMessage assistantMessage, CompletionMessage userMessage)
        {
            ConversationManager.CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };

            var newUserMessage = CreateNewMessage(userMessage, CompletionRole.User);
            var newAssistantMessage = CreateNewMessage(assistantMessage, CompletionRole.Assistant, newUserMessage.Guid);

            newUserMessage.Children.Add(newAssistantMessage.Guid);

            ConversationManager.CurrentConversation.Messages.Add(newUserMessage);
            ConversationManager.CurrentConversation.Messages.Add(newAssistantMessage);

            ConversationManager.PreviousCompletion = newAssistantMessage;
        }

        private CompletionMessage CreateNewMessage(CompletionMessage originalMessage, CompletionRole role, string parentGuid = null)
        {
            return new CompletionMessage
            {
                Parent = parentGuid,
                Role = role,
                Content = originalMessage.Content,
                Engine = originalMessage.Engine,
                Guid = Guid.NewGuid().ToString(),
                Children = new List<string>()
            };
        }

        private void buttonAttachImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = ImageHelpers.ShowAttachImageDialog();

            if (openFileDialog.FileName != "")
            {
                Base64Image = ImageHelpers.ImageToBase64(openFileDialog.FileName);
                Base64ImageType = ImageHelpers.GetImageType(openFileDialog.FileName);
            }
            else
            {
                Base64Image = "";
                Base64ImageType = "";
            }
        }

        private void tbSearch_TextChanged(object sender, EventArgs e)
        {
            FilterConversations(tbSearch.Text);
        }

        private void FilterConversations(string searchText)
        {
            foreach (DataGridViewRow row in dgvConversations.Rows)
            {
                if (row.Cells[0].Value == null) continue;

                var guid = row.Cells[0].Value.ToString();
                var conv = BranchedConversation.LoadConversation(guid);
                var allMessages = conv.Messages.Select(m => m.Content).ToList();

                row.Visible = allMessages.Any(m => m.Contains(searchText, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        private void btnClearSearch_Click(object sender, EventArgs e) => tbSearch.Clear();
    }
}