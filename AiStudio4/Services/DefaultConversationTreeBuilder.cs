using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class DefaultConvTreeBuilder : IConvTreeBuilder
    {
        private readonly ILogger<DefaultConvTreeBuilder> _logger;

        public DefaultConvTreeBuilder(ILogger<DefaultConvTreeBuilder> logger)
        {
            _logger = logger;
        }

        public dynamic BuildHistoricalConvTree(v4BranchedConv conv)
        {
            try
            {
                if (conv?.MessageHierarchy == null || !conv.MessageHierarchy.Any())
                {
                    _logger.LogWarning("Attempted to build tree from empty conv {ConvId}", conv?.ConvId);
                    return null;
                }

                // Get all messages in a flat list
                var allMessages = GetAllMessagesFlat(conv);

                // Find root messages (those with no parent or parent outside the conv)
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

                _logger.LogDebug("Built conv tree for {ConvId}", conv.ConvId);
                return treeNodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building conv tree for {ConvId}", conv?.ConvId);
                throw new ConvTreeException("Failed to build conv tree", ex);
            }
        }

        public List<v4BranchedConvMessage> GetMessageHistory(v4BranchedConv conv, string messageId)
        {
            try
            {
                if (conv == null) throw new ArgumentNullException(nameof(conv));
                if (string.IsNullOrEmpty(messageId)) throw new ArgumentException("Message ID cannot be empty", nameof(messageId));

                // Get all messages
                var allMessages = GetAllMessagesFlat(conv);

                // Find the target message
                var targetMessage = allMessages.FirstOrDefault(m => m.Id == messageId);
                if (targetMessage == null)
                {
                    _logger.LogWarning("Message {MessageId} not found in conv {ConvId}",
                        messageId, conv.ConvId);
                    return new List<v4BranchedConvMessage>();
                }

                // Build the path from this message up to the root
                var path = new List<v4BranchedConvMessage>();
                var currentMessage = targetMessage;

                while (currentMessage != null)
                {
                    path.Insert(0, CloneMessage(currentMessage));

                    if (string.IsNullOrEmpty(currentMessage.ParentId))
                        break;

                    currentMessage = allMessages.FirstOrDefault(m => m.Id == currentMessage.ParentId);
                }

                _logger.LogDebug("Retrieved message history for {MessageId} in {ConvId}",
                    messageId, conv.ConvId);
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message history for {MessageId}", messageId);
                throw new ConvTreeException($"Failed to get message history for {messageId}", ex);
            }
        }

        public dynamic BuildTreeNode(v4BranchedConvMessage message)
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
                throw new ConvTreeException($"Failed to build tree node for {message?.Id}", ex);
            }
        }

        private v4BranchedConvMessage CloneMessage(v4BranchedConvMessage message)
        {
            return new v4BranchedConvMessage
            {
                Id = message.Id,
                UserMessage = message.UserMessage,
                Role = message.Role,
                ParentId = message.ParentId,
                // Don't clone children to avoid circular references
                Children = new List<v4BranchedConvMessage>(),
                TokenUsage = message.TokenUsage,
                CostInfo = message.CostInfo
            };
        }

        private List<v4BranchedConvMessage> GetAllMessagesFlat(v4BranchedConv conv)
        {
            var result = new List<v4BranchedConvMessage>();
            CollectAllMessages(conv.MessageHierarchy, result);
            return result;
        }

        private void CollectAllMessages(IEnumerable<v4BranchedConvMessage> messages,
            List<v4BranchedConvMessage> allMessages)
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