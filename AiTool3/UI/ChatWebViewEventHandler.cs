using AiTool3.Audio;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.ExtensionMethods;
using AiTool3.FileAttachments;
using AiTool3.Helpers;
using AiTool3.Providers;
using AiTool3.Templates;
using AiTool3.Tools;
using AiTool3.Topics;
using AiTool3.UI.Forms;
using Newtonsoft.Json;
using SharedClasses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace AiTool3.UI
{
    public class ChatWebViewEventHandler
    {
        private readonly ChatWebView _chatWebView;
        private readonly ConversationManager _conversationManager;
        private readonly FileAttachmentManager _fileAttachmentManager;
        private readonly TemplateManager _templateManager;
        private readonly AiResponseHandler _aiResponseHandler;
        private readonly WebViewManager _webViewManager;
        private readonly DataGridView _dgvConversations;
        private readonly SettingsSet _currentSettings;
        private readonly ToolStripStatusLabel _tokenUsageLabel;
        private readonly AudioRecorderManager _audioRecorderManager;
        private readonly MenuStrip _menuBar;
        private CancellationTokenSource? _cts;
        private readonly Stopwatch _stopwatch;
        private readonly System.Windows.Forms.Timer _updateTimer;
        private readonly ScratchpadManager _scratchpadManager;

        public ChatWebViewEventHandler(
            ChatWebView chatWebView,
            ConversationManager conversationManager,
            FileAttachmentManager fileAttachmentManager,
            TemplateManager templateManager,
            AiResponseHandler aiResponseHandler,
            WebViewManager webViewManager,
            DataGridView dgvConversations,
            SettingsSet currentSettings,
            ToolStripStatusLabel tokenUsageLabel,
            AudioRecorderManager audioRecorderManager,
            MenuStrip menuBar,
            ScratchpadManager scratchpadManager)
        {
            _chatWebView = chatWebView;
            _conversationManager = conversationManager;
            _fileAttachmentManager = fileAttachmentManager;
            _templateManager = templateManager;
            _aiResponseHandler = aiResponseHandler;
            _webViewManager = webViewManager;
            _dgvConversations = dgvConversations;
            _currentSettings = currentSettings;
            _tokenUsageLabel = tokenUsageLabel;
            _audioRecorderManager = audioRecorderManager;
            _menuBar = menuBar;
            _scratchpadManager = scratchpadManager;
            _stopwatch = new Stopwatch();
            _updateTimer = new System.Windows.Forms.Timer();

            RegisterEvents();
        }

        private void RegisterEvents()
        {
            var eventMappings = new Dictionary<string, Delegate>
            {
               {"SendMessage", new EventHandler<ChatWebViewSendMessageEventArgs>(ChatWebView_ChatWebViewSendMessageEvent)},
               //{"Cancel", new EventHandler<ChatWebViewCancelEventArgs>(ChatWebView_ChatWebViewCancelEvent)},
               //{"Copy", new EventHandler<ChatWebViewCopyEventArgs>(ChatWebView_ChatWebViewCopyEvent)},
               //{"New", new EventHandler<ChatWebViewNewEventArgs>(ChatWebView_ChatWebViewNewEvent)},
               //{"AddBranch", new EventHandler<ChatWebViewAddBranchEventArgs>(ChatWebView_ChatWebViewAddBranchEvent)},
               //{"JoinWithPrevious", new EventHandler<ChatWebViewJoinWithPreviousEventArgs>(ChatWebView_ChatWebViewJoinWithPreviousEvent)},
               //{"DropdownChanged", new EventHandler<ChatWebViewDropdownChangedEventArgs>(ChatWebView_ChatWebDropdownChangedEvent)},
               //{"Simple", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewSimpleEvent)},
               //{"Continue", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewContinueEvent)},
               //{"Ready", new EventHandler<ChatWebViewSimpleEventArgs>(ChatWebView_ChatWebViewReadyEvent)}
            };

            foreach (var mapping in eventMappings)
            {
                typeof(ChatWebView).GetEvent($"ChatWebView{mapping.Key}Event")
                    .AddEventHandler(_chatWebView, mapping.Value);
            }

            //_chatWebView.FileDropped += ChatWebView_FileDropped;
        }

        private async void ChatWebView_ChatWebViewSendMessageEvent(object? sender, ChatWebViewSendMessageEventArgs e)
        {

            _cts = MaxsAiStudio.ResetCancellationtoken(_cts);
            _stopwatch.Restart();
            _updateTimer.Start();

            try
            {
                await _chatWebView.DisableSendButton();
                await _chatWebView.EnableCancelButton();
            }
            catch (Exception ex)
            {
            }

            _dgvConversations.Enabled = false;
            _webViewManager.Disable();


            await _aiResponseHandler.FetchAiInputResponse(_currentSettings, _cts.Token, e.SelectedTools, overrideUserPrompt: e.OverrideUserPrompt, sendSecondary: e.SendViaSecondaryAI, addEmbeddings: e.AddEmbeddings, prefill: e.Prefill,

                updateUiMethod: async (response) =>
                {
                    _updateTimer.Stop();

                    await UpdateUi(response);

                });

            EnableConversationsAndWebView();

        }

        private void EnableConversationsAndWebView()
        {
            _dgvConversations.Enabled = true;
            _webViewManager.Enable();
        }

        private async Task UpdateUi(AiResponse response)
        {
            var cost = _currentSettings.GetModel().GetCost(response.TokenUsage);

            _tokenUsageLabel.Text = $"Token Usage: ${cost} : {response.TokenUsage.InputTokens} in --- {response.TokenUsage.OutputTokens} out ";

            // response.TokenUsage.OutputTokens * 1000 / elapsed.Value.TotalMilliseconds to 2dp is 
            _tokenUsageLabel.Text += $" at {(response.TokenUsage.OutputTokens * 1000 / response.Duration.TotalMilliseconds).ToString("F2")} tokens per second";

            if (response.TokenUsage.CacheCreationInputTokens > 0)
            {
                _tokenUsageLabel.Text += $" ; {response.TokenUsage.CacheCreationInputTokens} cache creation tokens";
            }
            if (response.TokenUsage.CacheReadInputTokens > 0)
            {
                _tokenUsageLabel.Text += $" ; {response.TokenUsage.CacheReadInputTokens} cache read tokens";
            }

            if (response.TokenUsage.CacheCreationInputTokens > 0 || response.TokenUsage.CacheReadInputTokens > 0)
            {
                var actualInputTokens = response.TokenUsage.InputTokens + response.TokenUsage.CacheCreationInputTokens + response.TokenUsage.CacheReadInputTokens;
                var convertedCachedInputTokens = response.TokenUsage.InputTokens + response.TokenUsage.CacheCreationInputTokens * 1.25m + response.TokenUsage.CacheReadInputTokens * 0.1m;

                // "Used 33% more tokens than without caching"
                var percentage = (int)((convertedCachedInputTokens / actualInputTokens) * 100) - 100;


                _tokenUsageLabel.Text += $" ; this request used {percentage}% {(percentage > 0 ? "more" : "less")} tokens because of caching";

            }

            var row = _dgvConversations.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.Cells[0]?.Value?.ToString() == _conversationManager.Conversation.ConvGuid);

            if (row == null)
            {
                _dgvConversations.Rows.Insert(0, _conversationManager.Conversation.ConvGuid, _conversationManager.Conversation.Messages[0].Content, _conversationManager.Conversation.Messages[0].Engine, "");
            }
        }
    }
}