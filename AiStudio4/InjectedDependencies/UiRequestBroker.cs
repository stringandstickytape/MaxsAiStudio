using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;
        private readonly ChatManager _chatManager;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer, ChatManager chatManager)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
            _chatManager = chatManager;
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            var requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

           switch (requestType)
            {
                case "getAllHistoricalConversationTrees":
                    try
                    {
                        return await _chatManager.HandleGetAllHistoricalConversationTreesRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing getAllHistoricalConversationTrees request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }

                case "conversationmessages":
                    try
                    {
                        return await _chatManager.HandleConversationMessagesRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing conversation messages request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                case "historicalConversationTree":
                    try
                    {
                        return await _chatManager.HandleHistoricalConversationTreeRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing historical conversation tree request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }

                case "chat":
                    try
                    {
                        return await _chatManager.HandleChatRequest(clientId, requestObject);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing chat request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }

                case "getConfig":
                    try
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray()
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($@"Error processing config request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}