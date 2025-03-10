using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using AiTool3.Conversations;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class DefaultConversationTreeBuilder : IConversationTreeBuilder
    {
        private readonly ILogger<DefaultConversationTreeBuilder> _logger;

        public DefaultConversationTreeBuilder(ILogger<DefaultConversationTreeBuilder> logger)
        {
            _logger = logger;
        }

        public dynamic BuildHistoricalConversationTree(v4BranchedConversation conversation)
        {
            try
            {
                if (conversation?.MessageHierarchy == null || !conversation.MessageHierarchy.Any())
                {
                    _logger.LogWarning("Attempted to build tree from empty conversation {ConversationId}", conversation?.ConversationId);
                    return null;
                }

                // Get all messages in a flat list
                var allMessages = GetAllMessagesFlat(conversation);

                // Find root messages (those with no parent or parent outside the conversation)
                var rootMessages = allMessages
                    .Where(m => string.IsNullOrEmpty(m.ParentId) ||
                                !allMessages.Any(am => am.Id == m.ParentId))
                    .ToList();

                // If we don't have any root messages, use the first message as root
                if (!rootMessages.Any() && allMessages.Any())
                {
                    rootMessages.Add(allMessages.First());
                }

                // Build tree nodes for each root
                var treeNodes = rootMessages.Select(BuildTreeNode).ToList();

                _logger.LogDebug("Built conversation tree for {ConversationId}", conversation.ConversationId);
                return treeNodes;
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

                // Get all messages
                var allMessages = GetAllMessagesFlat(conversation);

                // Find the target message
                var targetMessage = allMessages.FirstOrDefault(m => m.Id == messageId);
                if (targetMessage == null)
                {
                    _logger.LogWarning("Message {MessageId} not found in conversation {ConversationId}",
                        messageId, conversation.ConversationId);
                    return new List<v4BranchedConversationMessage>();
                }

                // Build the path from this message up to the root
                var path = new List<v4BranchedConversationMessage>();
                var currentMessage = targetMessage;

                while (currentMessage != null)
                {
                    path.Insert(0, CloneMessage(currentMessage));

                    if (string.IsNullOrEmpty(currentMessage.ParentId))
                        break;

                    currentMessage = allMessages.FirstOrDefault(m => m.Id == currentMessage.ParentId);
                }

                _logger.LogDebug("Retrieved message history for {MessageId} in {ConversationId}",
                    messageId, conversation.ConversationId);
                return path;
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
                if (text?.Length > 20)
                    text = text.Substring(0, 20) + "...";

                // Build children nodes recursively
                var childNodes = message.Children?
                    .Select(BuildTreeNode)
                    .ToList() ?? new List<dynamic>();

                var node = new
                {
                    id = message.Id,
                    text = text ?? "[Empty Message]",
                    children = childNodes
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

        private v4BranchedConversationMessage CloneMessage(v4BranchedConversationMessage message)
        {
            return new v4BranchedConversationMessage
            {
                Id = message.Id,
                UserMessage = message.UserMessage,
                Role = message.Role,
                ParentId = message.ParentId,
                // Don't clone children to avoid circular references
                Children = new List<v4BranchedConversationMessage>(),
                TokenUsage = message.TokenUsage,
                CostInfo = message.CostInfo
            };
        }

        private List<v4BranchedConversationMessage> GetAllMessagesFlat(v4BranchedConversation conversation)
        {
            var result = new List<v4BranchedConversationMessage>();
            CollectAllMessages(conversation.MessageHierarchy, result);
            return result;
        }

        private void CollectAllMessages(IEnumerable<v4BranchedConversationMessage> messages,
            List<v4BranchedConversationMessage> allMessages)
        {
            foreach (var message in messages)
            {
                allMessages.Add(message);
                if (message.Children?.Any() == true)
                {
                    CollectAllMessages(message.Children, allMessages);
                }
            }
        }
    }
}