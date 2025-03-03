namespace AiStudio4.Core.Models
{
    public class ConversationUpdateDto
    {
        public string ConversationId { get; set; }
        public string MessageId { get; set; }
        public object Content { get; set; }
        public string ParentId { get; set; }
        public long Timestamp { get; set; }
        public string Source { get; set; }
    }

    public class StreamingUpdateDto
    {
        public string MessageType { get; set; }
        public string Content { get; set; }
    }
}