// AiStudio4/InjectedDependencies/RequestHandlers/ChatRequestHandler.cs

using AiStudio4.AiServices;







namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles chat-related requests
    /// </summary>
    public class ChatRequestHandler : BaseRequestHandler
    {
        private readonly ChatManager _chatManager;
        private readonly IChatService _chatService;
        private readonly ClientRequestCancellationService _cancellationService;
        private readonly WebSocketServer _webSocketServer;
        private readonly IConvStorage _convStorage;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<ChatRequestHandler> _logger;

        public ChatRequestHandler(
            ChatManager chatManager,
            IChatService chatService,
            ClientRequestCancellationService cancellationService,
            WebSocketServer webSocketServer,
            IConvStorage convStorage,
            IGeneralSettingsService generalSettingsService,
            IWebSocketNotificationService notificationService,
            ILogger<ChatRequestHandler> logger)
        {
            _chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _cancellationService = cancellationService ?? throw new ArgumentNullException(nameof(cancellationService));
            _webSocketServer = webSocketServer ?? throw new ArgumentNullException(nameof(webSocketServer));
            _convStorage = convStorage ?? throw new ArgumentNullException(nameof(convStorage));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getAllHistoricalConvTrees",
            "getModels",
            "getServiceProviders",
            "getAvailableServiceProviders",
            "convmessages",
            "getConv",
            "historicalConvTree",
            "deleteMessageWithDescendants",
            "deleteConv",
            "chat",
            "simpleChat",
            "cancelRequest",
            "updateMessage",
            "regenerateSummary"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getAllHistoricalConvTrees" => await _chatManager.HandleGetAllHistoricalConvTreesRequest(clientId, requestObject),
                    "getModels" => JsonConvert.SerializeObject(new { success = true, models = _generalSettingsService.CurrentSettings.ModelList }),
                    "getServiceProviders" => JsonConvert.SerializeObject(new { success = true, providers = _generalSettingsService.CurrentSettings.ServiceProviders }),
                    "getAvailableServiceProviders" => JsonConvert.SerializeObject(new { success = true, serviceProviders = AiServiceResolver.GetAvailableServiceNames() }),
                    "getConv" => await _chatManager.HandleHistoricalConvTreeRequest(clientId, requestObject),
                    "historicalConvTree" => await _chatManager.HandleHistoricalConvTreeRequest(clientId, requestObject),
                    "deleteMessageWithDescendants" => await _chatManager.HandleDeleteMessageWithDescendantsRequest(clientId, requestObject),
                    "deleteConv" => await _chatManager.HandleDeleteConvRequest(clientId, requestObject),
                    "chat" => await _chatManager.HandleChatRequest(clientId, requestObject),
                    "simpleChat" => await HandleSimpleChatRequest(requestObject),
                    "cancelRequest" => await HandleCancelRequestAsync(clientId, requestObject),
                    "updateMessage" => await HandleUpdateMessageRequest(clientId, requestObject),
                    "regenerateSummary" => await HandleRegenerateSummaryRequest(clientId, requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> HandleSimpleChatRequest(JObject requestObject)
        {
            try
            {
                string chatMessage = requestObject["chat"]?.ToString();
                if (string.IsNullOrEmpty(chatMessage))
                    return SerializeError("Chat message cannot be empty");

                var response = await _chatService.ProcessSimpleChatRequest(chatMessage);

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                return SerializeError($"Error processing simple chat request: {ex.Message}");
            }
        }

        private async Task<string> HandleCancelRequestAsync(string clientId, JObject requestObject)
        {
            try
            {
                bool anyCancelled = _cancellationService.CancelAllRequests(clientId);
                
                // Notify the client about the cancellation
                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    type = "request:cancelled",
                    content = new
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                }));

                return JsonConvert.SerializeObject(new { success = true, cancelled = anyCancelled });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error cancelling requests: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateMessageRequest(string clientId, JObject requestObject)
        {
            try
            {
                string convId = requestObject["convId"]?.ToString();
                string messageId = requestObject["messageId"]?.ToString();
                var contentBlocksToken = requestObject["contentBlocks"];

                if (string.IsNullOrEmpty(convId))
                    return SerializeError("Conversation ID cannot be empty");

                if (string.IsNullOrEmpty(messageId))
                    return SerializeError("Message ID cannot be empty");

                if (contentBlocksToken == null)
                    return SerializeError("Content blocks cannot be null");

                // Parse contentBlocks array
                var contentBlocksList = new List<Core.Models.ContentBlock>();
                if (contentBlocksToken is JArray contentBlocksArray)
                {
                    foreach (var blockToken in contentBlocksArray)
                    {
                        var content = blockToken["content"]?.ToString() ?? "";
                        var contentTypeString = blockToken["contentType"]?.ToString() ?? "text";
                        
                        // Parse contentType string to enum
                        Core.Models.ContentType contentType = Core.Models.ContentType.Text; // Default
                        if (Enum.TryParse<Core.Models.ContentType>(contentTypeString, true, out var parsedContentType))
                        {
                            contentType = parsedContentType;
                        }
                        
                        contentBlocksList.Add(new Core.Models.ContentBlock
                        {
                            Content = content,
                            ContentType = contentType
                        });
                    }
                }
                else
                {
                    return SerializeError("Content blocks must be an array");
                }

                // Load the conversation
                var conv = await _convStorage.LoadConv(convId);
                if (conv == null)
                    return SerializeError($"Conversation with ID {convId} not found");

                // Find the message in the conversation
                var allMessages = conv.GetAllMessages();
                var messageToUpdate = allMessages.FirstOrDefault(m => m.Id == messageId);

                if (messageToUpdate == null)
                    return SerializeError($"Message with ID {messageId} not found in conversation {convId}");

                // Update the message content blocks
                messageToUpdate.ContentBlocks.Clear();
                messageToUpdate.ContentBlocks.AddRange(contentBlocksList);

                // Save the updated conversation
                await _convStorage.SaveConv(conv);

                // Notify the client about the update
                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    type = "conv:upd",
                    content = new
                    {
                        id = messageId,
                        contentBlocks = contentBlocksList.Select(cb => new { content = cb.Content, contentType = cb.ContentType.ToString().ToLowerInvariant() }).ToArray(),
                        convId,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                }));

                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating message: {ex.Message}");
            }
        }

        private async Task<string> HandleRegenerateSummaryRequest(string clientId, JObject requestObject)
        {
            try
            {
                string convId = requestObject["convId"]?.ToString();
                if (string.IsNullOrEmpty(convId))
                    return SerializeError("Conversation ID cannot be empty");

                // Load the conversation
                var conv = await _convStorage.LoadConv(convId);
                if (conv == null)
                    return SerializeError($"Conversation with ID {convId} not found");

                // Check if secondary model is configured
                var secondaryModelName = _generalSettingsService.CurrentSettings.SecondaryModel;
                if (string.IsNullOrEmpty(secondaryModelName))
                    return SerializeError("Secondary model not configured for summary generation");

                var model = _generalSettingsService.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModelName);
                if (model == null)
                    return SerializeError($"Secondary model '{secondaryModelName}' not found in model list");

                try
                {
                    // Get the first user message and AI response for context
                    var userMessage = conv.Messages.FirstOrDefault(m => m.Role == v4BranchedConvMessageRole.User);
                    var aiMessage = conv.Messages.FirstOrDefault(m => m.Role == v4BranchedConvMessageRole.Assistant);

                    if (userMessage == null)
                        return SerializeError("No user message found in conversation");

                    // Extract text content from messages
                    var userMessageText = string.Join("\n\n", userMessage.ContentBlocks
                        .Where(cb => cb.ContentType == ContentType.Text)
                        .Select(cb => cb.Content));

                    var aiMessageText = aiMessage != null 
                        ? string.Join("\n\n", aiMessage.ContentBlocks
                            .Where(cb => cb.ContentType == ContentType.Text)
                            .Select(cb => cb.Content))
                        : "";

                    // Truncate messages for summary generation
                    string userMessageExcerpt = userMessageText.Length > 250 
                        ? userMessageText.Substring(0, 250) 
                        : userMessageText;

                    string aiResponseExcerpt = aiMessageText.Length > 250 
                        ? aiMessageText.Substring(0, 250) 
                        : aiMessageText;

                    // Generate summary prompt
                    var summaryPrompt = $"Generate a concise 6 - 10 word summary of the following content. Produce NO OTHER OUTPUT WHATSOEVER.\n\n```txt\nUser: {userMessageExcerpt}\nAI: {aiResponseExcerpt}\n```\n";

                    // Use the chat service to generate the summary
                    var summaryResponse = await _chatService.ProcessSimpleChatRequest(summaryPrompt);

                    if (!summaryResponse.Success)
                        return SerializeError($"Failed to generate summary: {summaryResponse.Error}");

                    // Clean and truncate the summary
                    var summary = summaryResponse.ResponseText.Trim();
                    if (summary.Length > 100)
                        summary = summary.Substring(0, 97) + "...";

                    // Update the conversation with the new summary
                    conv.Summary = summary;
                    await _convStorage.SaveConv(conv);

                    // Notify all clients about the summary update
                    await _notificationService.NotifyConvList(new ConvListDto
                    {
                        ConvId = conv.ConvId,
                        Summary = summary,
                        LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        FlatMessageStructure = BuildFlatMessageStructure(conv)
                    });

                    _logger.LogInformation("Successfully regenerated summary for conversation {ConvId}", convId);

                    return JsonConvert.SerializeObject(new { success = true, summary = summary });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating summary for conversation {ConvId}", convId);
                    return SerializeError($"Error generating summary: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleRegenerateSummaryRequest");
                return SerializeError($"Error regenerating summary: {ex.Message}");
            }
        }

        private List<object> BuildFlatMessageStructure(v4BranchedConv conv)
        {
            var allMessages = conv.GetAllMessages();

            return allMessages.Select(msg => new {
                id = msg.Id,
                contentBlocks = msg.ContentBlocks,
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
