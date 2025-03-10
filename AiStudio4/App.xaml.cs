using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.IO;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Interfaces;
using AiStudio4.Services;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies.WebSocket;
using AiStudio4.InjectedDependencies.WebSocketManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AiStudio4
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        public ServiceProvider Services => _serviceProvider;

        public App()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                // Other settings like null handling, etc.
            };

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure logging first
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Configure configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            // Register core services
            services.AddSingleton<IConversationStorage, FileSystemConversationStorage>();
            services.AddSingleton<IChatService, OpenAIChatService>();
            services.AddSingleton<IWebSocketNotificationService, WebSocketNotificationService>();
            services.AddSingleton<IToolService, ToolService>();
            services.AddSingleton<ISystemPromptService, SystemPromptService>();
            services.AddSingleton<IPinnedCommandService, PinnedCommandService>();

            // Register application services
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<WebSocketConnectionManager>();
            services.AddSingleton<WebSocketMessageHandler>();
            services.AddSingleton<WebSocketServer>();
            services.AddSingleton<ConversationService>();
            services.AddSingleton<ChatProcessingService>();
            services.AddSingleton<MessageHistoryService>();
            services.AddSingleton<ChatManager>();
            services.AddSingleton<UiRequestBroker>();
            services.AddSingleton<FileServer>();
            services.AddSingleton<WebServer>();
            services.AddSingleton<WindowManager>();
            services.AddTransient<WebViewWindow>();

            // Add hosted service to initialize services during startup
            services.AddHostedService<StartupService>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize services that need initialization
            var toolService = _serviceProvider.GetRequiredService<IToolService>();
            await toolService.InitializeAsync();

            var systemPromptService = _serviceProvider.GetRequiredService<ISystemPromptService>();
            await systemPromptService.InitializeAsync();
            
            // Initialize default model cost configurations if none exist
            var settingsManager = _serviceProvider.GetRequiredService<SettingsManager>();
            if (settingsManager.CurrentSettings.ModelCostConfigs == null || !settingsManager.CurrentSettings.ModelCostConfigs.Any())
            {
                InitializeDefaultModelCosts(settingsManager);
            }

            var webViewWindow = _serviceProvider.GetRequiredService<WebViewWindow>();
            webViewWindow.Show();

            // Start web server
            var webServer = _serviceProvider.GetRequiredService<WebServer>();
            _ = webServer.StartAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }
        
        private void InitializeDefaultModelCosts(SettingsManager settingsManager)
        {
            var defaultCosts = new List<Core.Models.ModelCostConfig>
            {
                // OpenAI models
                new Core.Models.ModelCostConfig { ModelName = "gpt-4", InputCostPer1M = 30.0m, OutputCostPer1M = 60.0m },
                new Core.Models.ModelCostConfig { ModelName = "gpt-4-turbo", InputCostPer1M = 10.0m, OutputCostPer1M = 30.0m },
                new Core.Models.ModelCostConfig { ModelName = "gpt-3.5-turbo", InputCostPer1M = 0.5m, OutputCostPer1M = 1.5m },
                
                // Anthropic models
                new Core.Models.ModelCostConfig { ModelName = "claude-2", InputCostPer1M = 8.0m, OutputCostPer1M = 24.0m },
                new Core.Models.ModelCostConfig { ModelName = "claude-3-opus-20240229", InputCostPer1M = 15.0m, OutputCostPer1M = 75.0m },
                new Core.Models.ModelCostConfig { ModelName = "claude-3-sonnet-20240229", InputCostPer1M = 3.0m, OutputCostPer1M = 15.0m },
                new Core.Models.ModelCostConfig { ModelName = "claude-3-haiku-20240307", InputCostPer1M = 0.25m, OutputCostPer1M = 1.25m },
                
                // Llama models (default to free as they're run locally)
                new Core.Models.ModelCostConfig { ModelName = "llama2", InputCostPer1M = 0m, OutputCostPer1M = 0m },
                new Core.Models.ModelCostConfig { ModelName = "llama3", InputCostPer1M = 0m, OutputCostPer1M = 0m },
                
                // Add other models as needed
            };
            
            settingsManager.CurrentSettings.ModelCostConfigs = defaultCosts;
            settingsManager.SaveSettings();
        }
    }
}