using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class ChatProcessingService
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IChatService _chatService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ChatProcessingService> _logger;
        private readonly SettingsManager _settingsManager;
        private readonly IToolService _toolService;
        private readonly ISystemPromptService _systemPromptService;

        public ChatProcessingService(
            IConversationStorage conversationStorage,
            IChatService chatService,
            IWebSocketNotificationService notificationService,
            ILogger<ChatProcessingService> logger,
            SettingsManager settingsManager,
            IToolService toolService,
            ISystemPromptService systemPromptService)
        {
            _conversationStorage = conversationStorage;
            _chatService = chatService;
            _notificationService = notificationService;
            _logger = logger;
            _settingsManager = settingsManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try
            {
                // Create and attach event handlers
                EventHandler<string> streamingHandler = (s, text) =>
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "cfrag", Content = text });
                EventHandler<string> completeHandler = (s, text) =>
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "endstream", Content = text });

                _chatService.StreamingTextReceived += streamingHandler;
                _chatService.StreamingComplete += completeHandler;

                try
                {
                    var chatRequest = new ChatRequest
                    {
                        ClientId = clientId,
                        ConversationId = (string)requestObject["conversationId"],
                        MessageId = (string)requestObject["newMessageId"],
                        ParentMessageId = (string)requestObject["parentMessageId"],
                        Message = (string)requestObject["message"],
                        Model = (string)requestObject["model"],
                        ToolIds = requestObject["toolIds"]?.ToObject<List<string>>() ?? new List<string>(),
                        SystemPromptId = (string)requestObject["systemPromptId"],
                        SystemPromptContent = (string)requestObject["systemPromptContent"]
                    };
                    System.Diagnostics.Debug.WriteLine($"--> Message: {chatRequest.Message}, MessageId: {chatRequest.MessageId}, ParentMessageId: {chatRequest.ParentMessageId}");

                    var conversation = await _conversationStorage.LoadConversation(chatRequest.ConversationId);
                    bool isFirstMessageInConversation = conversation.MessageHierarchy.Count <= 1 &&
                         (conversation.MessageHierarchy.Count == 0 || conversation.MessageHierarchy[0].Children.Count == 0);

                    var newUserMessage = conversation.AddNewMessage(v4BranchedConversationMessageRole.User, chatRequest.MessageId, chatRequest.Message, chatRequest.ParentMessageId);

                    // Save the conversation system prompt if provided
                    if (!string.IsNullOrEmpty(chatRequest.SystemPromptId))
                    {
                        conversation.SystemPromptId = chatRequest.SystemPromptId;
                        await _systemPromptService.SetConversationSystemPromptAsync(chatRequest.ConversationId, chatRequest.SystemPromptId);
                    }

                    await _conversationStorage.SaveConversation(conversation);

                    await _notificationService.NotifyConversationUpdate(clientId, new ConversationUpdateDto
                    {
                        ConversationId = conversation.ConversationId,
                        MessageId = newUserMessage.Id,
                        Content = newUserMessage.UserMessage,
                        ParentId = chatRequest.ParentMessageId,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Source = "user" // Explicitly set source as "user"
                    });

                    // Replace tree builder with direct message processing for conversation list notification
                    var messagesForClient = BuildFlatMessageStructure(conversation);
                    await _notificationService.NotifyConversationList(clientId, new ConversationListDto
                    {
                        ConversationId = conversation.ConversationId,
                        Summary = conversation.MessageHierarchy.First().Children.Count > 0
                            ? conversation.MessageHierarchy.First().Children[0].UserMessage ?? ""
                            : "New Conversation",
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        TreeData = messagesForClient
                    });

                    // Get message history without tree builder
                    var messageHistory = GetMessageHistory(conversation, chatRequest.MessageId);
                    chatRequest.MessageHistory = messageHistory.Select(msg => new MessageHistoryItem
                    {
                        Role = msg.Role.ToString().ToLower(),
                        Content = msg.UserMessage
                    }).ToList();

                    var response = await _chatService.ProcessChatRequest(chatRequest);
                    var newId = $"msg_{Guid.NewGuid()}";
                    var newAiReply = conversation.AddNewMessage(v4BranchedConversationMessageRole.Assistant, newId, response.ResponseText, chatRequest.MessageId);

                    System.Diagnostics.Debug.WriteLine($"<-- Message: {response.ResponseText}, MessageId: {newId}, ParentMessageId: {chatRequest.MessageId}");

                    if (isFirstMessageInConversation)
                    {
                        try
                        {
                            var secondaryModel = _settingsManager.DefaultSettings?.SecondaryModel;
                            if (!string.IsNullOrEmpty(secondaryModel))
                            {
                                var model = _settingsManager.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModel);
                                if (model != null)
                                {
                                    var message = $"Generate a concise 6 - 10 word summary of this conversation:\nUser: {(chatRequest.Message.Length > 250 ? chatRequest.Message.Substring(0, 250) : chatRequest.Message)}\nAI: {(response.ResponseText.Length > 250 ? response.ResponseText.Substring(0, 250) : response.ResponseText)}";

                                    var summaryChatRequest = new ChatRequest
                                    {
                                        ClientId = clientId,
                                        Model = secondaryModel,
                                        MessageHistory = new List<MessageHistoryItem> { new MessageHistoryItem { Content = message, Role = "user" } }
                                    };

                                    var service = SharedClasses.Providers.ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                                    var summaryResponse = await _chatService.ProcessChatRequest(summaryChatRequest);

                                    if (summaryResponse.Success)
                                    {
                                        var summary = summaryResponse.ResponseText.Length > 100
                                            ? summaryResponse.ResponseText.Substring(0, 97) + "..."
                                            : summaryResponse.ResponseText;

                                        conversation.Summary = summary;
                                        await _conversationStorage.SaveConversation(conversation);

                                        // Update client with the new summary, using our flat structure
                                        await _notificationService.NotifyConversationList(clientId, new ConversationListDto
                                        {
                                            ConversationId = conversation.ConversationId,
                                            Summary = summary,
                                            LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                            TreeData = BuildFlatMessageStructure(conversation)
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating conversation summary");
                        }
                    }

                    await _conversationStorage.SaveConversation(conversation);

                    await _notificationService.NotifyConversationUpdate(clientId, new ConversationUpdateDto
                    {
                        MessageId = newAiReply.Id,
                        Content = newAiReply.UserMessage,
                        ParentId = chatRequest.MessageId,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Source = "ai" // Explicitly set source as "ai"
                    });

                    return JsonConvert.SerializeObject(new { success = true, response = response });
                }
                finally
                {
                    _chatService.StreamingTextReceived -= streamingHandler;
                    _chatService.StreamingComplete -= completeHandler;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling chat request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        // Helper method to build flat message structure for client
        private List<object> BuildFlatMessageStructure(v4BranchedConversation conversation)
        {
            var allMessages = conversation.GetAllMessages();

            return allMessages.Select(msg => new {
                id = msg.Id,
                text = msg.UserMessage?.Length > 20
                    ? msg.UserMessage.Substring(0, 20) + "..."
                    : msg.UserMessage ?? "[Empty Message]",
                parentId = msg.ParentId,
                source = msg.Role == v4BranchedConversationMessageRole.User ? "user" :
                        msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system"
            }).ToList<object>();
        }

        // Helper method to get message history without using tree builder
        private List<v4BranchedConversationMessage> GetMessageHistory(v4BranchedConversation conversation, string messageId)
        {
            var allMessages = conversation.GetAllMessages();
            var path = new List<v4BranchedConversationMessage>();

            // Find the target message
            var currentMessage = allMessages.FirstOrDefault(m => m.Id == messageId);

            // Build path from message to root
            while (currentMessage != null)
            {
                // Add message to the beginning of the path (so we get root → leaf order)
                path.Insert(0, CloneMessage(currentMessage));

                // Stop if we've reached a message with no parent
                if (string.IsNullOrEmpty(currentMessage.ParentId))
                    break;

                // Find the parent of the current message
                currentMessage = allMessages.FirstOrDefault(m => m.Id == currentMessage.ParentId);
            }

            return path;
        }

        // Helper method to clone a message without children to avoid circular references
        private v4BranchedConversationMessage CloneMessage(v4BranchedConversationMessage message)
        {
            return new v4BranchedConversationMessage
            {
                Id = message.Id,
                UserMessage = message.UserMessage,
                Role = message.Role,
                ParentId = message.ParentId,
                Children = new List<v4BranchedConversationMessage>() // Empty children list
            };
        }
    }
}