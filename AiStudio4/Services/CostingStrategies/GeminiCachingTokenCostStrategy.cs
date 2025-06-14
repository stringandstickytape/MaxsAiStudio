// AiStudio4/Services/CostingStrategies/GeminiCachingTokenCostStrategy.cs


using SharedClasses.Providers;

namespace AiStudio4.Services.CostingStrategies
{
    public class GeminiCachingTokenCostStrategy : ITokenCostStrategy
    {
        public decimal CalculateCost(TokenUsage usage, Model model)
        {
            if (usage == null || model == null) return 0m;

            // Gemini doesn't tell us  usage.CacheCreationInputTokens

            // Determine which pricing tier to use based on the model's price boundary
            // We'll assume the boundary applies to the sum of all input-related tokens for the request.
            int totalInputTokens = usage.InputTokens + usage.CacheReadInputTokens;
            bool useAboveBoundaryPricing = model.PriceBoundary.HasValue && totalInputTokens > model.PriceBoundary.Value;
            
            decimal inputPrice = useAboveBoundaryPricing 
                ? model.InputPriceAboveBoundary ?? model.InputPriceBelowBoundary 
                : model.InputPriceBelowBoundary;
                
            decimal outputPrice = useAboveBoundaryPricing 
                ? model.OutputPriceAboveBoundary ?? model.OutputPriceBelowBoundary 
                : model.OutputPriceBelowBoundary;
            
            // For Gemini, cache read/create is just part of the input cost without special multipliers.
            decimal inputCost = ((usage.InputTokens-usage.CacheReadInputTokens) / 1_000_000m) * inputPrice;
            decimal outputCost = (usage.OutputTokens / 1_000_000m) * outputPrice;
            decimal cacheReadCost = (usage.CacheReadInputTokens / 1_000_000m) * inputPrice * 0.25m; // Gemini has 0.25x read cost

            return inputCost + outputCost + cacheReadCost;
        }
    }
}
