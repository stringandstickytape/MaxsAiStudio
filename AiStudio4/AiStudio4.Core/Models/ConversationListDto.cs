using System;

namespace AiStudio4.AiStudio4.Core.Models
{
    public class ConversationListDto
    {
        public string ConversationId { get; set; }
        public string Summary { get; set; }
        public string LastModified { get; set; }
        public object TreeData { get; set; }
    }
}