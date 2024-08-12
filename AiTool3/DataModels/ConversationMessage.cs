namespace AiTool3.DataModels
{
    public class ConversationMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        public string? base64type { get; set; }
        public string? base64image { get; set; }
    }
}