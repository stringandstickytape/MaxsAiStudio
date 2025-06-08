// AiStudio4/Services/CostingStrategies/OpenAICachingTokenCostStrategy.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.Services.CostingStrategies
{
    public class OpenAICachingTokenCostStrategy : ITokenCostStrategy
    {
        public decimal CalculateCost(TokenUsage usage, Model model)
        {
            if (usage == null || model == null) return 0m;

            decimal inputCost = (usage.InputTokens / 1_000_000m) * model.InputPriceBelowBoundary;
            decimal outputCost = (usage.OutputTokens / 1_000_000m) * model.OutputPriceBelowBoundary;
            decimal cacheCreationCost = (usage.CacheCreationInputTokens / 1_000_000m) * model.InputPriceBelowBoundary * 1.0m;
            decimal cacheReadCost = (usage.CacheReadInputTokens / 1_000_000m) * model.InputPriceBelowBoundary * 0.25m;
            
            return inputCost + outputCost + cacheCreationCost + cacheReadCost;
        }
    }
}