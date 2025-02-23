using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace AiStudio4.Services
{
    public class ChatProcessingService
    {
        private readonly IConversationStorage _conversationStorage;
        private readonly IConversationTreeBuilder _treeBuilder;
        private readonly IChatService _chatService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ChatProcessingService> _logger;

        public ChatProcessingService(
            IConversationStorage conversationStorage,
            IConversationTreeBuilder treeBuilder,
            IChatService chatService,
            IWebSocketNotificationService notificationService,
            ILogger<ChatProcessingService> logger)
        {
            _conversationStorage = conversationStorage;
            _treeBuilder = treeBuilder;
            _chatService = chatService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try 
            {
                // Create event handlers
                EventHandler<string> streamingHandler = (s, text) => 
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "cfrag", Content = text });
                EventHandler<string> completeHandler = (s, text) => 
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "endstream", Content = text });

                // Attach event handlers
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
                finally
                {
                    // Cleanup event handlers
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
    }
}