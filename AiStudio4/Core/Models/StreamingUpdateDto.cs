using System.Collections.Generic;

namespace AiStudio4.Core.Models
{

    public class StreamingUpdateDto
    {
        public string MessageId { get; set; } // Add messageId to target specific messages
        public string MessageType { get; set; }
        public string Content { get; set; }
    }
}