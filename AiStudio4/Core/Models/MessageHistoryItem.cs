using AiStudio4.DataModels;

namespace AiStudio4.Core.Models
{
    public class MessageHistoryItem
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}