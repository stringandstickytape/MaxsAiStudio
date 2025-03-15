using AiStudio4.Core.Models;

namespace AiStudio4.Core.Interfaces
{
    public interface ISimpleChatService
    {
        Task<SimpleChatResponse> ProcessSimpleChatAsync(SimpleChatRequest request);
    }
}