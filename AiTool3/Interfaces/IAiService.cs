using AiTool3.ApiManagement;
using AiTool3.Conversations;

namespace AiTool3.Interfaces
{
    public interface IAiService
    {
        Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, bool useStreaming = false);

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;
    }
}