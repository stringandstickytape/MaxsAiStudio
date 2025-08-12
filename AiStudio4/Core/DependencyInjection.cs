// AiStudio4.Core\DependencyInjection.cs


using AiStudio4.Core.Services;
using AiStudio4.Core.Tools;

using AiStudio4.InjectedDependencies.RequestHandlers;
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
        /// Registers tool-related services.
        /// </summary>
        public static IServiceCollection AddAiStudio4Services(this IServiceCollection services)
        {
            // Register controllers (for minimal hosting, this is needed)
            services.AddControllers();
            // Register all tool-related services
            services.AddToolServices();
            // Register the project file watcher service
            return services;
        }

        /// <summary>
        /// Registers all tool-related services using reflection to find ITool implementations.
        /// </summary>
        public static IServiceCollection AddToolServices(this IServiceCollection services)
        {
            // Register theme service
            services.AddTransient<IThemeService, ThemeService>();
            
            // Register adapters for shared library tools
            services.AddTransient<AiStudio4.Tools.Interfaces.IGeneralSettingsService>(sp =>
                new AiStudio4.Services.Adapters.GeneralSettingsServiceAdapter(sp.GetRequiredService<InjectedDependencies.IGeneralSettingsService>()));
            services.AddTransient<AiStudio4.Tools.Interfaces.IStatusMessageService>(sp =>
                new AiStudio4.Services.Adapters.StatusMessageServiceAdapter(sp.GetRequiredService<Core.Interfaces.IStatusMessageService>()));
            services.AddTransient<AiStudio4.Tools.Interfaces.IBuiltInToolExtraPropertiesService>(sp =>
                new AiStudio4.Services.Adapters.BuiltInToolExtraPropertiesServiceAdapter(sp.GetRequiredService<Core.Interfaces.IBuiltInToolExtraPropertiesService>()));
            
            // Register tools from SHARED LIBRARY by scanning the assembly
            var sharedToolInterfaceType = typeof(AiStudio4.Tools.Interfaces.ITool);
            var sharedAssembly = sharedToolInterfaceType.Assembly;
            
            var sharedToolTypes = sharedAssembly.GetTypes()
                .Where(t => sharedToolInterfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            
            foreach (var toolType in sharedToolTypes)
            {
                // Register the concrete type itself
                services.AddTransient(toolType);
                // Register as main app's ITool interface using adapter
                services.AddTransient<ITool>(sp =>
                {
                    var sharedTool = (AiStudio4.Tools.Interfaces.ITool)sp.GetRequiredService(toolType);
                    return new AiStudio4.Services.Adapters.SharedToolAdapter(sharedTool);
                });
            }
            
            // Register tools from MAIN APP by scanning the assembly for ITool implementations
            var toolInterfaceType = typeof(ITool);
            var assembly = toolInterfaceType.Assembly; // Assuming tools are in the same assembly as ITool

            var toolTypes = assembly.GetTypes()
                .Where(t => toolInterfaceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Where(t => t != typeof(AiStudio4.Services.Adapters.SharedToolAdapter)); // Exclude the adapter itself

            foreach (var toolType in toolTypes)
            {
                // Register the concrete type itself
                services.AddTransient(toolType);
                // Register the type also as ITool, resolving it from the container
                // This allows injecting IEnumerable<ITool>
                services.AddTransient<ITool>(sp => (ITool)sp.GetRequiredService(toolType));
                // Example registration: services.AddTransient<ITool>(sp => sp.GetRequiredService<CodeDiffTool>());
            }

            // Register project packager service
            services.AddSingleton<IProjectPackager, ProjectPackager>();

            // --- Register Costing Strategies ---
            services.AddSingleton<AiStudio4.Services.CostingStrategies.NoCachingTokenCostStrategy>();
            services.AddSingleton<AiStudio4.Services.CostingStrategies.ClaudeCachingTokenCostStrategy>();
            services.AddSingleton<AiStudio4.Services.CostingStrategies.OpenAICachingTokenCostStrategy>();
            services.AddSingleton<AiStudio4.Services.CostingStrategies.GeminiCachingTokenCostStrategy>();
            services.AddSingleton<AiStudio4.Services.CostingStrategies.ITokenCostStrategyFactory, AiStudio4.Services.CostingStrategies.TokenCostStrategyFactory>();
            // --- End register costing strategies ---

            // Register the tool service that consumes the collection of tools
            // BuiltinToolService: Manages the available tools and provides a centralized way to access them.
            services.AddTransient<IBuiltinToolService, BuiltinToolService>();

            // Register the new ToolExecutor for provider-managed tool loops
            services.AddScoped<Core.Interfaces.IToolExecutor, ToolExecutor>();

            return services;
        }
    }
}
