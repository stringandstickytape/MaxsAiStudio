using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using SharedClasses.Providers;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AiStudio4.InjectedDependencies
{
    public class v4BranchedConvMessage
    {
        public v4BranchedConvMessageRole Role { get; set; }

        // Removed Children collection for flat structure

        public string UserMessage { get; set; }
    public string Id { get; set; }

    // Add explicit parent reference
    public string ParentId { get; set; }

    public TokenCost CostInfo { get; set; }
    
    // Add support for multiple attachments
    public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    
    // Timestamp when the message was created (UTC)
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // Duration in milliseconds that the request took to process (0 for user messages)
    public long DurationMs { get; set; } = 0;
    }
}