using AiTool3.DataModels;

namespace AiTool3.Conversations
{
    public class Conversation
    {
        public List<ConversationMessage> messages { get; set; }
        public string systemprompt { get; set; }
        public Conversation()
        {

        }
        public string SystemPromptWithDateTime()
        {
            return $"{systemprompt}\r\n\r\nThe current date and time is {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
        }
    }
}