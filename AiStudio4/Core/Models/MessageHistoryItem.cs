using AiStudio4.DataModels;

namespace AiStudio4.Core.Models
{
    public class MessageHistoryItem
    {
        public string Role { get; set; }
        public List<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}