

namespace AiStudio4.Core.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponse> ProcessChatRequest(ChatRequest request, string assistantMessageId, CancellationToken cancellationToken);
        Task<SimpleChatResponse> ProcessSimpleChatRequest(string request);
        // Events removed, replaced by callbacks in ChatRequest
        // event EventHandler<string> StreamingTextReceived;
        // event EventHandler<string> StreamingComplete;
    }
}
