using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

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
                case "getAllConversations":
                    return await _chatManager.HandleGetAllConversationsRequest(clientId);

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
                case "cachedconversation":
                    try
                    {
                        return await _chatManager.HandleCachedConversationRequest(clientId, requestObject);



                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing cached conversation request: {ex.Message}");
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "Error processing request: " + ex.Message
                        });
                    }

                case "chat":

                    return await _chatManager.HandleChatRequest(clientId, requestObject);
                case "getConfig":
                    return JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray() });
                default:
                    throw new NotImplementedException();
            }
        }

        private async void AiService_StreamingCompleted(string clientId, string text)
        {
            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new { messageType = "endstream", content = text }));
        }

        private async void AiService_StreamingTextReceived(string clientId, string text)
        {
            // Send the streaming text to the specific client
            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new { messageType = "cfrag", content = text }));
        }
    }
}