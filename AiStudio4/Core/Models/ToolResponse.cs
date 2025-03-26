using System.Collections.Generic;

namespace AiStudio4.Core.Models
{
    public class ToolResponse
    {
        public List<ToolResponseItem> Tools { get; set; } = new List<ToolResponseItem>();
    }
}