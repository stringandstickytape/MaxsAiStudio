using AiStudio4.DataModels;
using SharedClasses.Providers;
using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    public class ConvUpdateDto
    {
        public string ConvId { get; set; }
        public string MessageId { get; set; }
        public object Content { get; set; }
        public string ParentId { get; set; }
        public long Timestamp { get; set; }
        public string Source { get; set; }
        public TokenUsage TokenUsage { get; set; }
        public TokenCost CostInfo { get; set; }
        public List<Attachment> Attachments { get; set; }
        public long DurationMs { get; set; }
        public bool IsCancelled { get; set; } = false; // Flag to indicate if the AI response was cancelled
    }

    public class StreamingUpdateDto
    {
        public string MessageType { get; set; }
        public string Content { get; set; }
    }
}
