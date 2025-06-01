using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Services;
using AiStudio4.InjectedDependencies.WebSocketManagement;
using AiStudio4.InjectedDependencies.WebSocket;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.IO;
using AiStudio4.InjectedDependencies;
using AiStudio4.InjectedDependencies.RequestHandlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AiStudio4.Core;
using System;
using System.Threading.Tasks;
using AiStudio4.Services.Interfaces;

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

            // default project path
            string reposPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos", "AiStudio4TestProject");
            Directory.CreateDirectory(reposPath);

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
            services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
            services.AddSingleton<FileSystemChangeHandler>();
            services.AddSingleton<IProjectFileWatcherService, ProjectFileWatcherService>();
            services.AddSingleton<IConvStorage, FileSystemConvStorage>();
            services.AddSingleton<IChatService, DefaultChatService>();
            services.AddSingleton<IWebSocketNotificationService, WebSocketNotificationService>();
            services.AddSingleton<IToolService, ToolService>();
            services.AddSingleton<ISystemPromptService, SystemPromptService>();
            services.AddSingleton<IPinnedCommandService, PinnedCommandService>();
            services.AddSingleton<IUserPromptService, UserPromptService>();
            services.AddSingleton<ClientRequestCancellationService>();
            services.AddSingleton<IMcpService, McpService>(); // Add McpService
            services.AddSingleton<IStatusMessageService, StatusMessageService>();
            services.AddSingleton<Services.Interfaces.INotificationFacade, NotificationFacade>();
            services.AddSingleton<IToolProcessorService, ToolProcessorService>(); // Add ToolProcessorService
            services.AddSingleton<IDialogService, WpfDialogService>(); // Add DialogService
            services.AddSingleton<IBuiltinToolService, BuiltinToolService>(); // Add BuiltinToolService
            services.AddSingleton<ISecondaryAiService, SecondaryAiService>(); // Add SecondaryAiService
            services.AddSingleton<LicenseService>(); // Add InterjectionService
            services.AddSingleton<IInterjectionService, InterjectionService>(); // Add InterjectionService
            services.AddSingleton<IDotNetProjectAnalyzerService, DotNetProjectAnalyzerService>(); // Add DotNetProjectAnalyzerService

            // Register application services
            services.AddSingleton<IConversationArchivingService, ConversationArchivingService>();
            services.AddSingleton<IGeneralSettingsService, GeneralSettingsService>();
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();
            services.AddSingleton<IProjectHistoryService, ProjectHistoryService>();
            services.AddSingleton<IBuiltInToolExtraPropertiesService, BuiltInToolExtraPropertiesService>();
            services.AddSingleton<WebSocketConnectionManager>();
            services.AddSingleton<WebSocketMessageHandler>();
            services.AddSingleton<WebSocketServer>();
            services.AddSingleton<ConvService>();
            services.AddSingleton<ChatProcessingService>();
            services.AddSingleton<MessageHistoryService>();
            services.AddSingleton<ChatManager>();
            
            // Register request handlers
            services.AddSingleton<ClipboardImageRequestHandler>();
            services.AddTransient<IRequestHandler, ToolRequestHandler>();
            services.AddTransient<IRequestHandler, SystemPromptRequestHandler>();
            services.AddTransient<IRequestHandler, UserPromptRequestHandler>();
            services.AddTransient<IRequestHandler, ThemeRequestHandler>();
            services.AddTransient<IRequestHandler, McpRequestHandler>();
            services.AddTransient<IRequestHandler, AppearanceRequestHandler>();
            services.AddTransient<IRequestHandler, PinnedCommandRequestHandler>();
            services.AddTransient<IRequestHandler, ChatRequestHandler>();
            services.AddTransient<IRequestHandler, FileSystemRequestHandler>();
            services.AddTransient<IRequestHandler, ModelRequestHandler>();
            services.AddTransient<IRequestHandler, MiscRequestHandler>();
            services.AddTransient<IRequestHandler, ConfigRequestHandler>();

            // Register request router and broker
            services.AddSingleton<UiRequestRouter>();
            services.AddSingleton<UiRequestBroker>();
            
            services.AddSingleton<FileServer>();
            services.AddSingleton<WebServer>();
            services.AddSingleton<WindowManager>();
            services.AddTransient<WebViewWindow>();

            // Register StartupService directly instead of as a hosted service
            services.AddSingleton<StartupService>();

        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize services directly since we're getting an error with IHost
            var startupService = _serviceProvider.GetRequiredService<StartupService>();
            await startupService.StartAsync(default);

            // Initialize user prompt service (not handled by StartupService)
            var userPromptService = _serviceProvider.GetRequiredService<IUserPromptService>();
            await userPromptService.InitializeAsync();
            
            // Get settings manager
            var generalSettingsService = _serviceProvider.GetRequiredService<IGeneralSettingsService>();
            var appearanceSettingsService = _serviceProvider.GetRequiredService<IAppearanceSettingsService>();
            var projectHistoryService = _serviceProvider.GetRequiredService<IProjectHistoryService>();

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