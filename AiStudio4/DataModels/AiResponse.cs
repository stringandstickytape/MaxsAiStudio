using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.DataModels
{
    public class AiResponse
    {
        public string ResponseText { get; set; }
        public bool Success { get; set; }

        public TokenUsage TokenUsage { get; set; }
        public string? SuggestedNextPrompt { get; set; }
        public TimeSpan Duration { get; set; }
        public string ChosenTool { get; set; }
        public TokenCost CostInfo { get; set; }
        public List<Attachment> Attachments { get; set; }
        public List<AiServices.ToolResponse> ToolResponses { get; internal set; }

        public AiResponse()
        {
            TokenUsage = new TokenUsage("", "");
        }
    }
}