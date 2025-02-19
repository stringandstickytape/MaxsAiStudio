using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            switch (requestType)
            {
                case "chat":
                    var model = _settingsManager.CurrentSettings.GetModel();

                    var msg = JsonConvert.DeserializeObject<JObject>(requestData);
                    var userMessage = (string)msg["message"];


                    //var userMessage = $"Write a bullet point list of things, in markdown format, and demarcate it as an md code block.";
                    var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);

                    // instantiate the service from name
                    var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

                    // Use a lambda to capture the clientId
                    aiService.StreamingTextReceived += (sender, text) => AiService_StreamingTextReceived(clientId, text);

                    var conversation = new Conversation(DateTime.Now)
                    {
                        systemprompt = "You are a helpful chatbot.",
                        messages = new List<ConversationMessage> {
                    new ConversationMessage { role = "user", content = userMessage }
                }
                    };

                    var response = await aiService!.FetchResponse(
                        service, model, conversation, null!, null!,
                        new CancellationToken(false), _settingsManager.CurrentSettings,
                        mustNotUseEmbedding: true, toolNames: null, useStreaming: true); // Set useStreaming to true

                    // send a sample conversation to the front-end
                    await _webSocketServer.SendToClientAsync(clientId,
                        JsonConvert.SerializeObject(new
                        {
                            messageType = "conversation",
                            content = new
                            {
                                id = $"msg_{Guid.NewGuid()}",
                                content = "Hello! I'm your AI assistant. How can I help you today?",
                                source = "ai",
                                parentId = (string)null,  // null for root message
                                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                children = new string[] { }
                            }
                        }));

                    return JsonConvert.SerializeObject(response);
                case "getConfig":
                default:
                    return JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray() });
            }
        }

        private async void AiService_StreamingTextReceived(string clientId, string text)
        {
            // Send the streaming text to the specific client
            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new { messageType = "c", content = text }));
        }
    }
}