
using AiStudio4.Core.Models;

namespace AiStudio4.DataModels
{
    public class LinearConvMessage
    {
        public string role { get; set; }
        public List<ContentBlock> contentBlocks { get; set; } = new List<ContentBlock>();

        // Keep for backward compatibility
        public string? base64type { get; set; }
        public string? base64image { get; set; }

        // New multi-attachment support
        public List<Attachment> attachments { get; set; } = new List<Attachment>();

        // For OpenAI function calling compatibility
        public string? function_call { get; set; }
        public string? name { get; set; } // For function result messages
    }
}
