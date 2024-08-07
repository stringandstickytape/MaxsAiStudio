using System.Text.RegularExpressions;

namespace AiTool3.Conversations
{
    public class ConversationMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        public string? base64type { get; set; }
        public string? base64image { get; set; }
    }
}