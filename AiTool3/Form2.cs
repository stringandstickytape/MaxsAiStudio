using AiTool3.ApiManagement;
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
using System.Reflection;
using System.Text;
using System.Drawing.Drawing2D;
using System.Linq.Expressions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace AiTool3
{
    public partial class Form2 : Form
    {
        public ConversationManager ConversationManager { get; set; } = new ConversationManager();
        public Settings.Settings CurrentSettings { get; set; } = AiTool3.Settings.Settings.Load();

        public TopicSet TopicSet { get; set; }

        private AudioRecorderManager audioRecorderManager = new AudioRecorderManager(GgmlType.TinyEn);

        public string Base64Image { get; set; }
        public string Base64ImageType { get; set; }

        private AudioRecorder recorder;
        private CancellationTokenSource _cts;
        private Task recordingTask;
        private bool isRecording = false;

        private WebViewManager webViewManager = null;

        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();


        public Form2()
        {
            InitializeComponent();

            webViewManager = new WebViewManager(ndcWeb);

            //SetPaperclipIcon(buttonAttachImage);

            rtbSystemPrompt.SetOverlayText("System Prompt");
            rtbInput.SetOverlayText("User Input");
            rtbOutput.SetOverlayText("AI Response");

            audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

            SetButtonIcon(IconChar.Paperclip, buttonAttachImage);
            SetButtonIcon(IconChar.PaperPlane, btnGo);
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

            rtbInput.KeyDown += (s, e) =>
            {
                CheckForCtrlReturn(e);
            };
            this.KeyDown += (s, e) =>
            {
                CheckForCtrlReturn(e);
            };

            DataGridViewHelper.InitialiseDataGridView(dgvConversations);

            InitialiseMenus();

            CreateNewWebNdc(CurrentSettings.ShowDevTools);

            BeginNewConversation();

            updateTimer.Interval = 100; // Update every 100 milliseconds
            updateTimer.Tick += UpdateTimer_Tick;

        }

        private void AutoSuggestStringSelected(string selectedString)
        {
            // put selected string into the input box, invoking if necessary
            if (rtbInput.InvokeRequired)
            {
                rtbInput.Invoke(new Action(() =>
                {
                    rtbInput.Text = selectedString;
                }));
            }
            else
            {
                rtbInput.Text = selectedString;
            }

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
            switch(e.MenuOption)
            {
                case "saveTxt":
                    var nodes = ConversationManager.GetParentNodeList();
                    var json = JsonConvert.SerializeObject(nodes);

                    // pretty-print the conversation from the nodes list
                    string conversation = nodes.Aggregate("", (acc, node) => acc + $"{node.Role.ToString()}: {node.Content}" + "\n\n");

                    // get a filename from the user
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, conversation);
                        // open the file in default handler
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                    break;
                case "saveHtml":
                    var nodes2 = ConversationManager.GetParentNodeList();
                    var json2 = JsonConvert.SerializeObject(nodes2);

                    StringBuilder htmlBuilder = new StringBuilder();
                    htmlBuilder.Append(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Conversation Export</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #e0e0e0;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #1a1a1a;
        }
        .conversation {
            background-color: #1a1a1a;
            border-radius: 8px;
            overflow: hidden;
        }
        .message {
            background-color: #2a2a2a;
            padding: 15px;
            margin-bottom: 10px;
            border-radius: 12px;
            transition: max-height 1s ease, background-color 0.5s ease;
            max-height: 200px;
            overflow: hidden;
            cursor: pointer;
            position: relative;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
        }
        .message.expanded {
            max-height: 2000px;
        }
        .message:last-child {
            margin-bottom: 0;
        }
        .message:hover {
            background-color: #333333;
        }
        .role {
            font-weight: bold;
            color: #b0b0b0;
            margin-bottom: 5px;
        }
        .content {
            white-space: pre-wrap;
        }
        .human .role {
            color: #64b5f6;
        }
        .assistant .role {
            color: #81c784;
        }
        .more {
            position: absolute;
            bottom: 0;
            left: 0;
            right: 0;
            height: 40px;
            background: linear-gradient(to bottom, rgba(42, 42, 42, 0) 0%, rgba(42, 42, 42, 1) 100%);
            display: flex;
            align-items: flex-end;
            justify-content: center;
            padding-bottom: 5px;
            transition: opacity 0.8s ease;
            border-bottom-left-radius: 12px;
            border-bottom-right-radius: 12px;
        }
        .more span {
            background-color: rgba(255, 255, 255, 0.2);
            color: #e0e0e0;
            padding: 2px 8px;
            border-radius: 10px;
            font-size: 14px;
            transition: transform 0.8s ease;
        }
        .message.expanded .more {
            opacity: 0;
            pointer-events: none;
        }
        .message.expanded .more span {
            transform: rotate(180deg);
        }
        @media (max-width: 600px) {
            body {
                padding: 10px;
            }
            .message {
                padding: 10px;
                margin-bottom: 8px;
            }
        }
    </style>
    <script>
        function toggleExpand(element) {
            element.classList.toggle('expanded');
            checkOverflow(element);
        }

        function checkOverflow(element) {
            const content = element.querySelector('.content');
            const more = element.querySelector('.more');
            if (content.scrollHeight > element.clientHeight) {
                more.style.display = 'flex';
            } else {
                more.style.display = 'none';
            }
        }

        window.onload = function() {
            document.querySelectorAll('.message').forEach(checkOverflow);
        };
    </script>
</head>
<body>
    <div class='conversation'>
");

                    foreach (var node in nodes2.Where(x => !x.Omit))
                    {
                        string roleClass = node.Role.ToString().ToLower();
                        htmlBuilder.Append($@"
        <div class='message {roleClass}' onclick='toggleExpand(this)'>
            <div class='role'>{node.Role}:</div>
            <div class='content'>{System.Web.HttpUtility.HtmlEncode(node.Content)}</div>
            <div class='more'><span>more...</span></div>
        </div>
");
                    }

                    htmlBuilder.Append(@"
    </div>
</body>
</html>
");

                    SaveFileDialog saveFileDialog2 = new SaveFileDialog();
                    saveFileDialog2.Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*";
                    saveFileDialog2.RestoreDirectory = true;
                    if (saveFileDialog2.ShowDialog() == DialogResult.OK)
                    {
                        System.IO.File.WriteAllText(saveFileDialog2.FileName, htmlBuilder.ToString());
                        Process.Start(new ProcessStartInfo(saveFileDialog2.FileName) { UseShellExecute = true });
                    }
                    break;
                default:
                    throw new NotImplementedException();
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

        private void AudioRecorderManager_AudioProcessed(object? sender, string e)
        {
            if (rtbInput.InvokeRequired)
            {
                rtbInput.Invoke(new Action(() =>
                {
                    rtbInput.Text += e;
                }));
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
                if (e.SelectedOption == "Save this branch as TXT")
                {

                }
                if (e.SelectedOption == "Save this branch as HTML")
                {

                }



                else if (e.SelectedOption == "Disable")
                {
                    var selectedGuid = e.SelectedNode.Guid;
                    var selectedMessage = ConversationManager.CurrentConversation.FindByGuid(selectedGuid);
                    selectedMessage.Omit = !selectedMessage.Omit;
                    e.SelectedNode.IsDisabled = selectedMessage.Omit;

                    DrawNetworkDiagram();
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
                btnClear_Click(null, null);
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
            var specialsMenu = new ToolStripMenuItem("Specials");
            specialsMenu.BackColor = Color.Black;
            specialsMenu.ForeColor = Color.White;
            var restartMenuItem = new ToolStripMenuItem("Pull Readme and update from latest diff");
            restartMenuItem.ForeColor = Color.White;
            restartMenuItem.BackColor = Color.Black;
            restartMenuItem.Click += async (s, e) =>
            {
                AiResponse response, response2;
                response = await SpecialsHelper.GetReadmeResponses((Model)cbEngine.SelectedItem);
                var snippets = FindSnippets(rtbOutput, response.ResponseText, null, null);
                
                try
                {
                    var code = snippets.First().Code;
                    // remove first and last lines
                    code = SnipperHelper.StripFirstAndLastLine(code);
                    // get first snippet
                    File.WriteAllText(@"C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\README.md", code);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error writing to file: {ex.Message}");
                }
            };

            AddSpecial(specialsMenu, "Review Code", (s, e) =>
                {
                    // go up from the working directory until you get to "MaxsAiTool"
                    SpecialsHelper.ReviewCode((Model)cbEngine.SelectedItem, out string userMessage);
                    rtbInput.Text = userMessage;
                });
            AddSpecial(specialsMenu, "Rewrite Summaries", (s, e) =>
            {
                ConversationManager.RegenerateAllSummaries((Model)cbEngine.SelectedItem, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations);
            });

            AddSpecial(specialsMenu, "Autosuggest",async (s, e) =>
            {
                var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbEngine.SelectedItem, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations);
                autoSuggestForm.StringSelected += AutoSuggestStringSelected;
            });

            AddSpecial(specialsMenu, "Autosuggest (Fun)", async (s, e) =>
            {
                var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbEngine.SelectedItem, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, true);
                autoSuggestForm.StringSelected += AutoSuggestStringSelected;
            });

            AddSpecial(specialsMenu, "Autosuggest (User-Specified)", async (s, e) =>
            {
                var userInputForm = new AutoSuggestUserInput();

                var prefix = "you are a bot who makes ";
                var suffix = " suggestions on how a user might proceed with a conversation.";
                userInputForm.Controls["label1"].Text = prefix;
                userInputForm.Controls["label2"].Text = suffix;
                var result = userInputForm.ShowDialog();

                if(result == DialogResult.OK)
                {
                    var userAutoSuggestPrompt = userInputForm.Controls["tbAutoSuggestUserInput"].Text;

                    userAutoSuggestPrompt = $"{prefix}{userAutoSuggestPrompt}{suffix}";

                    var autoSuggestForm = await ConversationManager.Autosuggest((Model)cbEngine.SelectedItem, CurrentSettings.GenerateSummariesUsingLocalAi, dgvConversations, true, userAutoSuggestPrompt);
                    autoSuggestForm.StringSelected += AutoSuggestStringSelected;
                }


            });

            AddSpecial(specialsMenu, "Set Code Highlight Colours (experimental)", async (s, e) =>
            {
                CSharpHighlighter.ConfigureColors();
            });

            // based on our conversation so far, give me ten things I might ask you to do next, in a bullet-point list


            specialsMenu.DropDownItems.Add(restartMenuItem);
            menuBar.Items.Add(specialsMenu);

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
            foreach (var model in CurrentSettings.ApiList.SelectMany(x => x.Models))
            {
                cbEngine.Items.Add(model);
            }

            // preselect the first Local api
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
            // for each split container incl in child items
            splitContainer1.Paint += new PaintEventHandler(SplitContainer_Paint);
            splitContainer2.Paint += new PaintEventHandler(SplitContainer_Paint);
            splitContainer3.Paint += new PaintEventHandler(SplitContainer_Paint);
            splitContainer5.Paint += new PaintEventHandler(SplitContainer_Paint);
        }

        private void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            SplitContainer sc = sender as SplitContainer;

            Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

            using (SolidBrush brush = new SolidBrush(Color.Gray))
            {
                e.Graphics.FillRectangle(brush, splitterRect);
            }
        }

        private void NdcConversation_NodeClicked(object? sender, NodeClickEventArgs e)
        {

            var clickedCompletion = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Guid == e.ClickedNode.Guid);
            ConversationManager.PreviousCompletion = clickedCompletion;

            rtbInput.Clear();
            if (ConversationManager.PreviousCompletion.Role == CompletionRole.User)
            {
                rtbInput.Text = ConversationManager.PreviousCompletion.Content;

                ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion.Parent);
            }
            if (ConversationManager.PreviousCompletion?.SystemPrompt != null)
            {
                rtbSystemPrompt.Text = ConversationManager.PreviousCompletion.SystemPrompt;
            }
            else rtbSystemPrompt.Text = "";
            FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(ConversationManager.PreviousCompletion?.Content ?? ""), clickedCompletion.Guid, ConversationManager.CurrentConversation.Messages);
        }

        private void WebViewNdc_WebNdcNodeClicked(object? sender, WebNdcNodeClickedEventArgs e)
        {
            var clickedCompletion = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Guid == e.NodeId);
            if (clickedCompletion == null)
                return;
            ConversationManager.PreviousCompletion = clickedCompletion;

            rtbInput.Clear();
            if (ConversationManager.PreviousCompletion.Role == CompletionRole.User)
            {
                rtbInput.Text = ConversationManager.PreviousCompletion.Content;

                ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion.Parent);
            }
            if (ConversationManager.PreviousCompletion?.SystemPrompt != null)
            {
                rtbSystemPrompt.Text = ConversationManager.PreviousCompletion.SystemPrompt;
            }
            else rtbSystemPrompt.Text = "";
            FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(ConversationManager.PreviousCompletion?.Content ?? ""), clickedCompletion.Guid, ConversationManager.CurrentConversation.Messages);
        }

        private SnippetManager snippetManager = new SnippetManager();

        public List<Snippet> FindSnippets(ButtonedRichTextBox richTextBox, string text, string messageGuid, List<CompletionMessage> messages)
        {
            richTextBox.Clear();
            richTextBox.Text = text;
            var snippets = snippetManager.FindSnippets(text);

            // Apply UI formatting
            foreach (var snippet in snippets.Snippets)
            {   // snippet.Type == "html"?

                int startIndex = 0;

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



        private async void btnGo_Click(object sender, EventArgs e)
        {
            // Cancel any ongoing operation
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            stopwatch.Restart();
            updateTimer.Start();

            CompletionMessage completionResponse = null;
            AiResponse? response = null;
            Model model = null;

            try
            {
                btnGo.Enabled = false;
                
                btnCancel.Visible = true; // Enable cancel button
                model = (Model)cbEngine.SelectedItem;

                // get the name of the service for the model
                var serviceName = model.ServiceName;

                // instantiate the service from the appropriate api
                var aiService = AiServiceResolver.GetAiService(serviceName);

                Conversation conversation = null;

                conversation = new Conversation();
                conversation.systemprompt = rtbSystemPrompt.Text;
                conversation.messages = new List<ConversationMessage>();
                List<CompletionMessage> nodes = ConversationManager.GetParentNodeList();

                Debug.WriteLine(nodes);

                foreach (var node in nodes)
                {
                    if (node.Role == CompletionRole.Root || node.Omit)
                        continue;

                    conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = node.Content });
                }
                conversation.messages.Add(new ConversationMessage { role = "user", content = rtbInput.Text });

                var previousCompletionGuidBeforeAwait = ConversationManager.PreviousCompletion?.Guid;
                var inputText = rtbInput.Text;
                var systemPrompt = rtbSystemPrompt.Text;
                // fetch the response from the api
                response = await aiService.FetchResponse(model, conversation, Base64Image, Base64ImageType, _cts.Token, rtbOutput, CurrentSettings.StreamResponses);

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
                    SystemPrompt = systemPrompt,
                    InputTokens = response.TokenUsage.InputTokens,
                    OutputTokens = 0,
                    CreatedAt = DateTime.Now,
                };

                if (response == null)
                {
                    MessageBox.Show("Response is null");
                    btnGo.Enabled = true;
                    return;
                }
                var pc = ConversationManager.CurrentConversation.FindByGuid(previousCompletionGuidBeforeAwait);

                if (pc != null)
                {
                    pc.Children.Add(completionInput.Guid);
                }

                ConversationManager.CurrentConversation.Messages.Add(completionInput);

                // Create a new completion object to store the response in
                completionResponse = new CompletionMessage
                {
                    Role = CompletionRole.Assistant,
                    Content = response.ResponseText,
                    Parent = completionInput.Guid,
                    Engine = model.ModelName,
                    Guid = System.Guid.NewGuid().ToString(),
                    Children = new List<string>(),
                    SystemPrompt = systemPrompt,
                    InputTokens = 0,
                    OutputTokens = response.TokenUsage.OutputTokens,
                    TimeTaken = stopwatch.Elapsed,
                    CreatedAt = DateTime.Now,
                };

                // add it to the current conversation
                ConversationManager.CurrentConversation.Messages.Add(completionResponse);

                // and display the results in the output box
                FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(string.Join("\r\n", response.ResponseText)), completionResponse.Guid, ConversationManager.CurrentConversation.Messages);

                if (CurrentSettings.NarrateResponses)
                {
                    // do this but in a new thread:                 TtsHelper.ReadAloud(rtbOutput.Text);
                    var text = rtbOutput.Text;
                    Task.Run(() => TtsHelper.ReadAloud(text));
                }

                completionInput.Children.Add(completionResponse.Guid);

                ConversationManager.PreviousCompletion = completionResponse;

                Base64Image = null;
                Base64ImageType = null;

                // draw the network diagram
                DrawNetworkDiagram();
                var a = await WebNdcDrawNetworkDiagram();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation was cancelled.");
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
                return;
            }
            finally
            {
                stopwatch.Stop();
                updateTimer.Stop();
                TimeSpan ts = stopwatch.Elapsed;
                btnGo.Enabled = true;
                btnCancel.Visible = false; // Disable cancel button
            }

            webViewManager.CentreOnNode(completionResponse.Guid);

            var summaryModel = CurrentSettings.ApiList.First(x => x.ApiName.StartsWith("Ollama")).Models.First();

            string title;
            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.CurrentConversation.ConvGuid);

            // using the title, update the dgvConversations

            ConversationManager.SaveConversation();

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.CurrentConversation.ConvGuid, ConversationManager.CurrentConversation.Messages[0].Content, ConversationManager.CurrentConversation.Messages[0].Engine, "");

                row = dgvConversations.Rows[0];
            }

            var cost = model.GetCost(response.TokenUsage);

            tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out";

            btnGo.Enabled = true;

            if (row != null && row.Cells[3].Value != null && string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
            {
                row.Cells[3].Value = await ConversationManager.GenerateConversationSummary(summaryModel, CurrentSettings.GenerateSummariesUsingLocalAi);
            }
        }

        private async Task<bool> WebNdcDrawNetworkDiagram()
        {
            if (webViewManager == null || webViewManager.webView.CoreWebView2 == null) return false;

            var a = await webViewManager.Clear();

            var nodes = ConversationManager.CurrentConversation.Messages
                .Where(x => x.Role != CompletionRole.Root)
                .Select(m => new IdNodeRole { id = m.Guid, label = m.Content, role = m.Role.ToString(), colour = m.GetColorHexForEngine() }).ToList();

            var links2 = ConversationManager.CurrentConversation.Messages
                .Where(x => x.Parent != null)
                .Select(x => new Link { source = x.Parent, target = x.Guid }).ToList();


            await webViewManager.EvaluateJavascriptAsync($"addNodes({JsonConvert.SerializeObject(nodes)});");
            await webViewManager.EvaluateJavascriptAsync($"addLinks({JsonConvert.SerializeObject(links2)});");
            return true;
        }


        private void DrawNetworkDiagram()
        {
            //// Clear the diagram
            //ndcConversation.Clear();
            //
            //var root = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Parent == null);
            //if (root == null)
            //{
            //    return;
            //}
            //var y = 100;
            //
            //var rootNode = new Node(root.Content, new Point(300, y), root.Guid, root.InfoLabel, root.Omit);
            //
            //// get the model with the same name as the engine
            //var model = CurrentSettings.ApiList.SelectMany(c => c.Models).Where(x => x.ModelName == root.Engine).FirstOrDefault();
            //
            //rootNode.BackColor = root.GetColorForEngine();
            //ndcConversation.AddNode(rootNode);
            //
            //// recursively draw the children
            //DrawChildren(root, rootNode, 300 + 100, ref y);


        }

        private void DrawChildren(CompletionMessage root, Node rootNode, int v, ref int y)
        {
            //y += 130;
            //foreach (var child in root.Children)
            //{
            //    // get from child string
            //    var childMsg = ConversationManager.CurrentConversation.Messages.FirstOrDefault(c => c.Guid == child);
            //
            //    var childNode = new Node(childMsg.Content, new Point(v, y), childMsg.Guid, childMsg.InfoLabel, childMsg.Omit);
            //    childNode.BackColor = childMsg.GetColorForEngine();
            //    ndcConversation.AddNode(childNode);
            //    ndcConversation.AddConnection(rootNode, childNode);
            //    DrawChildren(childMsg, childNode, v + 100, ref y);
            //}
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            BeginNewConversation();
            PopulateUiForTemplate(GetCurrentlySelectedTemplate());
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

        private void buttonNewKeepAll_Click(object sender, EventArgs e)
        {
            var lastAssistantMessage = ConversationManager.PreviousCompletion;
            var lastUserMessage = ConversationManager.CurrentConversation.FindByGuid(lastAssistantMessage.Parent);
            if (lastUserMessage == null)
                return;
            if (lastAssistantMessage.Role == CompletionRole.User)
                lastAssistantMessage = ConversationManager.CurrentConversation.FindByGuid(ConversationManager.PreviousCompletion.Parent);

            

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
            rootMessage.Children.Add(userMessage.Guid);
            assistantMessage.Parent = userMessage.Guid;
            userMessage.Children.Add(assistantMessage.Guid);

            ConversationManager.CurrentConversation.Messages.AddRange(new[] { assistantMessage, userMessage });
            ConversationManager.PreviousCompletion = assistantMessage;

            DrawNetworkDiagram();
            var a = WebNdcDrawNetworkDiagram().Result;
        }

        private async Task BeginNewConversation()
        {
            rtbInput.Clear();
            rtbSystemPrompt.Clear();
            rtbOutput.Clear();

            ConversationManager.CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            ConversationManager.CurrentConversation.AddNewRoot();
            ConversationManager.PreviousCompletion = ConversationManager.CurrentConversation.Messages.First();

            DrawNetworkDiagram();
            await WebNdcDrawNetworkDiagram();
        }

        private async void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var clickedGuid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();

            ConversationManager.LoadConversation(clickedGuid);

            DrawNetworkDiagram();
            await WebNdcDrawNetworkDiagram();

            //ndcConversation.FitAll();
        }

        private void cbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            // populate the cbTemplates with the templates for the selected category
            var selected = cbCategories.SelectedItem.ToString();

            var topics = TopicSet.Topics.Where(t => t.Name == selected).ToList();

            var templates = topics.SelectMany(t => t.Templates).Where(x => x.SystemPrompt != null).ToList();

            cbTemplates.Items.Clear();
            cbTemplates.Items.AddRange(templates.Select(t => t.TemplateName).ToArray());
            cbTemplates.DroppedDown = true;
        }

        private void cbTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnClear_Click(null, null);
            PopulateUiForTemplate(GetCurrentlySelectedTemplate());
        }

        private ConversationTemplate GetCurrentlySelectedTemplate()
        {
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null)
            {
                return null;
            }
            return TopicSet.Topics.First(t => t.Name == cbCategories.SelectedItem.ToString()).Templates.First(t => t.TemplateName == cbTemplates.SelectedItem.ToString());
        }

        private void PopulateUiForTemplate(ConversationTemplate template)
        {
            rtbInput.Clear();
            rtbSystemPrompt.Clear();
            if (template != null)
            {
                rtbInput.Text = template.InitialPrompt;
                rtbSystemPrompt.Text = template.SystemPrompt;
            }
        }

        private void buttonEditTemplate_Click(object sender, EventArgs e)
        {
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null) return;

            EditAndSaveTemplate(GetCurrentTemplate());
        }

        private ConversationTemplate GetCurrentTemplate()
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
            var r = ShowAttachmentDialog();

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
                            sb.Append("```");
                            sb.Append(Path.GetFileName(file));
                            sb.Append(Environment.NewLine);
                            sb.Append(File.ReadAllText(file));
                            sb.Append(Environment.NewLine);
                            sb.Append("```");
                            sb.Append(Environment.NewLine);
                            sb.Append(Environment.NewLine);
                        }
                        rtbInput.Text = $"{sb.ToString()}{rtbInput.Text}";

                        CurrentSettings.SetDefaultPath(Path.GetDirectoryName(attachTextFilesDialog.FileName));

                    }



                    break;
                case DialogResult.Cancel:
                    break;
            }





        }
        private CancellationTokenSource _cts2;

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
                        // Check if cancellation was requested
                        _cts2.Token.ThrowIfCancellationRequested();

                        if (row.Cells[0].Value == null) continue;

                        var guid = row.Cells[0].Value.ToString();

                        var conv = BranchedConversation.LoadConversation(guid);

                        var allMessages = conv.Messages.Select(m => m.Content).ToList();

                        //bool isVisible = allMessages.Any(m => m.Contains(tbSearch.Text, StringComparison.InvariantCultureIgnoreCase));
                        bool isVisible = false;
                        foreach (string message in allMessages)
                        {
                            if (message.IndexOf(tbSearch.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
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
            catch (OperationCanceledException)
            {
                Debug.WriteLine("OCE");
                // Operation was cancelled, do nothing
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void btnClearSearch_Click(object sender, EventArgs e) => tbSearch.Clear();

        public static DialogResult ShowAttachmentDialog()
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Attach image or text?",
                StartPosition = FormStartPosition.CenterScreen
            };

            Button imageButton = new Button() { Left = 50, Top = 30, AutoSize = true, Text = "Image" };
            Button textButton = new Button() { Left = 150, Top = 30, AutoSize = true, Text = "Text" };
            Button cancelButton = new Button() { Left = 260, Top = 30, AutoSize = true, Text = "Cancel" };

            imageButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Yes; };
            textButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.No; };
            cancelButton.Click += (sender, e) => { prompt.DialogResult = DialogResult.Cancel; };

            prompt.Controls.Add(imageButton);
            prompt.Controls.Add(textButton);
            prompt.Controls.Add(cancelButton);

            return prompt.ShowDialog();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            btnCancel.Visible = false;
        }

        public static void SetPaperclipIcon(Button button)
        {
            try
            {
                int width = 200;
                int height = 300;

                using (Bitmap bmp = new Bitmap(width, height))
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.StartFigure();
                        path.AddBezier(20, 40, 20, 20, 40, 20, 60, 20);
                        path.AddLine(140, 20, 140, 20);
                        path.AddBezier(160, 20, 160, 40, 160, 40, 160, 80);
                        path.AddLine(160, 220, 160, 220);
                        path.AddBezier(160, 240, 140, 240, 140, 240, 100, 240);
                        path.AddBezier(80, 240, 80, 220, 80, 220, 80, 80);
                        path.AddBezier(80, 60, 100, 60, 100, 60, 120, 60);
                        path.AddBezier(140, 60, 140, 80, 140, 80, 140, 180);
                        path.AddBezier(140, 200, 120, 200, 120, 200, 60, 200);
                        path.AddBezier(40, 200, 40, 180, 40, 180, 40, 40);
                        path.CloseFigure();

                        using (Pen pen = new Pen(Color.White, 16))
                        {
                            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                            pen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                            g.DrawPath(pen, path);
                        }
                    }

                    //Bitmap bmp2 = new Bitmap(24, 24);
                    //using (Graphics g2 = Graphics.FromImage(bmp2))
                    //{
                    //    g2.DrawImage(bmp, 0, 0, 24, 24);
                    //}
                    //
                    //button.Image = bmp2;

                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"Error setting paperclip icon: {ex.Message}");
            }
        }

        



        private async Task<bool> CreateNewWebNdc(bool showDevTools)
        {
            //if (webViewNdc != null)
            //{
            //    if (!webViewNdc.IsDisposed)
            //    {
            //        webViewNdc.Dispose();
            //    }
            //}
            string js = GetEmbeddedAssembly("AiTool3.JavaScript.NetworkDiagramJavascriptControl.js");
            var css = GetEmbeddedAssembly("AiTool3.JavaScript.NetworkDiagramCssControl.css");

            
            string html = GetEmbeddedAssembly("AiTool3.JavaScript.NetworkDiagramHtmlControl.html");
            string htmlAndCss = html.Replace("{magiccsstoken}", css);
            string result = htmlAndCss.Replace("<insertscripthere />", js);

            await webViewManager.OpenWebViewWithJs("", showDevTools);

            //Thread.Sleep(2000);

            webViewManager.NavigateToHtml(result);

            webViewManager.WebNdcContextMenuOptionSelected += WebViewNdc_WebNdcContextMenuOptionSelected;
            webViewManager.WebNdcNodeClicked += WebViewNdc_WebNdcNodeClicked;



            return true;
        }

        private static string GetEmbeddedAssembly(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);

            string result = "";
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }

 
        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            webViewManager.webView.Dispose();
        }
    }

    public class IdNodeRole
    {
        public string role { get; set; }

        public string id { get; set; }
        public string label { get; set; }

        public string colour { get; set; }
    }
    public class Link
    {
        public string source { get; set; }
        public string target { get; set; }
    }
}
