using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class ChatManager
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IConversationTreeBuilder _treeBuilder;
        private readonly IChatService _chatService;
        private readonly IWebSocketNotificationService _notificationService;

        public ChatManager(
            IConversationStorage conversationStorage,
            IConversationTreeBuilder treeBuilder,
            IChatService chatService,
            IWebSocketNotificationService notificationService)
        {
            _conversationStorage = conversationStorage;
            _treeBuilder = treeBuilder;
            _chatService = chatService;
            _notificationService = notificationService;
        }

        public async Task<string> HandleGetAllConversationsRequest(string clientId)
        {
            try
            {
                var conversations = await _conversationStorage.GetAllConversations();

                foreach (var conversation in conversations.Where(c => c.MessageHierarchy.Any()))
                {
                    var tree = _treeBuilder.BuildCachedConversationTree(conversation);
                    
                    await _notificationService.NotifyConversationList(clientId, new ConversationListDto
                    {
                        ConversationId = conversation.ConversationId,
                        Summary = conversation.MessageHierarchy.First().Children[0].UserMessage,
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        TreeData = tree
                    });
                }

                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try 
            {
                var chatRequest = new ChatRequest
                {
                    ClientId = clientId,
                    ConversationId = (string)requestObject["conversationId"],
                    MessageId = (string)requestObject["newMessageId"],
                    ParentMessageId = (string)requestObject["parentMessageId"],
                    Message = (string)requestObject["message"],
                    Model = (string)requestObject["model"]
                };

                var conversation = await _conversationStorage.LoadConversation(chatRequest.ConversationId);
                var newUserMessage = conversation.AddNewMessage(v4BranchedConversationMessageRole.User, chatRequest.MessageId, chatRequest.Message, chatRequest.ParentMessageId);
                await _conversationStorage.SaveConversation(conversation);

                // Update tree and notify clients
                var tree = _treeBuilder.BuildCachedConversationTree(conversation);
                await _notificationService.NotifyConversationList(clientId, new ConversationListDto
                {
                    ConversationId = conversation.ConversationId,
                    Summary = conversation.MessageHierarchy.First().Children[0].UserMessage ?? "",
                    LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    TreeData = tree
                });

                // Get message history and process chat
                var messageHistory = _treeBuilder.GetMessageHistory(conversation, chatRequest.MessageId);
                chatRequest.MessageHistory = messageHistory.Select(msg => new MessageHistoryItem 
                {
                    Role = msg.Role.ToString().ToLower(),
                    Content = msg.UserMessage
                }).ToList();

                _chatService.StreamingTextReceived += (s, text) => _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "cfrag", Content = text });
                _chatService.StreamingComplete += (s, text) => _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "endstream", Content = text });

                var response = await _chatService.ProcessChatRequest(chatRequest);
                var newAiReply = conversation.AddNewMessage(v4BranchedConversationMessageRole.Assistant, $"msg_{Guid.NewGuid()}", response.ResponseText, chatRequest.MessageId);
                await _conversationStorage.SaveConversation(conversation);

                await _notificationService.NotifyConversationUpdate(clientId, new ConversationUpdateDto
                {
                    MessageId = newAiReply.Id,
                    Content = newAiReply.UserMessage,
                    ParentId = chatRequest.MessageId,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });

                return JsonConvert.SerializeObject(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        internal async Task<string> HandleConversationMessagesRequest(string clientId, JObject? requestObject)
        {
            try
            {
                var messageId = requestObject["messageId"].ToString();
                var conversation = await _conversationStorage.FindConversationByMessageId(messageId);
                
                if (conversation != null)
                {
                    var messageHistory = _treeBuilder.GetMessageHistory(conversation, messageId);
                    var messages = messageHistory.Select(msg => new
                    {
                        id = msg.Id,
                        content = msg.UserMessage,
                        source = msg.Role == v4BranchedConversationMessageRole.User ? "user" : 
                                msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }).ToList();

                    await _notificationService.NotifyConversationUpdate(clientId, new ConversationUpdateDto
                    {
                        ConversationId = conversation.ConversationId,
                        MessageId = messageId,
                        Content = new
                        {
                            messageType = "loadConversation",
                            content = new
                            {
                                conversationId = conversation.ConversationId,
                                messages = messages
                            }
                        }
                    });

                    return JsonConvert.SerializeObject(new { success = true, messages = messages, conversationId = conversation.ConversationId });
                }
                return JsonConvert.SerializeObject(new { success = false, error = "Message not found" });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }
        internal async Task<string> HandleCachedConversationRequest(string clientId, JObject? requestObject)
        {
            try
            {
                var conversationId = requestObject["conversationId"].ToString();
                var conversation = await _conversationStorage.LoadConversation(conversationId);

                if (conversation == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Conversation not found" });
                }

                var tree = _treeBuilder.BuildCachedConversationTree(conversation);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    treeData = tree
                });
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }



    }



}