using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConversation
    {
        public string ConversationId { get; set; }
        public List<v4BranchedConversationMessage> MessageHierarchy { get; set; } = new List<v4BranchedConversationMessage>();
        public string Summary { get; set; }
        public string SystemPromptId { get; set; }

        public v4BranchedConversation() { }

        public v4BranchedConversation(string conversationId)
        {
            ConversationId = string.IsNullOrWhiteSpace(conversationId) ? throw new ArgumentNullException(nameof(conversationId)) : conversationId;
        }

        public void Save()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "conversations", $"{ConversationId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonConvert.SerializeObject(this));
        }

        internal v4BranchedConversationMessage AddNewMessage(v4BranchedConversationMessageRole role, string newMessageId, string userMessage, string parentMessageId)
        {
            var newMessage = new v4BranchedConversationMessage
            {
                Role = role,
                UserMessage = userMessage ?? string.Empty,
                Children = new List<v4BranchedConversationMessage>(),
                Id = newMessageId,
                ParentId = parentMessageId
            };

            // If no parent is specified or the parent doesn't exist, add as a root message
            if (string.IsNullOrEmpty(parentMessageId) || !AddToParent(newMessage, parentMessageId))
            {
                // If there are no messages yet, create a system message as the root
                if (!MessageHierarchy.Any())
                {
                    var systemRoot = new v4BranchedConversationMessage
                    {
                        Role = v4BranchedConversationMessageRole.System,
                        UserMessage = "Conversation Root",
                        Children = new List<v4BranchedConversationMessage> { newMessage },
                        Id = $"system_{Guid.NewGuid()}"
                    };

                    // Set the parent of the new message to the system root
                    newMessage.ParentId = systemRoot.Id;

                    MessageHierarchy.Add(systemRoot);
                }
                else
                {
                    // Add as child of the first root message
                    var root = MessageHierarchy.First();
                    root.Children.Add(newMessage);
                    newMessage.ParentId = root.Id;
                }
            }

            return newMessage;
        }

        private bool AddToParent(v4BranchedConversationMessage newMessage, string parentId)
        {
            // Helper function to recursively find and add the message to its parent
            bool FindAndAddToParent(List<v4BranchedConversationMessage> messages)
            {
                foreach (var message in messages)
                {
                    if (message.Id == parentId)
                    {
                        message.Children.Add(newMessage);
                        return true;
                    }

                    if (message.Children.Any() && FindAndAddToParent(message.Children))
                    {
                        return true;
                    }
                }

                return false;
            }

            return FindAndAddToParent(MessageHierarchy);
        }

        // Helper method to get all messages in a flat list
        public List<v4BranchedConversationMessage> GetAllMessages()
        {
            var allMessages = new List<v4BranchedConversationMessage>();
            CollectAllMessages(MessageHierarchy, allMessages);
            return allMessages;
        }

        private void CollectAllMessages(IEnumerable<v4BranchedConversationMessage> messages,
            List<v4BranchedConversationMessage> allMessages)
        {
            foreach (var message in messages)
            {
                allMessages.Add(message);
                if (message.Children.Any())
                {
                    CollectAllMessages(message.Children, allMessages);
                }
            }
        }
    }
}