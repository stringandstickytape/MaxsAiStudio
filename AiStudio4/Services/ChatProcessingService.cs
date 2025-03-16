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
using System.Text.RegularExpressions;
using AiTool3.Helpers;
using SharedClasses;
using SharedClasses.Helpers;

namespace AiStudio4.Services
{
    public class ChatProcessingService
    {
        private readonly IConvStorage _convStorage;
        private readonly IChatService _chatService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ChatProcessingService> _logger;
        private readonly SettingsManager _settingsManager;
        private readonly IToolService _toolService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly ClientRequestCancellationService _cancellationService;

        public ChatProcessingService(
            IConvStorage convStorage,
            IChatService chatService,
            IWebSocketNotificationService notificationService,
            ILogger<ChatProcessingService> logger,
            SettingsManager settingsManager,
            IToolService toolService,
            ISystemPromptService systemPromptService,
            ClientRequestCancellationService cancellationService)
        {
            _convStorage = convStorage;
            _chatService = chatService;
            _notificationService = notificationService;
            _logger = logger;
            _settingsManager = settingsManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _cancellationService = cancellationService;
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try
            {
                // Get cancellation token from the service
                var cancellationToken = _cancellationService.AddTokenSource(clientId);
                
                // Create and attach event handlers
                EventHandler<string> streamingHandler = (s, text) =>
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "cfrag", Content = text });
                EventHandler<string> completeHandler = (s, text) =>
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "endstream", Content = text });

                _chatService.StreamingTextReceived += streamingHandler;
                _chatService.StreamingComplete += completeHandler;

                try
                {
                    var message = (string)requestObject["message"];
                        
                    var matches =Regex.Matches(message, @"\[pull:(.*?)\]");

                    foreach (Match match in matches)
                    {
                        var url = match.Groups[1].Value;
                        var extractedText = await HtmlTextExtractor.ExtractTextFromUrlAsync(url);
                        if (extractedText != "")
                        {
                            message = message.Replace(match.Value, $"\n{BacktickHelper.ThreeTicks}{url}\n{extractedText}\n{BacktickHelper.ThreeTicks}\n");
                        }
                    }
                    var chatRequest = new ChatRequest
                    {
                        ClientId = clientId,
                        ConvId = (string)requestObject["convId"],
                        MessageId = (string)requestObject["newMessageId"],
                        ParentMessageId = (string)requestObject["parentMessageId"],
                        Message = message,
                        Model = (string)requestObject["model"],
                        ToolIds = requestObject["toolIds"]?.ToObject<List<string>>() ?? new List<string>(),
                        SystemPromptId = (string)requestObject["systemPromptId"],
                        SystemPromptContent = (string)requestObject["systemPromptContent"],
                        CancellationToken = cancellationToken
                    };
                    System.Diagnostics.Debug.WriteLine($"--> Message: {chatRequest.Message}, MessageId: {chatRequest.MessageId}, ParentMessageId: {chatRequest.ParentMessageId}");

                    var conv = await _convStorage.LoadConv(chatRequest.ConvId);
                    // Check if this is the first non-system message in the conversation
                    bool isFirstMessageInConv = conv.Messages.Count <= 1 ||
                        (conv.Messages.Count == 2 && conv.Messages.Any(m => m.Role == v4BranchedConvMessageRole.System));

                    var newUserMessage = conv.AddNewMessage(v4BranchedConvMessageRole.User, chatRequest.MessageId, chatRequest.Message, chatRequest.ParentMessageId);

                    // Save the conv system prompt if provided
                    if (!string.IsNullOrEmpty(chatRequest.SystemPromptId))
                    {
                        conv.SystemPromptId = chatRequest.SystemPromptId;
                        await _systemPromptService.SetConvSystemPromptAsync(chatRequest.ConvId, chatRequest.SystemPromptId);
                    }

                    await _convStorage.SaveConv(conv);

                    await _notificationService.NotifyConvUpdate(clientId, new ConvUpdateDto
                    {
                        ConvId = conv.ConvId,
                        MessageId = newUserMessage.Id,
                        Content = newUserMessage.UserMessage,
                        ParentId = chatRequest.ParentMessageId,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Source = "user" // Explicitly set source as "user"
                    });

                    // Replace tree builder with direct message processing for conv list notification
                    var messagesForClient = BuildFlatMessageStructure(conv);
                    await _notificationService.NotifyConvList(clientId, new ConvListDto
                    {
                        ConvId = conv.ConvId,
                        Summary = conv.Messages.Count > 1
                            ? conv.Messages.FirstOrDefault(m => m.Role == v4BranchedConvMessageRole.User)?.UserMessage ?? "New Conv"
                            : "New Conv",
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        FlatMessageStructure = messagesForClient
                    });

                    // Get message history without tree builder
                    var messageHistory = GetMessageHistory(conv, chatRequest.MessageId);
                    chatRequest.MessageHistory = messageHistory.Select(msg => new MessageHistoryItem
                    {
                        Role = msg.Role.ToString().ToLower(),
                        Content = msg.UserMessage
                    }).ToList();

                    var response = await _chatService.ProcessChatRequest(chatRequest);
                    var newId = $"msg_{Guid.NewGuid()}";
                    var newAiReply = conv.AddNewMessage(v4BranchedConvMessageRole.Assistant, newId, response.ResponseText, chatRequest.MessageId);

                    // Store cost information if available
                    if (response.CostInfo != null)
                    {
                        newAiReply.CostInfo = response.CostInfo;
                    }

                    System.Diagnostics.Debug.WriteLine($"<-- Message: {response.ResponseText}, MessageId: {newId}, ParentMessageId: {chatRequest.MessageId}");

                    if (isFirstMessageInConv)
                    {
                        try
                        {
                            var secondaryModel = _settingsManager.DefaultSettings?.SecondaryModel;
                            if (!string.IsNullOrEmpty(secondaryModel))
                            {
                                var model = _settingsManager.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModel);
                                if (model != null)
                                {
                                    var summaryMessage = $"Generate a concise 6 - 10 word summary of this conv:\nUser: {(chatRequest.Message.Length > 250 ? chatRequest.Message.Substring(0, 250) : chatRequest.Message)}\nAI: {(response.ResponseText.Length > 250 ? response.ResponseText.Substring(0, 250) : response.ResponseText)}";

                                    var summaryChatRequest = new ChatRequest
                                    {
                                        ClientId = clientId,
                                        Model = secondaryModel,
                                        MessageHistory = new List<MessageHistoryItem> { new MessageHistoryItem { Content = summaryMessage, Role = "user" } }
                                    };

                                    var service = SharedClasses.Providers.ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                                    var summaryResponse = await _chatService.ProcessChatRequest(summaryChatRequest);

                                    if (summaryResponse.Success)
                                    {
                                        var summary = summaryResponse.ResponseText.Length > 100
                                            ? summaryResponse.ResponseText.Substring(0, 97) + "..."
                                            : summaryResponse.ResponseText;

                                        conv.Summary = summary;
                                        await _convStorage.SaveConv(conv);

                                        // Update client with the new summary, using our flat structure
                                        await _notificationService.NotifyConvList(clientId, new ConvListDto
                                        {
                                            ConvId = conv.ConvId,
                                            Summary = summary,
                                            LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                            FlatMessageStructure = BuildFlatMessageStructure(conv)
                                        });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error generating conv summary");
                        }
                    }

                    await _convStorage.SaveConv(conv);

                    await _notificationService.NotifyConvUpdate(clientId, new ConvUpdateDto
                    {
                        MessageId = newAiReply.Id,
                        Content = newAiReply.UserMessage,
                        ParentId = chatRequest.MessageId,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Source = "ai", // Explicitly set source as "ai"
                        CostInfo = newAiReply.CostInfo
                    });

                    return JsonConvert.SerializeObject(new { success = true, response = response });
                }
                finally
                {
                    _chatService.StreamingTextReceived -= streamingHandler;
                    _chatService.StreamingComplete -= completeHandler;
                    _cancellationService.RemoveTokenSource(clientId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling chat request");
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        // Helper method to build flat message structure for client
        private List<object> BuildFlatMessageStructure(v4BranchedConv conv)
        {
            var allMessages = conv.GetAllMessages();

            return allMessages.Select(msg => new {
                id = msg.Id,
                text = msg.UserMessage ?? "[Empty Message]",
                parentId = msg.ParentId,
                source = msg.Role == v4BranchedConvMessageRole.User ? "user" :
                        msg.Role == v4BranchedConvMessageRole.Assistant ? "ai" : "system",
                costInfo = msg.CostInfo
            }).ToList<object>();
        }

        // Helper method to get message history without using tree builder
        private List<v4BranchedConvMessage> GetMessageHistory(v4BranchedConv conv, string messageId)
        {
            var allMessages = conv.GetAllMessages();
            var path = new List<v4BranchedConvMessage>();

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

        // Helper method to clone a message
        private v4BranchedConvMessage CloneMessage(v4BranchedConvMessage message)
        {
            return new v4BranchedConvMessage
            {
                Id = message.Id,
                UserMessage = message.UserMessage,
                Role = message.Role,
                ParentId = message.ParentId,
                CostInfo = message.CostInfo
            };
        }
    }
}