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
            var parentMessageId = (string)requestObject["parentMessageId"];
            var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == (string)requestObject["model"]);
            var userMessage = (string)requestObject["message"];

            var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
            var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

            aiService.StreamingTextReceived += (sender, text) => AiService_StreamingTextReceived(clientId, text);
            aiService.StreamingComplete += (sender, text) => AiService_StreamingCompleted(clientId, text);

            var v4conversation = LoadOrCreateConversation(conversationId);
            var newUserMessage = v4conversation.AddNewMessage(parentMessageId, userMessage);

            v4conversation.Save();
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

            //var newMessage = v4conversation.AddNewMessage(newUserMessage., userMessage);

            v4conversation.Save();

            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new
                {
                    messageType = "conversation",
                    content = new
                    {
                        id = $"msg_{Guid.NewGuid()}",
                        content = response.ResponseText,
                        source = "ai",
                        parentId = "1",
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
                existingConversation.Save();
                return existingConversation;
            }
            else
            {
                var newConversation = new v4BranchedConversation(conversationId)
                {

                };
                newConversation.Save();
                return newConversation;
            }
        }


    }

    public class v4BranchedConversation
    { 
        public string ConversationId { get; private set; }
        public List<v4BranchedConversationMessage> MessageHierarchy { get; private set; }

        public v4BranchedConversation()
        {

        }

        public void Save()
        {
            string conversationPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "conversations",
                $"{ConversationId}.json");

            Directory.CreateDirectory(Path.GetDirectoryName(conversationPath));

            File.WriteAllText(conversationPath, JsonConvert.SerializeObject(this));
        }

        public v4BranchedConversation(string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
                throw new ArgumentNullException(nameof(conversationId));

            ConversationId = conversationId;
            MessageHierarchy = new List<v4BranchedConversationMessage>();
        }



        internal v4BranchedConversationMessage AddNewMessage(string? parentMessageId, string? userMessage)
        {
            // Initialize Messages list if null
            if (MessageHierarchy == null)
            {
                MessageHierarchy = new List<v4BranchedConversationMessage>();
            }

            var newMessage = new v4BranchedConversationMessage
            {
                Role = v4BranchedConversationMessageRole.User,
                UserMessage = userMessage ?? string.Empty,
                Children = new List<v4BranchedConversationMessage>(),
                Id = $"msg_{Guid.NewGuid()}"
            };

            // Helper function to recursively find parent message
            bool FindAndAddToParent(v4BranchedConversationMessage current, string targetId)
            {
                if (current.UserMessage == targetId)
                {
                    current.Children.Add(newMessage);
                    return true;
                }

                foreach (var child in current.Children)
                {
                    if (FindAndAddToParent(child, targetId))
                        return true;
                }

                return false;
            }

            // Try to find parent and add new message as child
            bool foundParent = false;
            foreach (var message in MessageHierarchy)
            {
                if (FindAndAddToParent(message, parentMessageId))
                {
                    foundParent = true;
                    break;
                }
            }

            // If parent not found, create new root message
            if (!foundParent)
            {
                var rootMessage = new v4BranchedConversationMessage
                {
                    Role = v4BranchedConversationMessageRole.System,
                    UserMessage = parentMessageId,
                    Children = new List<v4BranchedConversationMessage> { newMessage },
                    Id = $"msg_{Guid.NewGuid()}"
                };
                MessageHierarchy.Add(rootMessage);
            }

            return newMessage;
        }


    }

    public enum v4BranchedConversationMessageRole
    {
        System,
        AI,
        User
    }

    public class v4BranchedConversationMessage
    {
        public v4BranchedConversationMessageRole Role { get; internal set; }
        public List<v4BranchedConversationMessage> Children { get; internal set; }

        public string UserMessage{ get; internal set; }

        public string Id { get; internal set; }
    }

}