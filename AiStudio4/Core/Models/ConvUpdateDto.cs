using AiStudio4.DataModels;
using AiStudio4.Core.Models;
using System;
using SharedClasses.Providers;

namespace AiStudio4.Core.Models
{
    public class ConvUpdateDto
    {
        public string ConvId { get; set; }
        public string MessageId { get; set; }
        public string ParentId { get; set; }        

        /// <summary>
        /// The new rich content representation for a message.
        /// </summary>
        public List<ContentBlock> ContentBlocks { get; set; }
        public long Timestamp { get; set; }
        public string Source { get; set; }
        public List<Attachment> Attachments { get; set; }
        public long DurationMs { get; set; }
        public TokenCost CostInfo { get; set; }
        public decimal CumulativeCost { get; set; }
        public TokenUsage TokenUsage { get; set; }
        public float? Temperature { get; set; }
    }
}