using SharedClasses.Providers;

namespace AiStudio4.DataModels
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