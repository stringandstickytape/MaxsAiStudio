namespace AiTool3.Providers
{
    public class TokenUsage
    {
        private int inputTokens = -1;
        private int outputTokens = -1;

        public int InputTokens { get => inputTokens; set => inputTokens = value; }
        public int OutputTokens { get => outputTokens; set => outputTokens = value; }

        public TokenUsage(string input, string output)
        {
            if (int.TryParse(input, out int inputTokens))
            {
                InputTokens = inputTokens;
            }

            if (int.TryParse(output, out int outputTokens))
            {
                OutputTokens = outputTokens;
            }
        }
    }
}