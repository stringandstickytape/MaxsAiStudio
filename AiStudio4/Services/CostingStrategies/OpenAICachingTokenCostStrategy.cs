// AiStudio4/Services/CostingStrategies/OpenAICachingTokenCostStrategy.cs


using SharedClasses.Providers;

namespace AiStudio4.Services.CostingStrategies
{
    public class OpenAICachingTokenCostStrategy : ITokenCostStrategy
    {
        public decimal CalculateCost(TokenUsage usage, Model model)
        {
            if (usage == null || model == null) return 0m;

            var fullPriceInputTokens = usage.InputTokens - usage.CacheReadInputTokens;

            decimal inputCost = (fullPriceInputTokens / 1_000_000m) * model.InputPriceBelowBoundary;
            decimal outputCost = (usage.OutputTokens / 1_000_000m) * model.OutputPriceBelowBoundary;
            decimal cacheReadCost = (usage.CacheReadInputTokens / 1_000_000m) * model.InputPriceBelowBoundary * 0.25m;
            
            return inputCost + outputCost + cacheReadCost;
        }
    }
}
