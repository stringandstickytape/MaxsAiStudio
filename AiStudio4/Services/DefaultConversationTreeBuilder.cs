using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using AiTool3.Conversations;
using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;

namespace AiStudio4.Services
{
    public class DefaultConversationTreeBuilder : IConversationTreeBuilder
    {
        private readonly ILogger<DefaultConversationTreeBuilder> _logger;

        public DefaultConversationTreeBuilder(ILogger<DefaultConversationTreeBuilder> logger)
        {
            _logger = logger;
        }

        public dynamic BuildCachedConversationTree(v4BranchedConversation conversation)
        {
            try
            {
                if (conversation?.MessageHierarchy == null || !conversation.MessageHierarchy.Any())
                {
                    _logger.LogWarning("Attempted to build tree from empty conversation {ConversationId}", conversation?.ConversationId);
                    return null;
                }

                var tree = BuildTreeNode(conversation.MessageHierarchy[0]);
                _logger.LogDebug("Built conversation tree for {ConversationId}", conversation.ConversationId);
                return tree;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building conversation tree for {ConversationId}", conversation?.ConversationId);
                throw new ConversationTreeException("Failed to build conversation tree", ex);
            }
        }

        public List<v4BranchedConversationMessage> GetMessageHistory(v4BranchedConversation conversation, string messageId)
        {
            try
            {
                if (conversation == null) throw new ArgumentNullException(nameof(conversation));
                if (string.IsNullOrEmpty(messageId)) throw new ArgumentException("Message ID cannot be empty", nameof(messageId));

                var history = new List<v4BranchedConversationMessage>();
                if (!BuildMessageHistory(conversation.MessageHierarchy, messageId, history))
                {
                    _logger.LogWarning("Message {MessageId} not found in conversation {ConversationId}", messageId, conversation.ConversationId);
                    return new List<v4BranchedConversationMessage>();
                }

                history.Reverse();
                _logger.LogDebug("Retrieved message history for {MessageId} in {ConversationId}", messageId, conversation.ConversationId);
                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message history for {MessageId}", messageId);
                throw new ConversationTreeException($"Failed to get message history for {messageId}", ex);
            }
        }

        public dynamic BuildTreeNode(v4BranchedConversationMessage message)
        {
            try
            {
                if (message == null) throw new ArgumentNullException(nameof(message));

                var text = message.UserMessage;
                if (text?.Length > 20) text = text.Substring(0, 20);

                var node = new
                {
                    id = message.Id,
                    text = text ?? "[Empty Message]",
                    children = message.Children?.Select(BuildTreeNode).ToList() ?? new List<dynamic>()
                };

                _logger.LogTrace("Built tree node for message {MessageId}", message.Id);
                return node;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building tree node for message {MessageId}", message?.Id);
                throw new ConversationTreeException($"Failed to build tree node for {message?.Id}", ex);
            }
        }

        private bool BuildMessageHistory(IEnumerable<v4BranchedConversationMessage> messages, string targetId, List<v4BranchedConversationMessage> history)
        {
            foreach (var message in messages)
            {
                if (message.Id == targetId)
                {
                    history.Add(message);
                    return true;
                }

                if (BuildMessageHistory(message.Children, targetId, history))
                {
                    history.Add(message);
                    return true;
                }
            }

            return false;
        }
    }
}