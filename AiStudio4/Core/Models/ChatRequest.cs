namespace AiStudio4.Core.Models
{
    public class ChatRequest
    {
        public string ClientId { get; set; }
        public string ConversationId { get; set; }
        public string MessageId { get; set; }
        public string ParentMessageId { get; set; }
        public string Message { get; set; }
        public string Model { get; set; }
        public List<MessageHistoryItem> MessageHistory { get; set; } = new List<MessageHistoryItem>();
    }

    public class MessageHistoryItem
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class ChatResponse
    {
        public bool Success { get; set; }
        public string ResponseText { get; set; }
        public string Error { get; set; }
    }
}