using AiStudio4.Core.Models;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IWebSocketNotificationService
    {
        /// <summary>
        /// Notifies a client about a conversation update
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="update">The update to send</param>
        Task NotifyConversationUpdate(string clientId, ConversationUpdateDto update);

        /// <summary>
        /// Notifies a client about a streaming update
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="update">The update to send</param>
        Task NotifyStreamingUpdate(string clientId, StreamingUpdateDto update);

        /// <summary>
        /// Notifies a client about a conversation list
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="conversations">The conversations to send</param>
        Task NotifyConversationList(string clientId, ConversationListDto conversations);
    }
}