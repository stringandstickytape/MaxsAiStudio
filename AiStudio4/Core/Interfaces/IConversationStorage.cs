using AiStudio4.InjectedDependencies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IConversationStorage
    {
        /// <summary>
        /// Loads a conversation by its ID
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to load</param>
        /// <returns>The conversation, or a new one if not found</returns>
        Task<v4BranchedConversation> LoadConversation(string conversationId);

        /// <summary>
        /// Saves a conversation
        /// </summary>
        /// <param name="conversation">The conversation to save</param>
        Task SaveConversation(v4BranchedConversation conversation);

        /// <summary>
        /// Finds a conversation containing a specific message
        /// </summary>
        /// <param name="messageId">The ID of the message to find</param>
        /// <returns>The conversation containing the message, or null if not found</returns>
        Task<v4BranchedConversation> FindConversationByMessageId(string messageId);

        /// <summary>
        /// Gets all conversations
        /// </summary>
        /// <returns>All conversations</returns>
        Task<IEnumerable<v4BranchedConversation>> GetAllConversations();
    }
}