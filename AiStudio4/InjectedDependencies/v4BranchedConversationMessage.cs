using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using AiStudio4.Core.Models;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConvMessage
    {
        public float? Temperature { get; set; }
        public v4BranchedConvMessageRole Role { get; set; }        // Removed Children collection for flat structure

        /// <summary>
        /// A list of rich content blocks that make up the message.  This supersedes <see cref="UserMessage"/>.
        /// </summary>
        [JsonPropertyName("contentBlocks")]
        public List<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();        
        [Obsolete("Use ContentBlocks instead.")]
        public string UserMessage { get; set; }
        
        public string Id { get; set; }

        // Add explicit parent reference
        public string ParentId { get; set; }

        public TokenCost CostInfo { get; set; }
        
        // Cumulative cost of this message and all its parent messages
        public decimal CumulativeCost { get; set; }

        // Add support for multiple attachments
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();

        // Timestamp when the message was created (UTC)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Duration in milliseconds that the request took to process (0 for user messages)
        public long DurationMs { get; set; } = 0;

        /// <summary>
        /// Creates a shallow clone of the current message.
        /// Note: This performs a shallow copy of the Attachments list.
        /// </summary>
        /// <returns>A new v4BranchedConvMessage instance with the same property values.</returns>
        public v4BranchedConvMessage Clone()
        {
            return new v4BranchedConvMessage
            {
                Id = this.Id,
                Role = this.Role,
                ParentId = this.ParentId,
                CostInfo = this.CostInfo, // Assuming TokenCost is immutable or a struct
                CumulativeCost = this.CumulativeCost,                Attachments = new List<Attachment>(this.Attachments), // Create a new list wrapping the same attachment references
                ContentBlocks = this.ContentBlocks != null ? new List<ContentBlock>(this.ContentBlocks) : new List<ContentBlock>(),
                Timestamp = this.Timestamp,
                DurationMs = this.DurationMs,
                Temperature = this.Temperature
            };
        }
    }
}