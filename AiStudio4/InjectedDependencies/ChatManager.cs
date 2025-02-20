using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class ChatManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;

        // usefulPath = Path.Combine(
        // Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        //        "AiStudio4",
        //        filename);

        public ChatManager(SettingsManager settingsManager, WebSocketServer webSocketServer)
        {
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
        }

        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            var conversationId = (string)requestObject["conversationId"]; // eg conv_1740088070013
            var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == (string)requestObject["model"]);
            var userMessage = (string)requestObject["message"];

            var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
            var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

            aiService.StreamingTextReceived += (sender, text) => AiService_StreamingTextReceived(clientId, text);
            aiService.StreamingComplete += (sender, text) => AiService_StreamingCompleted(clientId, text);

            var v4conversation = LoadOrCreateConversation(conversationId);

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

        private v4BranchedConversation LoadOrCreateConversation(string conversationId)
        {
            string conversationPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "conversations",
                $"{conversationId}.json");

            Directory.CreateDirectory(Path.GetDirectoryName(conversationPath));

            if (File.Exists(conversationPath))
            { 
                var existingConversation = JsonConvert.DeserializeObject<v4BranchedConversation>(File.ReadAllText(conversationPath));
                File.WriteAllText(conversationPath, JsonConvert.SerializeObject(existingConversation));
                return existingConversation;
            }
            else
            {
                var newConversation = new v4BranchedConversation(conversationId)
                {

                };
                File.WriteAllText(conversationPath, JsonConvert.SerializeObject(newConversation));
                return newConversation;
            }
        }
    }

    public class v4BranchedConversation
    { 
        public string ConversationId { get; internal set; }

        public v4BranchedConversation()
        {

        }

        public v4BranchedConversation(string conversationId)
        {
            ConversationId = conversationId;
        }
    }

}