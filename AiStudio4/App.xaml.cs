using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Services;
using AiStudio4.InjectedDependencies.WebSocketManagement;
using AiStudio4.InjectedDependencies.WebSocket;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.IO;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebSocketSharp;
using AiStudio4.Core;

namespace AiStudio4
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        public ServiceProvider Services => _serviceProvider;

        public T GetRequiredService<T>() => _serviceProvider.GetRequiredService<T>();

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
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.None);
            });

            // Configure configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            services.AddToolServices();

            // Register core services
            services.AddSingleton<IConvStorage, FileSystemConvStorage>();
            services.AddSingleton<IChatService, DefaultChatService>();
            services.AddSingleton<IWebSocketNotificationService, WebSocketNotificationService>();
            services.AddSingleton<IToolService, ToolService>();
            services.AddSingleton<ISystemPromptService, SystemPromptService>();
            services.AddSingleton<IPinnedCommandService, PinnedCommandService>();
            services.AddSingleton<IUserPromptService, UserPromptService>();
            services.AddSingleton<ClientRequestCancellationService>();
            services.AddSingleton<IMcpService, McpService>(); // Add McpService
            services.AddSingleton<IToolProcessorService, ToolProcessorService>(); // Add ToolProcessorService
            services.AddSingleton<IBuiltinToolService, BuiltinToolService>(); // Add BuiltinToolService

            // Register application services
            services.AddSingleton<SettingsManager>();
            services.AddSingleton<WebSocketConnectionManager>();
            services.AddSingleton<WebSocketMessageHandler>();
            services.AddSingleton<WebSocketServer>();
            services.AddSingleton<ConvService>();
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

            var mcpService = _serviceProvider.GetRequiredService<IMcpService>();
            await mcpService.InitializeAsync();

            var systemPromptService = _serviceProvider.GetRequiredService<ISystemPromptService>();
            await systemPromptService.InitializeAsync();

            var userPromptService = _serviceProvider.GetRequiredService<IUserPromptService>();
            await userPromptService.InitializeAsync();
            
            // Get settings manager
            var settingsManager = _serviceProvider.GetRequiredService<SettingsManager>();

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
        
    }
}