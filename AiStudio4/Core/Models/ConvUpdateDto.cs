using AiStudio4.DataModels;
using SharedClasses.Providers;

namespace AiStudio4.Core.Models
{
    public class ConvUpdateDto
    {
        public string ConvId { get; set; }
        public string MessageId { get; set; }
        public string ParentId { get; set; }
        public object Content { get; set; } // sometimes this is a c# string, sometimes another type...
        public long Timestamp { get; set; }
        public string Source { get; set; }
        public List<Attachment> Attachments { get; set; }
        public long DurationMs { get; set; }
        public TokenCost CostInfo { get; set; }
        public decimal CumulativeCost { get; set; }
        public TokenUsage TokenUsage { get; set; }
    }
}