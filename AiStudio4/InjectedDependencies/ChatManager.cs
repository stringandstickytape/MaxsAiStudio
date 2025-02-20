using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace AiStudio4.InjectedDependencies
{
    public class ChatManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;

        public ChatManager(SettingsManager settingsManager, WebSocketServer webSocketServer)
        {
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
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
                            parentId = (string)null,
                            timestamp = new DateTimeOffset(conv.Value.LastModified).ToUnixTimeMilliseconds(),
                            children = new string[] { }
                        }
                    }));
            }

            var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == (string)requestObject["model"]);
            var userMessage = (string)requestObject["message"];
            var parentId = $"msg_{Guid.NewGuid()}";

            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new
                {
                    messageType = "conversation",
                    content = new
                    {
                        id = parentId,
                        content = userMessage.Length > 20 ? userMessage.Substring(0, 20) : userMessage,
                        source = "user",
                        parentId = (string)null,
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
                mustNotUseEmbedding: true, toolNames: null, useStreaming: true);

            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new
                {
                    messageType = "conversation",
                    content = new
                    {
                        id = $"msg_{Guid.NewGuid()}",
                        content = response.ResponseText,
                        source = "ai",
                        parentId = parentId,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        children = new string[] { }
                    }
                }));

            return JsonConvert.SerializeObject(response);
        }

        private async void AiService_StreamingCompleted(string clientId, string text)
        {
            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new { messageType = "endstream", content = text }));
        }

        private async void AiService_StreamingTextReceived(string clientId, string text)
        {
            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new { messageType = "cfrag", content = text }));
        }
    }
}