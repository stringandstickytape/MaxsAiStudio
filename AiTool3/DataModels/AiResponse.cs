using AiTool3.Providers;

namespace AiTool3.DataModels
{
    public class AiResponse
    {
        public string ResponseText { get; set; }
        public bool Success { get; set; }

        public TokenUsage TokenUsage { get; set; }
        public string? SuggestedNextPrompt { get; set; }
        public TimeSpan Duration { get; internal set; }

        public AiResponse()
        {
            TokenUsage = new TokenUsage("", "");
        }
    }
}