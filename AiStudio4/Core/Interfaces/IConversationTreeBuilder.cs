using AiStudio4.InjectedDependencies;
using System.Collections.Generic;

namespace AiStudio4.Core.Interfaces
{
    public interface IConversationTreeBuilder
    {
        /// <summary>
        /// Builds a tree representation of a conversation for historical view
        /// </summary>
        /// <param name="conversation">The conversation to build a tree from</param>
        /// <returns>A dynamic object representing the tree structure</returns>
        dynamic BuildHistoricalConversationTree(v4BranchedConversation conversation);

        /// <summary>
        /// Gets the message history path from a specific message back to the root
        /// </summary>
        /// <param name="conversation">The conversation containing the message</param>
        /// <param name="messageId">The ID of the message to get history for</param>
        /// <returns>A list of messages representing the path from root to the specified message</returns>
        List<v4BranchedConversationMessage> GetMessageHistory(v4BranchedConversation conversation, string messageId);

        /// <summary>
        /// Builds a tree node for a message
        /// </summary>
        /// <param name="message">The message to build a node for</param>
        /// <returns>A dynamic object representing the node</returns>
        dynamic BuildTreeNode(v4BranchedConversationMessage message);
    }
}