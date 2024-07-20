using AiTool3.Providers;

namespace AiTool3
{
    public class AiResponse
    {
        public string ResponseText { get; set; }
        public bool Success { get; set; }

        public TokenUsage TokenUsage { get; set; }
        public string? SuggestedNextPrompt { get; set; }

        public AiResponse()
        {
            TokenUsage = new TokenUsage("","");
        }
    }
}