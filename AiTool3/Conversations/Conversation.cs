using AiTool3.DataModels;

namespace AiTool3.Conversations
{
    public class Conversation
    {
        public List<ConversationMessage> messages { get; set; }
        public string systemprompt { get; set; }
        public DateTime ConversationCreationDateTime { get; set; }
        public Conversation(DateTime creationDateTime)
        {
            ConversationCreationDateTime = creationDateTime;

        }
        public string SystemPromptWithDateTime()
        {
            return $"{systemprompt}\r\n\r\nThis conversation began at {ConversationCreationDateTime.ToString("yyyy-MM-dd HH:mm:ss")}.";
        }
    }
}