using AiTool3.ApiManagement;
using AiTool3.Providers;
using Newtonsoft.Json;

namespace AiTool3
{
    internal class ModelUsageManager
    {
        private Model model;


        public TokenUsage TokensUsed { get; set; }

        public ModelUsageManager(Model model)
        {
            this.model = model;

            // if the model has been used before, load the token usage from the file
            if (System.IO.File.Exists($"TokenUsage-{model.ToString()}.json"))
            {
                var json = System.IO.File.ReadAllText($"TokenUsage-{model.ToString()}.json");
                TokensUsed = JsonConvert.DeserializeObject<TokenUsage>(json);
            }
            else
            {
                TokensUsed = new TokenUsage("", "");
            }
        }

        internal void AddTokensAndSave(TokenUsage tokenUsage)
        {
            TokensUsed.InputTokens += tokenUsage.InputTokens;
            TokensUsed.OutputTokens += tokenUsage.OutputTokens;

            var json = JsonConvert.SerializeObject(TokensUsed);
            File.WriteAllText($"TokenUsage-{model.ToString()}.json", json);
        }
    }
}