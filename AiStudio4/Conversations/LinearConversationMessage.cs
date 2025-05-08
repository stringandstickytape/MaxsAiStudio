using System.Collections.Generic;

namespace AiStudio4.DataModels
{
    public class LinearConvMessage
    {
        public string role { get; set; }
        public string content { get; set; }

        // Keep for backward compatibility
        public string? base64type { get; set; }
        public string? base64image { get; set; }

        // New multi-attachment support
        public List<Attachment> attachments { get; set; } = new List<Attachment>();
    }
}