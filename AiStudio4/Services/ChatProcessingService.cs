










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
            IStatusMessageService statusMessageService) // Added IServiceProvider
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
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            try
            {
                await _statusMessageService.SendStatusMessageAsync(clientId, "Preparing request...");

                // Get cancellation token from the service
                var cancellationToken = _cancellationService.AddTokenSource(clientId);

                // Define callbacks inline, capturing clientId and messageId (will be set later)
                string assistantMessageId = $"msg_{Guid.NewGuid()}";



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
                        BranchedConv = conv
                    };

                    _logger.Log(LogLevel.Information, $"ChatRequest: model: {chatRequest.Model}, toolct {chatRequest.ToolIds.Count}, attachments {attachments?.Count}, MessageId: {chatRequest.MessageId}, ParentMessageId: {chatRequest.ParentMessageId}");

                    // Check if this is the first non-system message in the conversation
                    isFirstMessageInConv = conv.Messages.Count <= 1 ||
                        (conv.Messages.Count == 2 && conv.Messages.Any(m => m.Role == v4BranchedConvMessageRole.System));                    // Build new content block list for rich message model
                    var userContentBlocks = new List<ContentBlock>
                    {
                        new ContentBlock { Content = chatRequest.Message, ContentType = ContentType.Text }
                    };
                    var newUserMessage = conv.AddOrUpdateMessage(v4BranchedConvMessageRole.User, chatRequest.MessageId, userContentBlocks, chatRequest.ParentMessageId);
                    
                    // Add attachments to the message
                    if (attachments != null && attachments.Any())
                    {
                        newUserMessage.Attachments.AddRange(attachments);
                    }

                    // Create placeholder AI message before starting the AI stream
                    


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
                        ContentBlocks = newUserMessage.ContentBlocks,
                        ParentId = chatRequest.ParentMessageId,
                        Timestamp = new DateTimeOffset(newUserMessage.Timestamp).ToUnixTimeMilliseconds(),
                        Source = "user", // Explicitly set source as "user"
                        Attachments = newUserMessage.Attachments,
                        DurationMs = 0, // User messages have zero processing duration
                    });

                    var placeholderMessage = conv.CreatePlaceholder(assistantMessageId, newUserMessage.Id);

                    // Notify client to create the placeholder AI MessageItem
                    await _notificationService.NotifyConvPlaceholderUpdate(clientId, conv, placeholderMessage);


                    var messagesForClient = BuildFlatMessageStructure(conv);

                    var summaryText = conv.Summary;
                    if(summaryText == null)
                    {
                        var userMessage = string.Join("\n\n", conv.Messages.FirstOrDefault(m => m.Role == v4BranchedConvMessageRole.User)?.ContentBlocks ?? new List<ContentBlock>());

                        if(userMessage == null)
                        {
                            summaryText = "New Conversation";
                        } else
                        {
                            if(userMessage.Length > 100)
                            {
                                summaryText = userMessage.Substring(0, 100) + "...";

                            }
                            else
                            {
                                summaryText = userMessage;
                            }
                        }
                    }

                    await _notificationService.NotifyConvList(new ConvListDto
                    {
                        ConvId = conv.ConvId,
                        Summary = summaryText,
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        FlatMessageStructure = messagesForClient
                    });


                    chatRequest.BranchedConv = conv;

                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    

                    var response = await _chatService.ProcessChatRequest(chatRequest, assistantMessageId, cancellationToken);



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

                                            var lastMessageContent = string.Join("\n\n", conv.Messages.Last().ContentBlocks.Where(x => x.ContentType == ContentType.Text).Select(cb => cb.Content));

                                            string aiResponseExcerpt = lastMessageContent.Length > 250 
                                            ? lastMessageContent.Substring(0, 250) : lastMessageContent;
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
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message, response = ex.Message });
            }
        }

        // Helper method to build flat message structure for client
        private List<object> BuildFlatMessageStructure(v4BranchedConv conv)
        {
            var allMessages = conv.GetAllMessages();

            return allMessages.Select(msg => new {
                id = msg.Id,
                contentBlocks = msg.ContentBlocks,
                //text = (msg.ContentBlocks != null && msg.ContentBlocks.Count > 0) ? string.Join(" ", msg.ContentBlocks.Select(cb => cb.Content)) : (msg.UserMessage ?? "[Empty Message]"),
                parentId = msg.ParentId,
                source = msg.Role == v4BranchedConvMessageRole.User ? "user" :
                        msg.Role == v4BranchedConvMessageRole.Assistant ? "ai" : "system",
                costInfo = msg.CostInfo,
                attachments = msg.Attachments,
                timestamp = msg.Timestamp,
                durationMs = msg.DurationMs,
                temperature = msg.Temperature
            }).ToList<object>();
        }
    }
}
