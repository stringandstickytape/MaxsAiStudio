using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Tools;

namespace AiTool3.Interfaces
{
    public interface IAiService
    {
        Task<AiResponse> FetchResponse(ServiceProvider serviceProvider,
            Model model, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolNames, bool useStreaming = false, bool addEmbeddings = false);

        ToolManager ToolManager { get; set; }
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
    }
}