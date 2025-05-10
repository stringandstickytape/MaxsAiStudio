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
using SharedClasses;
using SharedClasses.Helpers;
using AiStudio4.DataModels;
using Microsoft.Extensions.DependencyInjection; // Added for IServiceProvider

namespace AiStudio4.Services
{
    public class ChatProcessingService
    {
        private readonly IStatusMessageService _statusMessageService;
        private readonly IConvStorage _convStorage;
        private readonly IChatService _chatService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ChatProcessingService> _logger;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IToolService _toolService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly ClientRequestCancellationService _cancellationService;
        private readonly IServiceProvider _serviceProvider; // Added for scoping
        private readonly IProjectHistoryService _projectHistoryService; // Added
        

        public ChatProcessingService(
            IConvStorage convStorage,
            IChatService chatService,
            IWebSocketNotificationService notificationService,
            ILogger<ChatProcessingService> logger,
            IGeneralSettingsService generalSettingsService,
            IToolService toolService,
            ISystemPromptService systemPromptService,
            ClientRequestCancellationService cancellationService,
            IServiceProvider serviceProvider,
            IStatusMessageService statusMessageService,
            IProjectHistoryService projectHistoryService) // Added IProjectHistoryService
        {
            _statusMessageService = statusMessageService;
            _convStorage = convStorage;
            _chatService = chatService;
            _notificationService = notificationService;
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _cancellationService = cancellationService;
            _serviceProvider = serviceProvider; // Assign injected service provider
            _projectHistoryService = projectHistoryService; // Added
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try
            {
                await _statusMessageService.SendStatusMessageAsync(clientId, "Preparing request...");

                // Get cancellation token from the service
                var cancellationToken = _cancellationService.AddTokenSource(clientId);

                // Define callbacks inline, capturing clientId
                Action<string> streamingUpdateCallback = (text) =>
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "cfrag", Content = text });
                Action streamingCompleteCallback = () =>
                    _notificationService.NotifyStreamingUpdate(clientId, new StreamingUpdateDto { MessageType = "endstream", Content = "" }); // Send empty content on complete
                bool isFirstMessageInConv = false;
                ChatRequest chatRequest = null;
                v4BranchedConv? conv = null;
                try
                {
                    // Check for cancellation before starting main processing
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);
                    var message = (string)requestObject["message"];
                        
                    var matches =Regex.Matches(message, @"\[pull:(.*?)\]");

                    foreach (Match match in matches)
                    {
                        // Check for cancellation during slow HTML extraction
                        if (cancellationToken.IsCancellationRequested)
                            throw new OperationCanceledException(cancellationToken);
                        var url = match.Groups[1].Value;
                        var extractedText = await HtmlTextExtractor.ExtractTextFromUrlAsync(url);
                        if (extractedText != "")
                        {
                            message = message.Replace(match.Value, $"\n{BacktickHelper.ThreeTicks}{url}\n{extractedText}\n{BacktickHelper.ThreeTicks}\n");
                        }
                    }

                    await _statusMessageService.SendStatusMessageAsync(clientId, "Parsing attachments...");

                    // Parse attachments if present
                    var attachments = new List<Attachment>();
                    if (requestObject["attachments"] != null && requestObject["attachments"].Type != JTokenType.Null)
                    {
                        foreach (var attachment in requestObject["attachments"])
                        {
                            // Check for cancellation during attachment parsing
                            if (cancellationToken.IsCancellationRequested)
                                throw new OperationCanceledException(cancellationToken);
                            attachments.Add(new Attachment
                            {
                                Id = (string)attachment["id"],
                                Name = (string)attachment["name"],
                                Type = (string)attachment["type"],
                                Content = (string)attachment["content"],
                                Size = (long)attachment["size"],
                                Width = attachment["metadata"]?["width"]?.ToObject<int>(),
                                Height = attachment["metadata"]?["height"]?.ToObject<int>(),
                                TextContent = (string)attachment["textContent"],
                                LastModified = attachment["metadata"]?["lastModified"]?.ToObject<long>()
                            });
                        }
                    }

                    // Check for cancellation before loading conversation
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    await _statusMessageService.SendStatusMessageAsync(clientId, $"Loading conversation {(string)requestObject["convId"]}...");

                    conv = await _convStorage.LoadConv((string)requestObject["convId"]);

                    chatRequest = new ChatRequest
                    {
                        ClientId = clientId,
                        MessageId = (string)requestObject["newMessageId"],
                        ParentMessageId = (string)requestObject["parentMessageId"],
                        Message = message,
                        Model = (string)requestObject["model"],
                        ToolIds = requestObject["toolIds"]?.ToObject<List<string>>() ?? new List<string>(),
                        SystemPromptId = (string)requestObject["systemPromptId"],
                        SystemPromptContent = (string)requestObject["systemPromptContent"],
                        CancellationToken = cancellationToken,
                        OnStreamingUpdate = streamingUpdateCallback, // Pass the callback
                        OnStreamingComplete = streamingCompleteCallback, // Pass the callback
                        BranchedConv = conv
                    };

                    System.Diagnostics.Debug.WriteLine($"--> Message: {chatRequest.Message}, MessageId: {chatRequest.MessageId}, ParentMessageId: {chatRequest.ParentMessageId}");
                    
                    // Check if this is the first non-system message in the conversation
                    isFirstMessageInConv = conv.Messages.Count <= 1 ||
                        (conv.Messages.Count == 2 && conv.Messages.Any(m => m.Role == v4BranchedConvMessageRole.System));

                    var newUserMessage = conv.AddNewMessage(v4BranchedConvMessageRole.User, chatRequest.MessageId, chatRequest.Message, chatRequest.ParentMessageId);
                    
                    // Add attachments to the message
                    if (attachments != null && attachments.Any())
                    {
                        newUserMessage.Attachments.AddRange(attachments);
                    }

                    // Save the conv system prompt if provided
                    if (!string.IsNullOrEmpty(chatRequest.SystemPromptId))
                    {
                        conv.SystemPromptId = chatRequest.SystemPromptId;
                        await _systemPromptService.SetConvSystemPromptAsync(chatRequest.BranchedConv.ConvId, chatRequest.SystemPromptId);
                    }

                    await _convStorage.SaveConv(conv);

                    await _notificationService.NotifyConvUpdate(clientId, new ConvUpdateDto
                    {
                        ConvId = conv.ConvId,
                        MessageId = newUserMessage.Id,
                        Content = newUserMessage.UserMessage,
                        ParentId = chatRequest.ParentMessageId,
                        Timestamp = new DateTimeOffset(newUserMessage.Timestamp).ToUnixTimeMilliseconds(),
                        Source = "user", // Explicitly set source as "user"
                        Attachments = newUserMessage.Attachments,
                        DurationMs = 0, // User messages have zero processing duration
                    });

                    // Replace tree builder with direct message processing for conv list notification
                    var messagesForClient = BuildFlatMessageStructure(conv);
                    await _notificationService.NotifyConvList(new ConvListDto
                    {
                        ConvId = conv.ConvId,
                        Summary = conv.Summary ?? (conv.Messages.Count > 1
                            ? conv.Messages.FirstOrDefault(m => m.Role == v4BranchedConvMessageRole.User)?.UserMessage ?? "New Conv"
                            : "New Conv"),
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        FlatMessageStructure = messagesForClient
                    });


                    chatRequest.BranchedConv = conv;

                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    // Determine effective project path
                    string projectFolderId = requestObject["projectFolderId"]?.ToString();
                    string effectiveProjectPath = _generalSettingsService.CurrentSettings.ProjectPath; // Default

                    if (!string.IsNullOrEmpty(projectFolderId))
                    {
                        string pathFromId = await _projectHistoryService.GetProjectPathByIdAsync(projectFolderId);
                        if (!string.IsNullOrEmpty(pathFromId) && Directory.Exists(pathFromId)) // Validate path exists
                        {
                            effectiveProjectPath = pathFromId;
                            _logger.LogInformation("Using project context from client ID '{ProjectFolderId}': {Path}", projectFolderId, effectiveProjectPath);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid or non-existent projectFolderId '{ProjectFolderId}' received. Falling back to server default: {DefaultPath}", projectFolderId, effectiveProjectPath);
                        }
                    }
                    
                    // Pass effectiveProjectPath to ProcessChatRequest
                    var response = await _chatService.ProcessChatRequest(chatRequest, effectiveProjectPath);



                    // Save the conversation state *before* returning the response, 
                    // but potentially before the background summary task completes.
                    await _convStorage.SaveConv(conv);

                    return JsonConvert.SerializeObject(new { success = true, response = response });
                }
                finally
                {
                    await _statusMessageService.ClearStatusMessageAsync(clientId);

                    // Remove old event unsubscribing
                    // _chatService.StreamingTextReceived -= streamingHandler;
                    // _chatService.StreamingComplete -= completeHandler;
                    _cancellationService.RemoveTokenSource(clientId, cancellationToken);

                    if (isFirstMessageInConv && conv != null && chatRequest != null)
                    {
                        // Run summary generation in the background using a proper DI scope
                        _ = Task.Run(async () =>
                        {
                            using (var scope = _serviceProvider.CreateScope()) // Create DI scope
                            {
                                var scopedConvStorage = scope.ServiceProvider.GetRequiredService<IConvStorage>();
                                var scopedChatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                                var scopedSettingsService = scope.ServiceProvider.GetRequiredService<IGeneralSettingsService>();
                                var scopedNotificationService = scope.ServiceProvider.GetRequiredService<IWebSocketNotificationService>();
                                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<ChatProcessingService>>(); // Resolve logger from scope
                                string conversationId = conv.ConvId; // Capture convId for use in background task

                                try
                                {
                                    var secondaryModelName = scopedSettingsService.CurrentSettings.SecondaryModel;
                                    if (!string.IsNullOrEmpty(secondaryModelName))
                                    {
                                        var model = scopedSettingsService.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModelName);
                                        if (model != null)
                                        {
                                            // Use captured request/response excerpts
                                            string userMessageExcerpt = chatRequest.Message.Length > 250 ? chatRequest.Message.Substring(0, 250) : chatRequest.Message;
                                            string aiResponseExcerpt = conv.Messages.Last().UserMessage.Length > 250 ? conv.Messages.Last().UserMessage.Substring(0, 250) : conv.Messages.Last().UserMessage;
                                            var summaryPrompt = $"Generate a concise 6 - 10 word summary of the following content.  Produce NO OTHER OUTPUT WHATSOEVER.  \n\n```txt\nUser: {userMessageExcerpt}\nAI: {aiResponseExcerpt}\n```\n";

                                            // Use scoped chat service
                                            var summaryResponse = await scopedChatService.ProcessSimpleChatRequest(summaryPrompt); // Pass necessary model/provider info if needed by ProcessSimpleChatRequest

                                            if (summaryResponse.Success)
                                            {
                                                var summary = summaryResponse.ResponseText.Length > 100
                                                    ? summaryResponse.ResponseText.Substring(0, 97) + "..."
                                                    : summaryResponse.ResponseText;

                                                // Re-load the conversation within the scope before modifying/saving
                                                var currentConv = await scopedConvStorage.LoadConv(conversationId);
                                                if (currentConv != null)
                                                {
                                                    currentConv.Summary = summary;
                                                    await scopedConvStorage.SaveConv(currentConv); // Use scoped storage

                                                    // Update client using scoped notification service, with error handling
                                                    try
                                                    {
                                                        await scopedNotificationService.NotifyConvList(new ConvListDto
                                                        {
                                                            ConvId = currentConv.ConvId,
                                                            Summary = summary,
                                                            LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                                            FlatMessageStructure = BuildFlatMessageStructure(currentConv) // Use the re-loaded conv
                                                        });
                                                    }
                                                    catch (Exception notifyEx)
                                                    {
                                                        // Log error if notification fails (e.g., client disconnected)
                                                        scopedLogger.LogWarning(notifyEx, "Failed to send summary update notification to client {ClientId} for ConvId {ConvId}", clientId, conversationId);
                                                    }
                                                }
                                                else
                                                {
                                                    scopedLogger.LogWarning("Conversation {ConvId} not found when trying to save summary in background task.", conversationId);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Use scoped logger and captured convId
                                    scopedLogger.LogError(ex, "Error generating conv summary in background task for ConvId {ConvId}", conversationId);
                                }
                            }
                        });
                    }
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
                costInfo = msg.CostInfo,
                attachments = msg.Attachments,
                timestamp = msg.Timestamp,
                durationMs = msg.DurationMs
            }).ToList<object>();
        }

        // Helper method to get message history without using tree builder -- REMOVED
        // private List<v4BranchedConvMessage> GetMessageHistory(v4BranchedConv conv, string messageId)
        // {
        //     var allMessages = conv.GetAllMessages();
        //     var path = new List<v4BranchedConvMessage>();

        //     // Find the target message
        //     var currentMessage = allMessages.FirstOrDefault(m => m.Id == messageId);

        //     // Build path from message to root
        //     while (currentMessage != null)
        //     {
        //         // Add message to the beginning of the path (so we get root → leaf order)
        //         path.Insert(0, CloneMessage(currentMessage));

        //         // Stop if we've reached a message with no parent
        //         if (string.IsNullOrEmpty(currentMessage.ParentId))
        //             break;

        //         // Find the parent of the current message
        //         currentMessage = allMessages.FirstOrDefault(m => m.Id == currentMessage.ParentId);
        //     }

        //     return path;
        // }

        // Helper method to clone a message -- REMOVED
        // private v4BranchedConvMessage CloneMessage(v4BranchedConvMessage message)
        // {
        //     return new v4BranchedConvMessage
        //     {
        //         Id = message.Id,
        //         UserMessage = message.UserMessage,
        //         Role = message.Role,
        //         ParentId = message.ParentId,
        //         CostInfo = message.CostInfo,
        //         Attachments = message.Attachments,
        //         Timestamp = message.Timestamp,
        //         DurationMs = message.DurationMs
        //     };
        // }
    }
}