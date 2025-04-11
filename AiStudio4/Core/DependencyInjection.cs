// AiStudio4.Core\DependencyInjection.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Tools;
using AiStudio4.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace AiStudio4.Core
{
    /// <summary>
    /// Extension methods for configuring dependency injection
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers theme library and tool-related services.
        /// </summary>
        public static IServiceCollection AddAiStudio4Services(this IServiceCollection services)
        {
            // Register ThemeService
            services.AddSingleton<AiStudio4.Core.Interfaces.IThemeService, AiStudio4.Services.ThemeService>();
            // Register controllers (for minimal hosting, this is needed)
            services.AddControllers();
            // Register all tool-related services
            services.AddToolServices();
            return services;
        }

        /// <summary>
        /// Registers all tool-related services using reflection to find ITool implementations.
        /// </summary>
        public static IServiceCollection AddToolServices(this IServiceCollection services)
        {
            // Register individual tools by scanning the assembly for ITool implementations
            var toolInterfaceType = typeof(ITool);
            var assembly = toolInterfaceType.Assembly; // Assuming tools are in the same assembly as ITool

            var toolTypes = assembly.GetTypes()
                .Where(t => toolInterfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var toolType in toolTypes)
            {
                // Register the concrete type itself
                services.AddTransient(toolType);
                // Register the type also as ITool, resolving it from the container
                // This allows injecting IEnumerable<ITool>
                services.AddTransient<ITool>(sp => (ITool)sp.GetRequiredService(toolType));
                // Example registration: services.AddTransient<ITool>(sp => sp.GetRequiredService<CodeDiffTool>());
            }

            // Register the tool service that consumes the collection of tools
            // BuiltinToolService: Manages the available tools and provides a centralized way to access them.
            services.AddTransient<IBuiltinToolService, BuiltinToolService>();

            return services;
        }
    }
}