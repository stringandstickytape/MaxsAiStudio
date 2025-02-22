using Newtonsoft.Json;
using System.IO;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConversation
    { 
        public string ConversationId { get; set; }
        public List<v4BranchedConversationMessage> MessageHierarchy { get; set; }

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



        internal v4BranchedConversationMessage AddNewMessage(v4BranchedConversationMessageRole role, string? newMessageId, string? userMessage, string parentMessageId)
        {
            // Initialize Messages list if null
            if (MessageHierarchy == null)
            {
                MessageHierarchy = new List<v4BranchedConversationMessage>();
            }

            var newMessage = new v4BranchedConversationMessage
            {
                Role = role,
                UserMessage = userMessage ?? string.Empty,
                Children = new List<v4BranchedConversationMessage>(),
                Id = newMessageId
            };

            // Helper function to recursively find parent message
            bool FindAndAddToParent(v4BranchedConversationMessage current, string targetId)
            {
                if (current.Id == targetId)
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
                    Id = parentMessageId
                };
                MessageHierarchy.Add(rootMessage);
            }

            return newMessage;
        }


    }



}