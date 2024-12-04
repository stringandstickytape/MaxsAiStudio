namespace AiTool3.Providers
{
    public class TokenUsage
    {
        private int inputTokens = 0;
        private int outputTokens = 0;
        private int cacheCreationInputTokens = 0;
        private int cacheReadInputTokens = 0;
        private TimeSpan? duration = null;

        public int InputTokens { get => inputTokens; set => inputTokens = value; }
        public int OutputTokens { get => outputTokens; set => outputTokens = value; }
        public int CacheCreationInputTokens { get => cacheCreationInputTokens; set => cacheCreationInputTokens = value; }
        public int CacheReadInputTokens { get => cacheReadInputTokens; set => cacheReadInputTokens = value; }


        public TokenUsage(string input, string output, string cacheCreationInputTokens = "0", string cacheReadInputTokens = "0", TimeSpan? duration = null)
        {
            if (int.TryParse(input, out int inputTokens))
            {
                InputTokens = inputTokens;
            }

            if (int.TryParse(output, out int outputTokens))
            {
                OutputTokens = outputTokens;
            }

            if (int.TryParse(cacheCreationInputTokens, out int cacheCreationInputTokensInt))
            {
                CacheCreationInputTokens = cacheCreationInputTokensInt;
            }

            if (int.TryParse(cacheReadInputTokens, out int cacheReadInputTokensInt))
            {
                CacheReadInputTokens = cacheReadInputTokensInt;
            }

            this.duration = duration;
        }

        public override string ToString()
        {
            return $"{InputTokens} {OutputTokens} {CacheCreationInputTokens} {CacheReadInputTokens} {(duration == null ? "" : duration.ToString())}";
        }
    }
}