using AiStudio4.DataModels;

namespace AiStudio4.Core.Models
{
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string ResponseText { get; set; }
        public string Error { get; set; }
        public TokenCost CostInfo { get; set; }
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
        public bool IsCancelled { get; set; } = false; // Flag to indicate if the chat request was cancelled
    }
}
