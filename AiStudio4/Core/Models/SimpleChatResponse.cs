

namespace AiStudio4.Core.Models
{
    public class SimpleChatResponse
    {
        public bool Success { get; set; }
        public List<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();
        public string Error { get; set; }
        public TimeSpan ProcessingTime { get; internal set; }
    }
}
