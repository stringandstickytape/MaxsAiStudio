// AiStudio4/Services/CostingStrategies/ClaudeCachingTokenCostStrategy.cs


using SharedClasses.Providers;

namespace AiStudio4.Services.CostingStrategies
{
    public class ClaudeCachingTokenCostStrategy : ITokenCostStrategy
    {
        public decimal CalculateCost(TokenUsage usage, Model model)
        {
            if (usage == null || model == null) return 0m;

            decimal inputCost = (usage.InputTokens / 1_000_000m) * model.InputPriceBelowBoundary;
            decimal outputCost = (usage.OutputTokens / 1_000_000m) * model.OutputPriceBelowBoundary;
            decimal cacheCreationCost = (usage.CacheCreationInputTokens / 1_000_000m) * model.InputPriceBelowBoundary * 1.25m;
            decimal cacheReadCost = (usage.CacheReadInputTokens / 1_000_000m) * model.InputPriceBelowBoundary * 0.1m;
            
            return inputCost + outputCost + cacheCreationCost + cacheReadCost;
        }
    }
}
