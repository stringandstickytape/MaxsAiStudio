using AiStudio4.AiStudio4.Core.Models;
using AiStudio4.Core.Models;

namespace AiStudio4.Core.Interfaces
{
    public interface IWebSocketNotificationService
    {
        Task NotifyConversationUpdate(string clientId, ConversationUpdateDto update);
        Task NotifyStreamingUpdate(string clientId, StreamingUpdateDto update);
        Task NotifyConversationList(string clientId, ConversationListDto conversations);
    }
}