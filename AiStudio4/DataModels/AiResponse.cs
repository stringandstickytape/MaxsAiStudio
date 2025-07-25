
using SharedClasses.Providers;

namespace AiStudio4.DataModels
{
    public class AiResponse
    {

        public List<ContentBlock> ContentBlocks { get; set; } = new() ;
        public bool Success { get; set; }

        public TokenUsage TokenUsage { get; set; }
        public string? SuggestedNextPrompt { get; set; }
        public TimeSpan Duration { get; set; }
        public string ChosenTool { get; set; }

        public TokenCost CostInfo { get; set; }
        public List<Attachment> Attachments { get; set; }

        public AiResponse()
        {
            TokenUsage = new TokenUsage("", "");
        }

        public ToolResponse ToolResponseSet { get; set; }
        public bool IsCancelled { get; set; } = false; // Flag to indicate if the request was cancelled mid-stream
    }
}
