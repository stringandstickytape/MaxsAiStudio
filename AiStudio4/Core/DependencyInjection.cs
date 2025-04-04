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
            // CodeDiffTool: Provides the ability to compare two pieces of code and identify differences.
            services.AddTransient<CodeDiffTool>();
            // StopTool: Allows the AI to halt its current task or process.
            services.AddTransient<StopTool>();
            // RetrieveTextFromUrlTool: Enables the AI to fetch and read the text content of a given URL.
            services.AddTransient<RetrieveTextFromUrlTool>();
            // ReadFilesTool: Equips the AI with the capability to read and process content from specified files.
            services.AddTransient<ReadFilesTool>();
            // ThinkTool: Allows the AI to take a step back and think through the current problem or task.
            services.AddTransient<ThinkTool>();
            // DirectoryTreeTool: Provides the AI with the ability to explore and understand the structure of directories.
            services.AddTransient<DirectoryTreeTool>();
            // InfoRequestTool: Enables the AI to request additional information to aid in its task.
            services.AddTransient<InfoRequestTool>();
            // FileSearchTool: Allows the AI to search for specific files based on certain criteria.
            services.AddTransient<FileSearchTool>();
            // ReadDatabaseSchemaTool: Provides functionality to read and understand database schemas.
            services.AddTransient<ReadDatabaseSchemaTool>();
            // RunDuckDuckGoSearchTool: Allows the AI to perform searches using the DuckDuckGo search engine.
            services.AddTransient<RunDuckDuckGoSearchTool>();
            // YouTubeSearchTool: Enables searching YouTube for videos, channels, and playlists.
            services.AddTransient<YouTubeSearchTool>();

            // Register the tool service
            // BuiltinToolService: Manages the available tools and provides a centralized way to access them.
            services.AddTransient<IBuiltinToolService, BuiltinToolService>();

            return services;
        }
    }
}