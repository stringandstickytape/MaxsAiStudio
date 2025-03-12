using SharedClasses.Providers;
using System;

namespace AiStudio4.Core.Models
{
    public class TokenCost
    {
        // Cost per 1M tokens in USD
        public decimal InputCostPer1M { get; set; }
        public decimal OutputCostPer1M { get; set; }

        // Total cost calculated for this request
        public decimal TotalCost { get; set; }

        // Reference to the token usage for convenience
        public TokenUsage TokenUsage { get; set; }

        // Model GUID used to process this request
        public string ModelGuid { get; set; }

        public TokenCost()
        {
            TokenUsage = new TokenUsage("", "");
        }

        public TokenCost(TokenUsage tokenUsage, decimal inputCostPer1M = 0, decimal outputCostPer1M = 0)
        {
            TokenUsage = tokenUsage;
            InputCostPer1M = inputCostPer1M;
            OutputCostPer1M = outputCostPer1M;
            CalculateTotalCost();
        }

        public TokenCost(TokenUsage tokenUsage, Model model)
        {
            TokenUsage = tokenUsage;
            InputCostPer1M = model?.input1MTokenPrice ?? 0m;
            OutputCostPer1M = model?.output1MTokenPrice ?? 0m;
            ModelGuid = model?.Guid ?? string.Empty;
            CalculateTotalCost();
        }



        public void CalculateTotalCost()
        {
            if (TokenUsage == null) return;

            // Calculate costs: (tokens / 1M) * cost per 1M tokens
            decimal inputCost = (TokenUsage.InputTokens / 1_000_000m) * InputCostPer1M;
            decimal outputCost = (TokenUsage.OutputTokens / 1_000_000m) * OutputCostPer1M;
            decimal inCreate = (TokenUsage.CacheCreationInputTokens / 1_000_000m) * InputCostPer1M * 1.25m;
            decimal inRead = (TokenUsage.CacheReadInputTokens / 1_000_000m) * InputCostPer1M * 0.1m;

            TotalCost = inputCost + outputCost;
        }
    }
}