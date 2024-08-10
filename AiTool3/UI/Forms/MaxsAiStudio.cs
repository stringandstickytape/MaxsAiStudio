using AiTool3.Audio;
using AiTool3.Communications;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.Providers;
using AiTool3.Settings;
using AiTool3.Snippets;
using AiTool3.Templates;
using AiTool3.Tools;
using AiTool3.Topics;
using AiTool3.UI;
using AiTool3.UI.Forms;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using Whisper.net.Ggml;
using static AiTool3.Communications.NamedPipeListener;

namespace AiTool3
{
    public partial class MaxsAiStudio : Form
    {
        // injected dependencies
        private ToolManager _toolManager;
        private SnippetManager _snippetManager;
        private NamedPipeListener _namedPipeListener;
        private SearchManager _searchManager;
        private FileAttachmentManager _fileAttachmentManager;
        private TemplateManager _templateManager;
        private ScratchpadManager _scratchpadManager;
        private AiResponseHandler _aiResponseHandler;

        public static readonly decimal Version = 0.3m;

        public static readonly string ThreeTicks = new string('`', 3);

        public ConversationManager ConversationManager;
        public SettingsSet CurrentSettings { get; set; }

        private CancellationTokenSource? _cts, _cts2;
        private WebViewManager? webViewManager = null;
        private System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private AudioRecorderManager audioRecorderManager;

        public string selectedConversationGuid = "";

        public MaxsAiStudio(ToolManager toolManager,
                            SnippetManager snippetManager,
                            NamedPipeListener namedPipeListener,
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

                if (!File.Exists("Settings\\settings.json"))
                {
                    CurrentSettings = AiTool3.SettingsSet.Load()!;
                    // show the settings dialog first up
                    var settingsForm = new SettingsForm(CurrentSettings);
                    var result = settingsForm.ShowDialog();
                    CurrentSettings = settingsForm.NewSettings;
                    SettingsSet.Save(CurrentSettings);
                }
                else CurrentSettings = AiTool3.SettingsSet.Load()!;

                InitializeComponent();

                _toolManager = toolManager;
                _snippetManager = snippetManager;
                _searchManager = searchManager;
                _searchManager.SetDgv(dgvConversations);
                _fileAttachmentManager = fileAttachmentManager;
                _fileAttachmentManager.InjectDependencies(chatWebView);
                _namedPipeListener = namedPipeListener;
                _namedPipeListener.NamedPipeMessageReceived += NamedPipeListener_NamedPipeMessageReceived;
                ConversationManager = conversationManager;
                ConversationManager.InjectDepencencies(dgvConversations);
                chatWebView.InjectDependencies(toolManager);
                _scratchpadManager = scratchpadManager;
                _aiResponseHandler = aiResponseHandler;
                webViewManager = new WebViewManager(ndcWeb);
                _aiResponseHandler.InjectDependencies(chatWebView, webViewManager);

                splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

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

                dgvConversations.InitialiseDataGridView(RegenerateSummary, DeleteConversation, selectedConversationGuid);

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



        private async void NamedPipeListener_NamedPipeMessageReceived(object? sender, string e)
        {
            VSCodeSelection selection = JsonConvert.DeserializeObject<VSCodeSelection>(e);

            // create a new one-off summary-model conversation with the selected text as the user prompt
            var summaryModel = CurrentSettings.GetSummaryModel();
            var tempConversationManager = new ConversationManager();
            tempConversationManager.Conversation = new BranchedConversation { ConvGuid = Guid.NewGuid().ToString() };
            tempConversationManager.Conversation.AddNewRoot();

            var content = $"{ThreeTicks}\n{selection.Before}<CURSOR LOCATION>{selection.After}\n{ThreeTicks}\n\n The user's instruction is: \n{ThreeTicks}\n{selection.Selected}\n{ThreeTicks}\n\n";

            // or alternatively:

            var conversation = new Conversation
            {
                systemprompt = "You are a code completion AI. You return a single code block which will be inserted in the user's current cursor location. The code block must be in the correct language and satisfy the user's request, based on the context before and after the user's current cursor location.",
                messages = new List<ConversationMessage>
                {
                    new ConversationMessage { role = "user", content = content }
                }
            };

            var aiService = AiServiceResolver.GetAiService(summaryModel.ServiceName, _toolManager);
            var response = await aiService.FetchResponse(summaryModel, conversation, null, null, CancellationToken.None, CurrentSettings, mustNotUseEmbedding: true, toolNames: null, useStreaming: false);

            var txt = SnippetHelper.StripFirstAndLastLine(response.ResponseText);

            await _namedPipeListener.SendResponseAsync(txt);

        }

        private async void ChatWebView_ChatWebViewReadyEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await InitialiseApiList_New();

            // send color schemes to the chatwebview
            var themesPath = Path.Combine("Settings\\Themes.json");
            if (File.Exists(themesPath))
            {
                await chatWebView.SetThemes(File.ReadAllText(themesPath));
                await chatWebView.SetTheme(CurrentSettings.SelectedTheme);
            }
            else
            {
                var themesJson = AssemblyHelper.GetEmbeddedAssembly("AiTool3.Defaults.themes.json");
                await chatWebView.SetThemes(themesJson);
                File.WriteAllText(themesPath, themesJson);
                CurrentSettings.SelectedTheme = "Serene";
                SettingsSet.Save(CurrentSettings);
            }

            // if there isn't a scratchpad file but there is a scratchpad bak file, rename the bak file to scratchpad.json
            if (File.Exists(Path.Combine("Settings", "Scratchpad.json.bak")) && !File.Exists(Path.Combine("Settings", "Scratchpad.json")))
            {
                File.Move(Path.Combine("Settings", "Scratchpad.json.bak"), Path.Combine("Settings", "Scratchpad.json"));
            }

            var scratchpadContent = _scratchpadManager.LoadScratchpad();
            if (!string.IsNullOrEmpty(scratchpadContent))
            {
                await chatWebView.ExecuteScriptAsync($"window.setScratchpadContentAndOpen({scratchpadContent})");
            }

            await chatWebView.SetTools();

        }

        private async void ChatWebView_ChatWebViewContinueEvent(object? sender, ChatWebViewSimpleEventArgs e)
        {
            await _aiResponseHandler.FetchAiInputResponse(CurrentSettings, null, "Continue from PRECISELY THE CHARACTER where you left off.  Do not restart or repeat anything.  Demarcate your output with three backticks.",
                updateUiMethod: (response) =>
                {
                    UpdateUi(response);
                });

            ConversationManager.ContinueUnterminatedCodeBlock(e);

            await WebNdcDrawNetworkDiagram();

            WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(e.Guid));
        }


        private void ImportTemplate(string jsonContent)
        {
            try
            {
                var importTemplate = JsonConvert.DeserializeObject<TemplateImport>(jsonContent);

                var template = new ConversationTemplate(importTemplate.systemPrompt, importTemplate.initialUserPrompt);

                if (template != null)
                {
                    var categoryForm = new Form();
                    categoryForm.Text = "Select Category";
                    categoryForm.Size = new Size(300, 150);
                    categoryForm.StartPosition = FormStartPosition.CenterScreen;

                    var comboBox = new ComboBox();
                    comboBox.Dock = DockStyle.Top;
                    comboBox.Items.AddRange(_templateManager.TemplateSet.Categories.Select(c => c.Name).ToArray());

                    var okButton = new Button();
                    okButton.Text = "OK";
                    okButton.DialogResult = DialogResult.OK;
                    okButton.Dock = DockStyle.Bottom;

                    categoryForm.Controls.Add(comboBox);
                    categoryForm.Controls.Add(okButton);

                    if (categoryForm.ShowDialog() == DialogResult.OK)
                    {
                        var selectedCategory = comboBox.SelectedItem?.ToString();
                        if (!string.IsNullOrEmpty(selectedCategory))
                        {
                            _templateManager.EditAndSaveTemplate(template, true, selectedCategory);
                            MenuHelper.RemoveOldTemplateMenus(menuBar);
                            MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, _templateManager, CurrentSettings, this);
                            MessageBox.Show("Template imported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
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
                case "importTemplate":
                    ImportTemplate(e.Json);
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
                case "ApplyFaRArray":
                    var fnrs = JsonConvert.DeserializeObject<FindAndReplaceSet>(e.Json);

                    var grouped = fnrs.replacements.GroupBy(r => r.filename);

                    foreach (var group in grouped)
                    {
                        var originalContent = File.ReadAllText(group.Key);
                        var processed = FileProcessor.ApplyFindAndReplace(originalContent, group.ToList(), out string errorString);
                        if (processed == null)
                        {
                            await chatWebView.SetUserPrompt(await chatWebView.GetUserPrompt() + $"\nError processing file {group.Key}: {errorString}");
                            break;
                        }
                    }

                    // for each group
                    foreach (var group in grouped)
                    {
                        var originalContent = File.ReadAllText(group.Key);
                        var processed = FileProcessor.ApplyFindAndReplace(originalContent, group.ToList(), out string errorString);
                        if (processed != null)
                        {
                            File.WriteAllText(group.Key, processed);
                        }
                        else
                        {
                            await chatWebView.SetUserPrompt(await chatWebView.GetUserPrompt() + $"\nError processing file {group.Key}: {errorString}");
                        }
                    }
                    MessageBox.Show($"Done.");
                    //var processed = FileProcessor.ApplyFindAndReplace(originalContent, fnrs.replacements.ToList());
                    break;

                case "attach":



                    await _fileAttachmentManager.HandleAttachment(chatWebView, this, CurrentSettings.SoftwareToyMode);

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

                    await Task.Run(async () =>
                    {
                        try
                        {
                            form = new FileSearchForm(CurrentSettings.DefaultPath, CurrentSettings.ProjectHelperFileExtensions);
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
                    this.HideWorking();
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
                    await InitialiseApiList_New();
                }
            };

            MenuHelper.CreateMenuItem("Licenses", ref editMenu).Click += (s, e) => new LicensesForm(AssemblyHelper.GetEmbeddedAssembly("AiTool3.UI.Licenses.txt")).ShowDialog();

            await MenuHelper.CreateSpecialsMenu(menuBar, CurrentSettings, chatWebView, _snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager, this);
            await MenuHelper.CreateEmbeddingsMenu(this, menuBar, CurrentSettings, chatWebView, _snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager);

            MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, _templateManager, CurrentSettings, this);

            // check for updates
            try
            {
                var latestVersionDetails = await VersionHelper.GetLatestRelease();
                if (latestVersionDetails.Item1 != "")
                {
                    var latestVersion = latestVersionDetails.Item2;
                    var latestVersionUrl = latestVersionDetails.Item1.ToString();

                    var currentVersion = MaxsAiStudio.Version;

                    ToolStripMenuItem updateMenu = null;
                    if (latestVersion > currentVersion)
                    {
                        updateMenu = MenuHelper.CreateMenu("Update Available");
                        updateMenu.BackColor = System.Drawing.Color.DarkRed;
                    }
                    else if (latestVersion < currentVersion)
                    {
                        updateMenu = MenuHelper.CreateMenu($"Pre-Release Version {currentVersion}");
                        updateMenu.BackColor = System.Drawing.Color.DarkSalmon;
                    }
                    else if (latestVersion == currentVersion)
                    {
                        updateMenu = MenuHelper.CreateMenu($"Version {currentVersion}");
                        updateMenu.BackColor = System.Drawing.Color.DarkGreen;
                    }

                    updateMenu.Click += (s, e) =>
                    {
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {latestVersionUrl.Replace("&", "^&")}") { CreateNoWindow = true });
                    };
                    menuBar.Items.Add(updateMenu);
                }
            }
            catch { }
        }
        private async void ChatWebView_FileDropped(object sender, string filename)
        {
            if (filename.StartsWith("http"))
            {
                var textFromUrl = await HtmlTextExtractor.ExtractTextFromUrlAsync(filename);

                var quotedFile = HtmlTextExtractor.QuoteFile(filename, textFromUrl);

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
                        var output = await _fileAttachmentManager.TranscribeMP4(filename, CurrentSettings.PathToCondaActivateScript);
                        chatWebView.SetUserPrompt(output);
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


        private async void OnLoad(object sender, EventArgs e)
        {
            Load -= OnLoad!;

            await chatWebView.EnsureCoreWebView2Async(null);

            await BeginNewConversation();

            await CreateNewWebNdc(false);

            this.BringToFront();

            // Create things in Ready instead...

        }

        private async Task InitialiseApiList_New()
        {


            await chatWebView.SetModels(CurrentSettings.ModelList);

            if (CurrentSettings.SelectedModel != "")
            {
                var matchingModel = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName == CurrentSettings.SelectedModel.Split(' ')[0]);
                await chatWebView.SetDropdownValue("mainAI", matchingModel.ToString());
            }
            else
            {
                var selectedModel = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName.Contains("llama3"));
                await chatWebView.SetDropdownValue("mainAI", selectedModel.ToString());
                CurrentSettings.SelectedModel = selectedModel.ToString();
                SettingsSet.Save(CurrentSettings);
            }

            if (CurrentSettings.SelectedSummaryModel != "")
            {
                var matchingModel = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName == CurrentSettings.SelectedSummaryModel.Split(' ')[0]);
                await chatWebView.SetDropdownValue("summaryAI", matchingModel.ToString());
            }
            else
            {
                var selectedModel = CurrentSettings.ModelList.FirstOrDefault(m => m.ModelName.Contains("llama3"));
                await chatWebView.SetDropdownValue("summaryAI", selectedModel.ToString());
                CurrentSettings.SelectedSummaryModel = selectedModel.ToString();
                SettingsSet.Save(CurrentSettings);
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
                    try
                    {
                        selectedConversationGuid = dgvConversations.Rows[hti.RowIndex].Cells[0].Value.ToString();
                    }
                    catch { }
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
            await _aiResponseHandler.FetchAiInputResponse(CurrentSettings, e.SelectedTools, sendSecondary: e.SendViaSecondaryAI, addEmbeddings: e.AddEmbeddings,
                updateUiMethod: (response) =>
                {
                    UpdateUi(response);
                });
        }






        private async Task UpdateUi(AiResponse response)
        {
            var model = await chatWebView.GetDropdownModel("mainAI", CurrentSettings);
            var cost = model.GetCost(response.TokenUsage);

            tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out";

            var row = dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == ConversationManager.Conversation.ConvGuid);

            if (row == null)
            {
                dgvConversations.Rows.Insert(0, ConversationManager.Conversation.ConvGuid, ConversationManager.Conversation.Messages[0].Content, ConversationManager.Conversation.Messages[0].Engine, "");
            }
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


        private async Task NewKeepContext()
        {
            CompletionMessage? lastAssistantMessage, lastUserMessage;
            ConversationManager.GetConversationContext(out lastAssistantMessage, out lastUserMessage);

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

            ConversationManager.AddMessagePair(userMessage, assistantMessage);

            await chatWebView.AddMessage(userMessage);
            await chatWebView.AddMessage(assistantMessage);

            await WebNdcDrawNetworkDiagram();
        }



        private async Task BeginNewConversation()
        {
            await chatWebView.Clear(CurrentSettings);
            webViewManager.Enable();

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

            var models = CurrentSettings.ModelList;
            var matchingModel = models.FirstOrDefault(m => $"{e.ModelString.Split(' ')[0]}" == m.ModelName);
            if (e.Dropdown == "mainAI")
            {


                CurrentSettings.SelectedModel = matchingModel.ModelName;
                SettingsSet.Save(CurrentSettings);
            }
            else if (e.Dropdown == "summaryAI")
            {
                CurrentSettings.SelectedSummaryModel = matchingModel.ModelName;
                SettingsSet.Save(CurrentSettings);
            }
        }

        private void chatWebView_DragDrop(object sender, DragEventArgs e)
        {
            // get the name of the dropped file

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        }
    }
}