using AiStudio4.Core.Models;

namespace AiStudio4.Core.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponse> ProcessChatRequest(ChatRequest request);
        event EventHandler<string> StreamingTextReceived;
        event EventHandler<string> StreamingComplete;
    }
}