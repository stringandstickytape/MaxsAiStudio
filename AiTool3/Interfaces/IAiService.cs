using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Tools;
using SharedClasses.Providers;

namespace AiTool3.Interfaces
{
    public interface IAiService
    {
        Task<AiResponse> FetchResponse(ServiceProvider serviceProvider,
            Model model, LinearConversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, ApiSettings apiSettings, bool mustNotUseEmbedding, List<string> toolNames, bool useStreaming = false, bool addEmbeddings = false);

        ToolManager ToolManager { get; set; }
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
    }
}