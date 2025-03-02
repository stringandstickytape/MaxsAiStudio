using Newtonsoft.Json;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConversation
    {
        public string ConversationId { get; set; }
        public List<v4BranchedConversationMessage> MessageHierarchy { get; set; } = new List<v4BranchedConversationMessage>(); // Initialize here
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

        internal v4BranchedConversationMessage AddNewMessage(v4BranchedConversationMessageRole role, string? newMessageId, string? userMessage, string parentMessageId)
        {
            var newMessage = new v4BranchedConversationMessage
            {
                Role = role,
                UserMessage = userMessage ?? string.Empty,
                Children = new List<v4BranchedConversationMessage>(),
                Id = newMessageId
            };

            bool FindAndAddToParent(v4BranchedConversationMessage current, string targetId)
            {
                if (current.Id == targetId)
                {
                    current.Children.Add(newMessage);
                    return true;
                }

                return current.Children.Any(child => FindAndAddToParent(child, targetId));
            }

            if (string.IsNullOrEmpty(parentMessageId) || !MessageHierarchy.Any(message => FindAndAddToParent(message, parentMessageId)))
            {
                MessageHierarchy.Add(new v4BranchedConversationMessage
                {
                    Role = v4BranchedConversationMessageRole.System,
                    UserMessage = parentMessageId,
                    Children = new List<v4BranchedConversationMessage> { newMessage },
                    Id = parentMessageId
                });
            }

            return newMessage;
        }
    }
}