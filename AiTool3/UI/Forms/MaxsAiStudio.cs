using AiTool3.Audio;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.AiServices;
using AiTool3.Snippets;
using AiTool3.Templates;
using AiTool3.Tools;
using AiTool3.Topics;
using AiTool3.UI;
using AiTool3.UI.Forms;
using AiTool3;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using SharedClasses;
using SharedClasses.Helpers;
using System;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Whisper.net.Ggml;
using AiTool3.ExtensionMethods;

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
        private readonly ChatWebViewEventHandler _chatWebViewEventHandler;

        public static readonly decimal Version = 0.3m;

        public static readonly string ThreeTicks = new string('`', 3);

        public event Action<SettingsSet> SettingsChanged;

        private SettingsSet currentSettings;
        public ConversationManager ConversationManager;
        public SettingsSet CurrentSettings
        {
            get => currentSettings;
            set
            {
                currentSettings = value;
                SettingsChanged?.Invoke(currentSettings);
            }
        }


        private CancellationTokenSource? _cts, _cts2;
        private WebViewManager? _webViewManager = null;
        
        private System.Windows.Forms.Timer updateTimer = new System.Windows.Forms.Timer();
        private AudioRecorderManager _audioRecorderManager;

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
                _webViewManager = new WebViewManager(ndcWeb);
                _aiResponseHandler.InjectDependencies(chatWebView, _webViewManager);



                splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

                splitContainer1.Panel1Collapsed = CurrentSettings.CollapseConversationPane;

                _audioRecorderManager = new AudioRecorderManager(GgmlType.SmallEn, chatWebView);
                _audioRecorderManager.AudioProcessed += AudioRecorderManager_AudioProcessed;

                splitContainer1.Paint += new PaintEventHandler(SplitContainerHelper.SplitContainer_Paint!);
                splitContainer5.Paint += new PaintEventHandler(SplitContainerHelper.SplitContainer_Paint!);

                dgvConversations.InitialiseDataGridView(RegenerateSummary, DeleteConversation);

                InitialiseMenus();

                Load += OnLoad!;

                dgvConversations.MouseDown += DgvConversations_MouseDown;

                _chatWebViewEventHandler = new ChatWebViewEventHandler(
                    chatWebView,
                    ConversationManager,
                    _fileAttachmentManager,
                    _templateManager,
                    _aiResponseHandler,
                    _webViewManager,
                    dgvConversations,
                    CurrentSettings,
                    tokenUsageLabel,
                    _audioRecorderManager,
                    menuBar,
                    _scratchpadManager,
                    this);

                SettingsChanged += _chatWebViewEventHandler.UpdateSettings;

            }
            finally
            {
                splashManager.CloseSplash();
            }
        }



     

        private async void DeleteConversation(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete this conversation?", "Delete Conversation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // are we deleting the current conversation? if so, start a new one.
                if (selectedConversationGuid == ConversationManager.Conversation.ConvGuid)
                {
                    await _chatWebViewEventHandler.BeginNewConversation();
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

            MenuHelper.CreateMenuItem("Settings", ref editMenu).Click += async (s, e) => CurrentSettings = await SettingsSet.OpenSettingsForm(chatWebView, CurrentSettings);

            MenuHelper.CreateMenuItem("Test SP", ref editMenu).Click += async (s, e) => new ServiceProviderForm(new List<ServiceProvider> { new ServiceProvider { FriendlyName = "1", Url = "2"} }).ShowDialog();

            MenuHelper.CreateMenuItem("Licenses", ref editMenu).Click += (s, e) => new LicensesForm(AssemblyHelper.GetEmbeddedResource("AiTool3.UI.Licenses.txt")).ShowDialog();

            await MenuHelper.CreateSpecialsMenu(menuBar, CurrentSettings, chatWebView, _snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager, this);
            await MenuHelper.CreateEmbeddingsMenu(this, menuBar, CurrentSettings, chatWebView, _snippetManager, dgvConversations, ConversationManager, AutoSuggestStringSelected, _fileAttachmentManager);

            MenuHelper.CreateTemplatesMenu(menuBar, chatWebView, _templateManager, CurrentSettings, this);

            await VersionHelper.CheckForUpdate(menuBar);
        }


        private async void OnLoad(object sender, EventArgs e)
        {
            Load -= OnLoad!;

            await chatWebView.EnsureCoreWebView2Async(null);

            await _chatWebViewEventHandler.BeginNewConversation();

            await _webViewManager.CreateNewWebNdc(false, WebViewNdc_WebNdcContextMenuOptionSelected, _chatWebViewEventHandler.WebViewNdc_WebNdcNodeClicked);

            this.BringToFront();

            // Create things in Ready instead...

        }

        private void DgvConversations_MouseDown(object? sender, MouseEventArgs e)
        {
            dgvConversations.SetConversationForDgvClick(ref selectedConversationGuid, e);
        }



        private async void RegenerateSummary(object sender, EventArgs e) =>
            await ConversationManager.RegenerateSummary(dgvConversations, selectedConversationGuid, CurrentSettings);



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
                            _chatWebViewEventHandler.WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(messageGuid));
                        }
                    }
                }
            }
            else
            {
                WebNdcRightClickLogic.ProcessWebNdcContextMenuOption(ConversationManager.GetParentNodeList(), e.MenuOption);
            }
        }

        private async void AudioRecorderManager_AudioProcessed(object? sender, string e) => await chatWebView.ConcatenateUserPrompt(e);


        private async Task<bool> WebNdcDrawNetworkDiagram() => await _webViewManager.DrawNetworkDiagram(ConversationManager.Conversation.Messages);


        private async void dgvConversations_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                var clickedGuid = dgvConversations.Rows[e.RowIndex].Cells[0].Value.ToString();

                ConversationManager.LoadConversation(clickedGuid!);

                if (ConversationManager.Conversation.GetRootNode() != null)
                {
                    _chatWebViewEventHandler.WebViewNdc_WebNdcNodeClicked(null, new WebNdcNodeClickedEventArgs(ConversationManager.Conversation.GetRootNode()?.Guid));
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
            _chatWebViewEventHandler.EnableConversationsAndWebView();

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

        private void MaxsAiStudio_FormClosing(object sender, FormClosingEventArgs e) => _webViewManager!.webView.Dispose();

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
    }
}