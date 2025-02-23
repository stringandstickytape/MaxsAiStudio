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

        public ChatManager(SettingsManager settingsManager, WebSocketServer webSocketServer)
        {
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
        }

        public async Task<string> HandleGetAllConversationsRequest(string clientId)
        {
            var conversationsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "conversations");

            Directory.CreateDirectory(conversationsPath);
            var conversations = new List<object>();

            var ctr = 0;

            await Task.Run(async () =>
            {

                int ctr = 0;
    foreach (var file in Directory.GetFiles(conversationsPath, "conv_*.json"))
    {
        try
        {
            var conversation = JsonConvert.DeserializeObject<v4BranchedConversation>(File.ReadAllText(file));
            if (conversation != null && conversation.MessageHierarchy.Any() && conversation.MessageHierarchy.First().UserMessage != null)
            {// clientidclientidclientid
                var tree = BuildCachedConversationTree(conversation);
                            
                await _webSocketServer.SendToClientAsync(clientId,
                    JsonConvert.SerializeObject(new
                    {
                        messageType = "cachedconversation",
                        content = new
                        {
                            convGuid = conversation.ConversationId,
                            summary = conversation.MessageHierarchy.First().Children[0].UserMessage,
                            fileName = $"conv_{conversation.ConversationId}.json",
                            lastModified = File.GetLastWriteTimeUtc(file).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            treeData = tree
                        }
                    }));
                ctr++;
                //if (ctr == 5) break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading conversation: {ex.Message}");
            continue;
        }
    }
});

            return JsonConvert.SerializeObject(new { success = true });
        }
        public async Task<string> HandleChatRequest(string clientId, JObject requestObject)
        {
            var conversationId = (string)requestObject["conversationId"]; // eg conv_1740088070013
            var newUserMessageId = (string)requestObject["newMessageId"];
            var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == (string)requestObject["model"]);
            var userMessage = (string)requestObject["message"];
            var parentMessageId = (string)requestObject["parentMessageId"];


            var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
            var aiService = AiServiceResolver.GetAiService(service.ServiceName, null);

            aiService.StreamingTextReceived += (sender, text) => AiService_StreamingTextReceived(clientId, text);
            aiService.StreamingComplete += (sender, text) => AiService_StreamingCompleted(clientId, text);

            var v4conversation = LoadOrCreateConversation(conversationId);
            var newUserMessage = v4conversation.AddNewMessage(v4BranchedConversationMessageRole.User, newUserMessageId, userMessage, parentMessageId);
            v4conversation.Save();

            // Build the cached conversation tree from the updated branched conversation
            var cachedConversationTree = BuildCachedConversationTree(v4conversation);

            // Send the updated cached conversation to the client for sidebar display
            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new
                {
                    messageType = "cachedconversation",
                    content = new
                    {
                        convGuid = v4conversation.ConversationId,
                        summary = v4conversation.MessageHierarchy.First().Children[0].UserMessage ?? "",
                        fileName = $"conv_{v4conversation.ConversationId}.json",
                        lastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        treeData = cachedConversationTree
                    }
                }));

            // Get message history by traversing up from new message
            var messageHistory = GetMessageHistory(v4conversation, newUserMessageId);
            
            var conversation = new LinearConversation(DateTime.Now)
            {
                systemprompt = "You are a helpful chatbot.",
                messages = messageHistory.Where(x => x.Role != v4BranchedConversationMessageRole.System).Select(msg => new LinearConversationMessage { 
                    role = msg.Role == v4BranchedConversationMessageRole.User ? "user" : 
                          msg.Role == v4BranchedConversationMessageRole.Assistant ? "assistant" : "system",
                    content = msg.UserMessage
                }).ToList()
            };

            var response = await aiService!.FetchResponse(
                service, model, conversation, null!, null!,
                new CancellationToken(false), _settingsManager.CurrentSettings,
                mustNotUseEmbedding: true, toolNames: null, useStreaming: true);

            var newAiReply = v4conversation.AddNewMessage(v4BranchedConversationMessageRole.Assistant, $"msg_{Guid.NewGuid()}", response.ResponseText, newUserMessageId);

            v4conversation.Save();

            await _webSocketServer.SendToClientAsync(clientId,
                JsonConvert.SerializeObject(new
                {
                    messageType = "conversation",
                    content = new
                    {
                        id = newAiReply.Id,
                        content = newAiReply.UserMessage,
                        source = "ai",
                        parentId = newUserMessageId,
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

        private List<v4BranchedConversationMessage> GetMessageHistory(v4BranchedConversation conversation, string messageId)
        {
            var history = new List<v4BranchedConversationMessage>();
            
            // Helper function to find message and build history
            bool FindMessageAndBuildHistory(v4BranchedConversationMessage current, string targetId)
            {
                if (current.Id == targetId)
                {
                    history.Add(current);
                    return true;
                }

                foreach (var child in current.Children)
                {
                    if (FindMessageAndBuildHistory(child, targetId))
                    {
                        history.Add(current);
                        return true;
                    }
                }

                return false;
            }



            // Search through message hierarchy
            foreach (var message in conversation.MessageHierarchy)
            {
                if (FindMessageAndBuildHistory(message, messageId))
                    break;
            }

            // Reverse to get chronological order (root to leaf)
            history.Reverse();
            return history;
        }

        /// <summary>
        /// Recursively builds a tree structure to represent the cached conversation.
        /// Each node contains an id, a truncated text summary and optional children.
        /// </summary>
        /// <param name="conversation">The branched conversation to build the tree from.</param>
        /// <returns>A dynamic object representing the conversation tree.</returns>
        private dynamic BuildCachedConversationTree(v4BranchedConversation conversation)
        {
            if (conversation.MessageHierarchy == null || conversation.MessageHierarchy.Count == 0)
            {
                return null;
            }
            // For simplicity, build the tree from the first root message
            return BuildTreeNode(conversation.MessageHierarchy[0]);
        }

        /// <summary>
        /// Recursively builds a tree node from a v4BranchedConversationMessage.
        /// </summary>
        /// <param name="message">The conversation message.</param>
        /// <returns>A dynamic object with properties id, text, and children.</returns>
        private dynamic BuildTreeNode(v4BranchedConversationMessage message)
        {
            // Truncate the message text to 20 characters for summary display
            var text = message.UserMessage;
            if (text.Length > 20) text = text.Substring(0, 20);

            var node = new
            {
                id = message.Id,
                text = text,
                children = new List<dynamic>()
            };

            foreach (var child in message.Children)
            {
                ((List<dynamic>)node.children).Add(BuildTreeNode(child));
            }
            return node;
        }

        internal async Task<string> HandleConversationMessagesRequest(string clientId, JObject? requestObject)
        {
            var messageId = requestObject["messageId"].ToString();
            var foundConversation = FindConversationByMessageId(messageId);
            
            if (foundConversation != null)
            {
                var messageHistory = GetMessageHistory(foundConversation, messageId);
                var messages = messageHistory.Select(msg => new
                {
                    id = msg.Id,
                    content = msg.UserMessage,
                    source = msg.Role == v4BranchedConversationMessageRole.User ? "user" : 
                            msg.Role == v4BranchedConversationMessageRole.Assistant ? "ai" : "system",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }).ToList();

                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    messageType = "loadConversation",
                    content = new
                    {
                        conversationId = foundConversation.ConversationId,
                        messages = messages
                    }
                }));

                return JsonConvert.SerializeObject(new { success = true, messages = messages, conversationId = foundConversation.ConversationId });
            }
            return JsonConvert.SerializeObject(new { success = false, error = "Message not found" });
        }

        internal async Task<string> HandleCachedConversationRequest(string clientId, JObject? requestObject)
        {

            // Handle tree view loading (existing code)
            var conversationId = requestObject["conversationId"].ToString();
            var branchedConversation = LoadOrCreateConversation(conversationId);

            if (branchedConversation == null)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Conversation not found" });
            }
            var m1 = branchedConversation.MessageHierarchy.First();

            var o = new
            {
                id = m1.Id,
                text = m1.UserMessage,
                children = CreateChildrenArray(m1.Children)
            };

            return JsonConvert.SerializeObject(new
            {
                success = true,
                treeData = o
            });
        }

        // Add this helper method to the class
        private List<object> CreateChildrenArray(List<v4BranchedConversationMessage> children)
        {
            var childrenArray = new List<object>();
            foreach (var child in children)
            {
                var childObject = new
                {
                    id = child.Id,
                    text = child.UserMessage,
                    children = CreateChildrenArray(child.Children)
                };
                childrenArray.Add(childObject);
            }
            return childrenArray;
        }
        private v4BranchedConversation FindConversationByMessageId(string messageId)
        {
            var conversationsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "conversations");

            foreach (var file in Directory.GetFiles(conversationsPath, "*.json"))
            {
                try
                {
                    var conversation = JsonConvert.DeserializeObject<v4BranchedConversation>(File.ReadAllText(file));
                    if (conversation != null)
                    {
                        // Helper function to search for message
                        bool ContainsMessage(v4BranchedConversationMessage message)
                        {
                            if (message.Id == messageId) return true;
                            return message.Children.Any(ContainsMessage);
                        }

                        // Check if conversation contains the message
                        if (conversation.MessageHierarchy.Any(ContainsMessage))
                        {
                            return conversation;
                        }
                    }
                }
                catch
                {
                    continue; // Skip invalid files
                }
            }
            return null;
        }
    }



}