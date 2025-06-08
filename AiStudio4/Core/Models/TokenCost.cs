using SharedClasses.Providers;
using System;

using AiStudio4.Core.Interfaces;

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

        [Obsolete("Use the constructor that accepts an ITokenCostStrategy for accurate pricing.")]
        public TokenCost(TokenUsage tokenUsage, decimal inputCostPer1M = 0, decimal outputCostPer1M = 0)
        {
            TokenUsage = tokenUsage;
            InputCostPer1M = inputCostPer1M;
            OutputCostPer1M = outputCostPer1M;
            CalculateTotalCost();
        }

        [Obsolete("Use the constructor that accepts an ITokenCostStrategy for accurate pricing.")]
        public TokenCost(TokenUsage tokenUsage, Model model)
        {
            TokenUsage = tokenUsage;
            InputCostPer1M = model?.input1MTokenPrice ?? 0m;
            OutputCostPer1M = model?.output1MTokenPrice ?? 0m;
            ModelGuid = model?.Guid ?? string.Empty;
            CalculateTotalCost();
        }

        // --- NEW CONSTRUCTOR ---
        public TokenCost(TokenUsage tokenUsage, Model model, ITokenCostStrategy strategy)
        {
            TokenUsage = tokenUsage;
            InputCostPer1M = model?.input1MTokenPrice ?? 0m;
            OutputCostPer1M = model?.output1MTokenPrice ?? 0m;
            ModelGuid = model?.Guid ?? string.Empty;
            TotalCost = strategy.CalculateCost(tokenUsage, model);
        }
        // --- END NEW CONSTRUCTOR ---



        [Obsolete("This method now performs a basic calculation. Use the strategy-based constructor for accuracy.")]
        public void CalculateTotalCost()
        {
            if (TokenUsage == null) return;

            // Calculate costs: (tokens / 1M) * cost per 1M tokens
            decimal inputCost = (TokenUsage.InputTokens / 1_000_000m) * InputCostPer1M;
            decimal outputCost = (TokenUsage.OutputTokens / 1_000_000m) * OutputCostPer1M;
            TotalCost = inputCost + outputCost;
        }
    }
}