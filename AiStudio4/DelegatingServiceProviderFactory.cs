// AiStudio4/DelegatingServiceProviderFactory.cs
using Microsoft.Extensions.DependencyInjection;


namespace AiStudio4
{
    /// <summary>
    /// A service provider factory that delegates to an existing service provider.
    /// This allows us to use the existing service provider with IHost.
    /// </summary>
    public class DelegatingServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        private readonly IServiceProvider _existingServiceProvider;

        public DelegatingServiceProviderFactory(IServiceProvider existingServiceProvider)
        {
            _existingServiceProvider = existingServiceProvider ?? throw new ArgumentNullException(nameof(existingServiceProvider));
        }

        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            // Return the existing service provider instead of building a new one
            return _existingServiceProvider;
        }
    }
}
