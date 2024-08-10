using AiTool3.FileAttachments;
using AiTool3.Tools;
using AiTool3.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.Conversations
{
    public class AIResponseHandler
    {
        private readonly ConversationManager _conversationManager;
        private readonly ChatWebView _chatWebView;
        private readonly SettingsSet _currentSettings;
        private readonly ToolManager _toolManager;
        private readonly FileAttachmentManager _fileAttachmentManager;
        private readonly WebViewManager _webViewManager;

        public AIResponseHandler(ConversationManager conversationManager, ChatWebView chatWebView, SettingsSet currentSettings, ToolManager toolManager, FileAttachmentManager fileAttachmentManager, WebViewManager webViewManager)
        {
            _conversationManager = conversationManager;
            _chatWebView = chatWebView;
            _currentSettings = currentSettings;
            _toolManager = toolManager;
            _fileAttachmentManager = fileAttachmentManager;
            _webViewManager = webViewManager;
        }
    }
}
