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
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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