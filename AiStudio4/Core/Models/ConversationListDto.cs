using System;

namespace AiStudio4.Core.Models
{
    public class ConvListDto
    {
        public string ConvId { get; set; }
        public string Summary { get; set; }
        public string LastModified { get; set; }
        public object FlatMessageStructure { get; set; }
    }
}