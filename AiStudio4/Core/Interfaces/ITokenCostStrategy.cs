// AiStudio4/Core/Interfaces/ITokenCostStrategy.cs
using AiStudio4.Core.Models;
using SharedClasses.Providers;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a strategy that calculates the cost of an AI request
    /// based on token usage and model pricing.
    /// </summary>
    public interface ITokenCostStrategy
    {
        /// <summary>
        /// Calculates the total cost of an AI request.
        /// </summary>
        /// <param name="usage">The token usage data for the request.</param>
        /// <param name="model">The model used, containing pricing information.</param>
        /// <returns>The calculated total cost as a decimal.</returns>
        decimal CalculateCost(TokenUsage usage, Model model);
    }
}