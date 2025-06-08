// AiStudio4/Services/CostingStrategies/NoCachingTokenCostStrategy.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.Services.CostingStrategies
{
    public class NoCachingTokenCostStrategy : ITokenCostStrategy
    {
        public decimal CalculateCost(TokenUsage usage, Model model)
        {
            if (usage == null || model == null) return 0m;

            decimal inputCost = (usage.InputTokens / 1_000_000m) * model.InputPriceBelowBoundary;
            decimal outputCost = (usage.OutputTokens / 1_000_000m) * model.OutputPriceBelowBoundary;
            return inputCost + outputCost;
        }
    }
}