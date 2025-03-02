

using AiStudio4.DataModels;

namespace AiStudio4.Conversations
{
    public class LinearConversation
    {
        public List<LinearConversationMessage> messages { get; set; }
        public string systemprompt { get; set; }
        public DateTime ConversationCreationDateTime { get; set; }
        public LinearConversation(DateTime creationDateTime)
        {
            ConversationCreationDateTime = creationDateTime;

        }
        public string SystemPromptWithDateTime()
        {
            return $"{systemprompt}\r\n\r\nThis conversation began at {ConversationCreationDateTime.ToString("yyyy-MM-dd HH:mm:ss")}.";
        }
    }
}