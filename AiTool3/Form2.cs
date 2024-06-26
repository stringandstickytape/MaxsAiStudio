﻿using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Threading.Tasks;
using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using AiTool3.Settings;
using AiTool3.Topics;
using AiTool3.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AiTool3.UI.NetworkDiagramControl;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;


namespace AiTool3
{
    public partial class Form2 : Form
    {

        public BranchedConversation CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
        public CompletionMessage PreviousCompletion;

        public SettingsManager Settings { get; set; } = SettingsManager.ReadFromJson();

        public TopicSet TopicSet { get; set; }

        public Form2()
        {
            InitializeComponent();

            // if topics.json exists, load it
            TopicSet = TopicSet.Load();

            foreach (var topic in TopicSet.Topics)
            {
                cbCategories.Items.Add(topic.Name);
            }

            InitialiseApiList();

            ndcConversation.NodeClicked += NdcConversation_NodeClicked;

            SetSplitContainerEvents();

            rtbInput.KeyDown += (s, e) =>
            {
                CheckForCtrlReturn(e);
            };
            this.KeyDown += (s, e) =>
            {
                CheckForCtrlReturn(e);
            };


            // hide dgv headers
            dgvConversations.ColumnHeadersVisible = false;
            // Setting the default cell style for the DataGridView
            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            cellStyle.BackColor = System.Drawing.Color.Black;
            cellStyle.ForeColor = System.Drawing.Color.White;
            cellStyle.WrapMode = DataGridViewTriState.True;

            dgvConversations.DefaultCellStyle = cellStyle;

            // add cols to dgv
            dgvConversations.Columns.Add("ConvGuid", "ConvGuid");
            dgvConversations.Columns.Add("Content", "Content");
            dgvConversations.Columns.Add("Engine", "Engine");
            dgvConversations.Columns.Add("Title", "Title");
            dgvConversations.Columns[0].Visible = false;
            dgvConversations.Columns[1].Visible = false;
            dgvConversations.Columns[2].Visible = false;
            // make the last column fill the parent
            dgvConversations.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;


            // make the columns wrap text
            //dgvConversations.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvConversations.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            // make the selection column thin
            dgvConversations.RowHeadersWidth = 10;


            // populate dgvConversations with the conversation files in the current directory, ordered by date desc
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "v3-conversation-*.json").OrderByDescending(f => new FileInfo(f).LastWriteTime);
            foreach (var file in files)
            {
                var conv = JsonConvert.DeserializeObject<BranchedConversation>(File.ReadAllText(file));
                if (!conv.Messages.Any())
                    continue;

                dgvConversations.Rows.Add(conv.ConvGuid, conv.Messages[0].Content, conv.Messages[0].Engine, conv.Title);


            }

            InitialiseMenus();
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
                var settingsForm = new SettingsForm(Settings);
                var result = settingsForm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    Settings = settingsForm.NewSettings;
                    SettingsManager.WriteToJson(Settings);
                }
            };

            fileMenu.DropDownItems.Add(quitMenuItem);
            editMenu.DropDownItems.Add(clearMenuItem);
            editMenu.DropDownItems.Add(settingsMenuItem);
            menuBar.Items.Add(fileMenu);
            menuBar.Items.Add(editMenu);
        }

        private void InitialiseApiList()
        {
            foreach (var api in Settings.ApiList)
            {
                foreach (var model in api.Models)
                {
                    cbEngine.Items.Add(model);
                }
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
            splitContainer1.Paint += new PaintEventHandler(SplitContainer_Paint);
            splitContainer2.Paint += new PaintEventHandler(SplitContainer_Paint);
            splitContainer3.Paint += new PaintEventHandler(SplitContainer_Paint);
            splitContainer5.Paint += new PaintEventHandler(SplitContainer_Paint);
        }

        private void SplitContainer_Paint(object sender, PaintEventArgs e)
        {
            SplitContainer sc = sender as SplitContainer;

            // Set the splitter color
            Color splitterColor = Color.Yellow;

            // Get the position and size of the splitter
            Rectangle splitterRect = sc.Orientation == Orientation.Horizontal
                ? new Rectangle(0, sc.SplitterDistance, sc.Width, sc.SplitterWidth)
                : new Rectangle(sc.SplitterDistance, 0, sc.SplitterWidth, sc.Height);

            // Draw the splitter
            using (SolidBrush brush = new SolidBrush(splitterColor))
            {
                e.Graphics.FillRectangle(brush, splitterRect);
            }
        }

        // summarise our conversation in six words or fewer as a json object

        private void NdcConversation_NodeClicked(object? sender, NodeClickEventArgs e)
        {

            var clickedCompletion = CurrentConversation.Messages.FirstOrDefault(c => c.Guid == e.ClickedNode.Guid);
            PreviousCompletion = clickedCompletion;
            if (PreviousCompletion.Role == CompletionRole.User)
            {
                rtbInput.Text = PreviousCompletion.Content;

                PreviousCompletion = CurrentConversation.FindByGuid(PreviousCompletion.Parent);
            }
            else
            {
                rtbInput.Text = "";
            }

            FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(PreviousCompletion?.Content ?? ""));


        }


        public List<Snippet> FindSnippets(RichTextBox richTextBox, string v)
        {
            richTextBox.Text = v;
            string pattern = @"```(.*?)```";
            List<Snippet> snippets = new List<Snippet>();

            // Save the current selection start and length
            int originalSelectionStart = richTextBox.SelectionStart;
            int originalSelectionLength = richTextBox.SelectionLength;

            // Use Regex to find sections between triple backticks
            var matches = Regex.Matches(v, pattern, RegexOptions.Singleline);

            foreach (Match match in matches) // each of these is a snippet
            {
                if (match.Groups.Count > 1)
                {
                    // Calculate the start index and length of the matched section
                    int startIndex = match.Groups[1].Index;
                    int length = match.Groups[1].Length;

                    // get the line of text before startIndex
                    var line = richTextBox.GetLineFromCharIndex(startIndex);
                    var lineStart = richTextBox.GetFirstCharIndexFromLine(line);
                    var lineEnd = richTextBox.GetFirstCharIndexFromLine(line + 1);
                    string lineText = richTextBox.Text.Substring(lineStart, lineEnd - lineStart);

                    // get the line before that
                    var lineBefore = richTextBox.GetLineFromCharIndex(lineStart - 1);
                    var lineBeforeStart = richTextBox.GetFirstCharIndexFromLine(lineBefore);
                    var lineBeforeEnd = richTextBox.GetFirstCharIndexFromLine(lineBefore + 1);
                    string lineBeforeText = richTextBox.Text.Substring(lineBeforeStart, lineBeforeEnd - lineBeforeStart);

                    string type = null;
                    string filename = null;

                    // if linebeforetext is of the form "### HTML (index.html)\n", get the type and filename into variables
                    var match2 = Regex.Match(lineBeforeText, @"### (.*?) \((.*?)\)");
                    if (match2.Success)
                    {
                        type = match2.Groups[1].Value;
                        filename = match2.Groups[2].Value;
                    }

                    // get the text from startindex to length
                    var text = richTextBox.Text.Substring(startIndex, length);

                    // Collect the snippet
                    snippets.Add(new Snippet
                    {
                        Type = type,
                        Filename = filename,
                        Code = text.Trim()
                    });

                    // Select the matched section in the RichTextBox
                    richTextBox.Select(startIndex, length);

                    // Set the font to Courier New for the selected section, yellow
                    richTextBox.SelectionColor = Color.Yellow;
                    richTextBox.SelectionFont = new Font("Courier New", richTextBox.SelectionFont?.Size ?? 10);
                }
            }

            // Restore the original selection
            richTextBox.Select(originalSelectionStart, originalSelectionLength);

            panelSnippets.Controls.Clear();
            foreach (var snippet in snippets)
            {
                var button = new Button();
                button.Text = $"Copy {snippet.Type ?? "Text"}";
                button.ForeColor = Color.White;

                // autosize the height
                button.AutoSize = true;
                button.AutoSizeMode = AutoSizeMode.GrowAndShrink;


                button.Tag = snippet;
                button.Click += (s, e) =>
                {
                    var snip = (Snippet)((Button)s).Tag;
                    // copy to clipboard
                    Clipboard.SetText(snip.Code);
                };
                panelSnippets.Controls.Add(button);

                // add another button which, when clicked, writes the contents to a temp file and launches it in Chrome
                //if (snippet.Type == "HTML")
                //{
                var button2 = new Button();
                button2.Text = $"View";
                button2.ForeColor = Color.White;
                button2.BackColor = Color.Blue;
                button2.AutoSize = true;
                button2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button2.Tag = snippet;
                button2.Click += (s, e) =>
                {
                    LaunchHtml(s);

                };
                // position button2 below button
                button2.Location = new Point(button.Location.X, button.Location.Y + button.Height + 5);
                panelSnippets.Controls.Add(button2);

                // launch c#
                var button3 = new Button();
                button3.Text = $"Run C#";
                button3.ForeColor = Color.White;
                button3.BackColor = Color.Blue;
                button3.AutoSize = true;
                button3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                button3.Tag = snippet;
                button3.Click += (s, e) =>
                {
                    var code = snippet.Code;
                    if (code.StartsWith("csharp\n"))
                        code = code.Substring(7);
                    LaunchCSharp(code);

                };
                // position button2 below button
                button3.Location = new Point(button2.Location.X, button2.Location.Y + button2.Height + 5);
                panelSnippets.Controls.Add(button3);

                //}
            }

            return snippets;
        }

        private static void LaunchHtml(object? s)
        {
            var snip = (Snippet)((Button)s).Tag;

            var code = snip.Code;

            if (code.StartsWith("html\n"))
                code = code.Substring(5);

            var tempFile = $"{Path.GetTempPath()}{Guid.NewGuid().ToString()}.html";
            File.WriteAllText(tempFile, code);

            // find chrome path from registry
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
            var chromePath = key.GetValue(null).ToString();

            // start chrome
            Process.Start(chromePath, tempFile);
        }

        public async void LaunchCSharp(string code)
        {



            try
            {
                var scriptOptions = ScriptOptions.Default.AddReferences(typeof(Console).Assembly);
                // evaluate c# in .net core 8
                var result = await Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript.EvaluateAsync(code, scriptOptions);

                MessageBox.Show(result.ToString());

            }
            catch (CompilationErrorException e)
            {
                Console.WriteLine("Compilation error: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Execution error: " + e.Message);
            }


        }

        private async void btnGo_Click(object sender, EventArgs e)
        {
            btnGo.Enabled = false;

            // Current Model
            var model = (Model)cbEngine.SelectedItem;

            // get the name of the service for the model
            var serviceName = model.ServiceName;

            // instantiate the service from the appropriate api
            var aiService = (IAiService)Activator.CreateInstance(Type.GetType($"AiTool3.Providers.{serviceName}"));

            Conversation conversation = null;

            if (CurrentConversation.Messages.Count == 0)
            {
                // create a conversation from the system prompt and user input
                conversation = new Conversation(rtbSystemPrompt.Text, rtbInput.Text);
            }
            else
            {
                conversation = new Conversation();//tbSystemPrompt.Text, tbInput.Text
                conversation.systemprompt = rtbSystemPrompt.Text;
                conversation.messages = new List<ConversationMessage>();
                List<CompletionMessage> nodes = GetParentNodeList();

                Debug.WriteLine(nodes);

                foreach (var node in nodes)
                {
                    conversation.messages.Add(new ConversationMessage { role = node.Role == CompletionRole.User ? "user" : "assistant", content = node.Content });
                }
                conversation.messages.Add(new ConversationMessage { role = "user", content = rtbInput.Text });
            }
            // fetch the response from the api
            var response = await aiService.FetchResponse(model, conversation, null);

            // create a completion message for the user input
            var completionInput = new CompletionMessage
            {
                Role = CompletionRole.User,
                Content = rtbInput.Text,
                Parent = PreviousCompletion?.Guid,
                Engine = model.ModelName,
                Guid = System.Guid.NewGuid().ToString(),
                Children = new List<string>()
            };

            if (response == null)
            {
                MessageBox.Show("Response is null");
                btnGo.Enabled = true;
                return;
            }
            if (PreviousCompletion != null)
            {
                PreviousCompletion.Children.Add(completionInput.Guid);
            }

            CurrentConversation.Messages.Add(completionInput);

            // Create a new completion object to store the response in
            var completionResponse = new CompletionMessage
            {
                Role = CompletionRole.Assistant,
                Content = response.ResponseText,
                Parent = completionInput.Guid,
                Engine = model.ModelName,
                Guid = System.Guid.NewGuid().ToString(),
                Children = new List<string>()
            };

            // add it to the current conversation
            CurrentConversation.Messages.Add(completionResponse);

            // and display the results in the output box
            FindSnippets(rtbOutput, RtbFunctions.GetFormattedContent(string.Join("\r\n", response.ResponseText)));

            completionInput.Children.Add(completionResponse.Guid);

            PreviousCompletion = completionResponse;

            // draw the network diagram
            DrawNetworkDiagram();

            var currentResponseNode = ndcConversation.GetNodeForGuid(completionResponse.Guid);
            ndcConversation.CenterOnNode(currentResponseNode);
            var summaryModel = Settings.ApiList.First(x => x.ApiName.StartsWith("Ollama")).Models.First();

            string title;
            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == CurrentConversation.ConvGuid);

            // using the title, update the dgvConversations

            if (row != null)
            {
                if (string.IsNullOrWhiteSpace(row.Cells[3].Value.ToString()))
                {
                    title = await CurrentConversation.GenerateSummary(summaryModel);

                    CurrentConversation.SaveAsJson();
                }
                else title = row.Cells[3].Value.ToString();

                row.Cells[3].Value = title;
                CurrentConversation.SaveAsJson();
            }
            else
            {
                title = await CurrentConversation.GenerateSummary(summaryModel);

                CurrentConversation.SaveAsJson();

                dgvConversations.Rows.Insert(0, CurrentConversation.ConvGuid, CurrentConversation.Messages[0].Content, CurrentConversation.Messages[0].Engine, title);
            }

            btnGo.Enabled = true;

        }

        private List<CompletionMessage> GetParentNodeList()
        {
            // starting at PreviousCompletion, walk up the tree to the root node and return a list of nodes
            var nodes = new List<CompletionMessage>();
            var current = PreviousCompletion?.Guid;

            while (current != null)
            {
                var node = CurrentConversation.FindByGuid(current);
                nodes.Add(node);
                current = node.Parent;
            }

            nodes.Reverse();

            return nodes;
        }


        private void DrawNetworkDiagram()
        {
            // Clear the diagram
            ndcConversation.Clear();


            // find the root node
            var root = CurrentConversation.Messages.FirstOrDefault(c => c.Parent == null);
            if (root == null)
            {
                return;
            }
            var y = 0;

            var rootNode = new Node(root.Content, new Point(100, y), root.Guid);
            rootNode.BackColor = root.GetColorForEngine();
            ndcConversation.AddNode(rootNode);

            // recursively draw the children
            DrawChildren(root, rootNode, 100, ref y);

        }

        private void DrawChildren(CompletionMessage root, Node rootNode, int v, ref int y)
        {
            y += 100;
            foreach (var child in root.Children)
            {
                // get from child string
                var childMsg = CurrentConversation.Messages.FirstOrDefault(c => c.Guid == child);

                var childNode = new Node(childMsg.Content, new Point(v, y), childMsg.Guid);
                childNode.BackColor = childMsg.GetColorForEngine();
                ndcConversation.AddNode(childNode);
                ndcConversation.AddConnection(rootNode, childNode);
                DrawChildren(childMsg, childNode, v + 100, ref y);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rtbInput.Text = "";
            rtbSystemPrompt.Text = "";
            rtbOutput.Text = RtbFunctions.Clear();
            CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            PreviousCompletion = null;
            panelSnippets.Controls.Clear();
            var template = GetCurrentTemplate();
            if (template != null)
            {
                rtbSystemPrompt.Text = template.SystemPrompt;
                rtbInput.Text = template.InitialPrompt;
            }
            DrawNetworkDiagram();


        }

        private void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // work out the guid of the clicked row (first col)

            var guid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();
            // load that conversation from the json file
            CurrentConversation = JsonConvert.DeserializeObject<BranchedConversation>(File.ReadAllText($"v3-conversation-{guid}.json"));

            // draw the network diagram
            DrawNetworkDiagram();

            var currentResponseNode = ndcConversation.GetNodeForGuid(CurrentConversation.Messages.Last().Guid);
            //ndcConversation.CenterOnNode(currentResponseNode);
            ndcConversation.FitAll();
        }

        private void cbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            // get the selected item
            var selected = cbCategories.SelectedItem.ToString();

            // get all topics that match the selected item
            var topics = TopicSet.Topics.Where(t => t.Name == selected).ToList();

            // get all the templates for those topics
            var templates = topics.SelectMany(t => t.Templates).Where(x => x.SystemPrompt != null).ToList();

            // and add them to cbTemplates using LINQ
            cbTemplates.Items.Clear();
            cbTemplates.Items.AddRange(templates.Select(t => t.TemplateName).ToArray());

            // drop the cbtemplates down
            cbTemplates.DroppedDown = true;

        }

        private void cbTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            // get the selected item
            var selected = cbTemplates.SelectedItem.ToString();

            // get the category
            var category = cbCategories.SelectedItem.ToString();

            // find the template
            var template = TopicSet.Topics.First(t => t.Name == category).Templates.First(t => t.TemplateName == selected);

            btnClear_Click(null, null);

            rtbInput.Text = template.InitialPrompt;
            rtbSystemPrompt.Text = template.SystemPrompt;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ConversationTemplate template;
            if (cbCategories.SelectedItem == null || cbTemplates.SelectedItem == null)
            {
                // create a new template
                //template = new ConversationTemplate("System Prompt", "Initial Prompt");
                return;
            }
            template = GetCurrentTemplate();

            EditAndSaveTemplate(template);
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
            var form = new Form();
            form.Text = "Edit Template";
            form.Size = new Size(800, 600);
            form.Padding = new Padding(10);

            var tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.RowCount = 4;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));

            // Template Name
            AddLabelAndTextBox(tableLayoutPanel, "Template Name:", template.TemplateName, 0);

            // System Prompt
            AddLabelAndTextBox(tableLayoutPanel, "System Prompt:", template.SystemPrompt, 1);

            // User Prompt
            AddLabelAndTextBox(tableLayoutPanel, "User Prompt:", template.InitialPrompt, 2);

            // Buttons
            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Dock = DockStyle.Fill;

            var btnCancel = new Button { Text = "Cancel" };
            btnCancel.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };
            btnCancel.AutoSize = true;
            buttonPanel.Controls.Add(btnCancel);

            var btnOk = new Button { Text = "OK" };
            btnOk.Click += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };
            btnOk.AutoSize = true;

            buttonPanel.Controls.Add(btnOk);

            tableLayoutPanel.Controls.Add(buttonPanel, 1, 3);

            form.Controls.Add(tableLayoutPanel);
            form.ShowDialog();

            // if ok returned, update the template with the new values...
            if (form.DialogResult == DialogResult.OK)
            {
                template.TemplateName = tableLayoutPanel.Controls[1].Text;
                template.SystemPrompt = tableLayoutPanel.Controls[3].Text;
                template.InitialPrompt = tableLayoutPanel.Controls[5].Text;

                if (add)
                {
                    var categoryTopic = TopicSet.Topics.FirstOrDefault(t => t.Name == category);

                    if (categoryTopic == null)
                    {
                        categoryTopic = new Topic(Guid.NewGuid().ToString(), category);
                        TopicSet.Topics.Add(categoryTopic);
                    }

                    categoryTopic.Templates.Add(template);
                }

                TopicSet.Save();
            }
        }

        private void AddLabelAndTextBox(TableLayoutPanel panel, string labelText, string textBoxContent, int row)
        {
            var label = new Label
            {
                Text = labelText,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(label, 0, row);

            var textBox = new RichTextBox
            {
                Text = textBoxContent,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 0, 5)
            };
            panel.Controls.Add(textBox, 1, row);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ConversationTemplate template;
            if (string.IsNullOrWhiteSpace(cbCategories.Text))
            {
                // create a new template
                //template = new ConversationTemplate("System Prompt", "Initial Prompt");
                return;
            }
            var category = cbCategories.Text;

            template = new ConversationTemplate("System Prompt", "Initial Prompt");

            EditAndSaveTemplate(template, true, category);


        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            rtbOutput.Text = RtbFunctions.Clear();
            CurrentConversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            PreviousCompletion = null;
            panelSnippets.Controls.Clear();
            DrawNetworkDiagram();
        }

        private AudioRecorder recorder;
        private CancellationTokenSource cts;
        private Task recordingTask;
        private bool isRecording = false;


        private async void button3_Click(object sender, EventArgs e)
        {
            if (!isRecording)
            {
                // Start recording
                StartRecording();
            }
            else
            {
                // Stop recording
                await StopRecording();
            }
        }

        private void StartRecording()
        {
            recorder = new AudioRecorder();
            cts = new CancellationTokenSource();

            // Start recording in a separate task
            recordingTask = recorder.RecordAudioAsync("output.wav", cts.Token);

            isRecording = true;
            button3.Text = "Stop Recording"; // Optional: Update button text
        }

        private async Task StopRecording()
        {
            if (cts != null)
            {
                // Stop the recording
                cts.Cancel();

                // Wait for the recording task to complete
                await recordingTask;

                isRecording = false;
                button3.Text = "Start Recording"; // Optional: Update button text

                // Clean up
                cts.Dispose();
                cts = null;
                recorder = null;

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                process.StartInfo = startInfo;
                process.Start();

                process.StandardInput.WriteLine("call C:\\ProgramData\\Miniconda3\\condabin\\activate.bat");
                process.StandardInput.WriteLine("conda activate whisperx");
                //process.StandardInput.WriteLine("conda --version");
                //process.StandardInput.WriteLine("python --version");
                //process.StandardInput.WriteLine("pip list");
                process.StandardInput.WriteLine("whisperx output.wav");
                process.StandardInput.WriteLine("exit");

                // wait for completion
                process.WaitForExit();

                // get the output from output.txt
                var output = File.ReadAllText("output.txt");
                rtbInput.Text += output;
            }
        }

    }
}
