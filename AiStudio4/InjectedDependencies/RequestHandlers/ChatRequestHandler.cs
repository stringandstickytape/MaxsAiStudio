// AiStudio4/InjectedDependencies/RequestHandlers/ChatRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.AiServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public ChatRequestHandler(
            ChatManager chatManager,
            IChatService chatService,
            ClientRequestCancellationService cancellationService,
            WebSocketServer webSocketServer,
            IConvStorage convStorage,
            IGeneralSettingsService generalSettingsService)
        {
            _chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _cancellationService = cancellationService ?? throw new ArgumentNullException(nameof(cancellationService));
            _webSocketServer = webSocketServer ?? throw new ArgumentNullException(nameof(webSocketServer));
            _convStorage = convStorage ?? throw new ArgumentNullException(nameof(convStorage));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
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
            "updateMessage"
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
                string content = requestObject["content"]?.ToString();

                if (string.IsNullOrEmpty(convId))
                    return SerializeError("Conversation ID cannot be empty");

                if (string.IsNullOrEmpty(messageId))
                    return SerializeError("Message ID cannot be empty");

                if (content == null) // Allow empty content but not null
                    return SerializeError("Message content cannot be null");

                // Load the conversation
                var conv = await _convStorage.LoadConv(convId);
                if (conv == null)
                    return SerializeError($"Conversation with ID {convId} not found");

                // Find the message in the conversation
                var allMessages = conv.GetAllMessages();
                var messageToUpdate = allMessages.FirstOrDefault(m => m.Id == messageId);

                if (messageToUpdate == null)
                    return SerializeError($"Message with ID {messageId} not found in conversation {convId}");

                // Update the message content
                messageToUpdate.UserMessage = content;

                // Save the updated conversation
                await _convStorage.SaveConv(conv);

                // Notify the client about the update
                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    type = "conv:update",
                    content = new
                    {
                        messageId,
                        content,
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
    }
}