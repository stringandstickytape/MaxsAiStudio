using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using AiStudio4.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AiStudio4.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWebSocketServices(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            services.AddSingleton<IWebSocketNotificationService, WebSocketNotificationService>();
            services.AddSingleton<WebSocketServer>();

            return services;
        }
    }
}