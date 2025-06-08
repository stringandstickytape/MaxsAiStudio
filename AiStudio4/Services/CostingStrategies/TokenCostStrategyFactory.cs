// AiStudio4/Services/CostingStrategies/TokenCostStrategyFactory.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using SharedClasses.Providers;
using System;

namespace AiStudio4.Services.CostingStrategies
{
    public interface ITokenCostStrategyFactory
    {
        ITokenCostStrategy GetStrategy(ChargingStrategyType strategyType);
    }

    public class TokenCostStrategyFactory : ITokenCostStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public TokenCostStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ITokenCostStrategy GetStrategy(ChargingStrategyType strategyType)
        {
            return strategyType switch
            {
                ChargingStrategyType.Claude => _serviceProvider.GetRequiredService<ClaudeCachingTokenCostStrategy>(),
                ChargingStrategyType.OpenAI => _serviceProvider.GetRequiredService<OpenAICachingTokenCostStrategy>(),
                ChargingStrategyType.Gemini => _serviceProvider.GetRequiredService<GeminiCachingTokenCostStrategy>(),
                ChargingStrategyType.NoCaching => _serviceProvider.GetRequiredService<NoCachingTokenCostStrategy>(),
                _ => _serviceProvider.GetRequiredService<NoCachingTokenCostStrategy>(),
            };
        }
    }
}