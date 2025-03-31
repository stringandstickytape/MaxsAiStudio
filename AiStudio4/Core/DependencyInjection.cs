using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools;
using AiStudio4.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiStudio4.Core
{
    /// <summary>
    /// Extension methods for configuring dependency injection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers all tool-related services
        /// </summary>
        public static IServiceCollection AddToolServices(this IServiceCollection services)
        {
            // Register individual tools
            services.AddTransient<CodeDiffTool>();
            services.AddTransient<StopTool>();
            services.AddTransient<RetrieveTextFromUrlTool>();
            services.AddTransient<ReadFilesTool>();
            services.AddTransient<ThinkTool>();
            services.AddTransient<DirectoryTreeTool>();
            services.AddTransient<InfoRequestTool>();
            services.AddTransient<FileSearchTool>();
            services.AddTransient<RunDuckDuckGoSearchTool>();

            // Register the tool service
            services.AddTransient<IBuiltinToolService, BuiltinToolService>();

            return services;
        }
    }
}