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

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
        }

        private JArray BuildMessageTree(CompletionMessage message, List<CompletionMessage> allMessages)
        {
            var text = message.Content;

            if (text.Length > 20) text = text.Substring(0, 20);
            var messageObj = new JObject
            {
                { "id", message.Guid },
                { "text", text },
                { "children", new JArray() }
            };

            // Find and add all child messages recursively
            var childMessages = allMessages.Where(m => m.Parent == message.Guid);
            if (childMessages.Any())
            {
                var childrenArray = (JArray)messageObj["children"];
                foreach (var childMessage in childMessages)
                {
                    childrenArray.Add(BuildMessageTree(childMessage, allMessages)[0]);
                }
            }

            return new JArray { messageObj };
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            switch (requestType)
            {
                case "cachedconversation":
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Received cached conversation request: {requestData}");

                        var requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

                        // Load the conversation
                        var conversationId = requestObject["conversationId"].ToString();
                        var branchedConversation = BranchedConversation.LoadConversation(conversationId);

                        if (branchedConversation == null)
                        {
                            return JsonConvert.SerializeObject(new { success = false, error = "Conversation not found" });
                        }
                        var m1 = branchedConversation.Messages[1];
                        var o = BuildMessageTree(m1, branchedConversation.Messages);

                        //var t = JsonConvert.SerializeObject(o);
                        // Return the conversation data
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            treeData = o
                        });
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


                    var conversationCacheManager = new ConversationCacheManager();

                    foreach (var conv in conversationCacheManager.Conversations)
                    {
                        await _webSocketServer.SendToClientAsync(clientId,
                            JsonConvert.SerializeObject(new
                            {
                                messageType = "cachedconversation",
                                content = new
                                {
                                    id = conv.Value.ConvGuid,
                                    content = conv.Value.Summary.Length > 20 ? conv.Value.Summary.Substring(0,20) : conv.Value.Summary,
                                    source = "ai",
                                    parentId = (string)null,  // null for root message
                                    timestamp = new DateTimeOffset(conv.Value.LastModified).ToUnixTimeMilliseconds(),
                                    children = new string[] { }
                                }
                            }));
                    }




                    var model = _settingsManager.CurrentSettings.GetModel();

                    var msg = JsonConvert.DeserializeObject<JObject>(requestData);
                    var userMessage = (string)msg["message"];

                    var parentId = $"msg_{Guid.NewGuid()}";

                    // send a sample conversation to the front-end
                    await _webSocketServer.SendToClientAsync(clientId,
                        JsonConvert.SerializeObject(new
                        {
                            messageType = "conversation",
                            content = new
                            {
                                id = parentId,
                                content = userMessage.Length > 20 ? userMessage.Substring(0, 20) : userMessage,
                                source = "user",
                                parentId = (string)null,  // null for root message
                                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                children = new string[] { }
                            }
                        }));

                    var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);

                    var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

                    aiService.StreamingTextReceived += (sender, text) => AiService_StreamingTextReceived(clientId, text);
                    aiService.StreamingComplete += (sender, text) => AiService_StreamingCompleted(clientId, text);

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


                    await _webSocketServer.SendToClientAsync(clientId,
                        JsonConvert.SerializeObject(new
                        {
                            messageType = "conversation",
                            content = new
                            {
                                id = $"msg_{Guid.NewGuid()}",
                                content = response.ResponseText,
                                source = "ai",
                                parentId = parentId,  // null for root message
                                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                                children = new string[] { }
                            }
                        }));

                    


                    return JsonConvert.SerializeObject(response);
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