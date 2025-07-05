// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;



using AiStudio4.Services;
using AiStudio4.Services.Interfaces;
using AiStudio4.Services.Logging;
using AiStudio4.Dialogs;
using AiStudio4.InjectedDependencies.WebSocketManagement;
using AiStudio4.InjectedDependencies.WebSocket;
using Microsoft.Extensions.Hosting;



using AiStudio4.InjectedDependencies.RequestHandlers;

using Newtonsoft.Json.Serialization;
using AiStudio4.Core.Models;
using AiStudio4.Core;


using AiStudio4.Services.Interfaces;
using System.Net.Http; 
namespace AiStudio4
{
    public partial class App : Application
    {
        public const decimal VersionNumber = 1.06m;

        private ServiceProvider _serviceProvider;
        public ServiceProvider Services => _serviceProvider;

        public T GetRequiredService<T>() => _serviceProvider.GetRequiredService<T>();

        public App()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
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
                builder.AddDebug();                // Capture all log messages; UI will filter as needed
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);                // Register in-memory logger provider so logs are routed to the in-app viewer
                builder.Services.AddSingleton<ILoggerProvider, InMemoryLoggerProvider>();
            });

            // Configure configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Register configuration
            services.AddSingleton<IConfiguration>(configuration);

            services.AddToolServices();            // Register core services
            services.AddSingleton<HttpClient>(); // Added for GitHubReleaseService
            services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
            services.AddSingleton<ILlamaServerService, LlamaServerService>(); // Add LlamaServerService

            services.AddSingleton<FileSystemChangeHandler>();
            services.AddSingleton<IProjectFileWatcherService, ProjectFileWatcherService>();
            services.AddSingleton<IConvStorage, FileSystemConvStorage>();
            services.AddSingleton<IChatService, DefaultChatService>();
            services.AddSingleton<IWebSocketNotificationService, WebSocketNotificationService>();
            services.AddSingleton<IToolService, ToolService>();
            services.AddSingleton<ISystemPromptService, SystemPromptService>();
            services.AddSingleton<IProjectService, ProjectService>();
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
            services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>(); // Added for GitHubReleaseService
            services.AddSingleton<IUpdateNotificationService, UpdateNotificationService>(); // Added for update notifications

            // Register application services
            services.AddSingleton<IConversationArchivingService, ConversationArchivingService>();
            services.AddSingleton<IGeneralSettingsService, GeneralSettingsService>();
            services.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>();
            services.AddSingleton<IBuiltInToolExtraPropertiesService, BuiltInToolExtraPropertiesService>();
            services.AddSingleton<WebSocketConnectionManager>();
            services.AddSingleton<WebSocketMessageHandler>();
            services.AddSingleton<WebSocketServer>();
            services.AddSingleton<ConvService>();
            services.AddSingleton<ChatProcessingService>();
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
            services.AddTransient<IRequestHandler, ProjectRequestHandler>();

            // Register request router and broker
            services.AddSingleton<UiRequestRouter>();
            services.AddSingleton<UiRequestBroker>();
            
            services.AddSingleton<FileServer>();
            services.AddSingleton<WebServer>();            services.AddSingleton<WindowManager>();

            // ----------------------------------------------------------
            //  Log Viewer registrations
            // ----------------------------------------------------------
            services.AddSingleton<ILogViewerService, LogViewerService>();
            services.AddTransient<LogViewerViewModel>();
            services.AddTransient<LogViewerWindow>();
            services.AddTransient<WebViewWindow>();

            // Register StartupService directly instead of as a hosted service
            services.AddSingleton<StartupService>();

        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);            // Check for testing profile parameter
            if (e.Args.Contains("--testing-profile"))
            {
                PathHelper.IsTestingProfile = true;
                
                // Empty the testing profile folder
                string testingProfilePath = PathHelper.ProfileRootPath;
                if (Directory.Exists(testingProfilePath))
                {
                    try
                    {
                        Directory.Delete(testingProfilePath, true);
                    }
                    catch (Exception ex)
                    {
                        // If deletion fails, log the error but continue startup
                        System.Diagnostics.Debug.WriteLine($"Failed to delete testing profile folder: {ex.Message}");
                    }
                }
            }

            // Initialize services directly since we're getting an error with IHost
            var startupService = _serviceProvider.GetRequiredService<StartupService>();
            await startupService.StartAsync(default);

            // Initialize user prompt service (not handled by StartupService)
            var userPromptService = _serviceProvider.GetRequiredService<IUserPromptService>();
            await userPromptService.InitializeAsync();
            
            // Initialize project service
            var projectService = _serviceProvider.GetRequiredService<IProjectService>();
            await projectService.InitializeAsync();
            
            // Get settings manager
            var generalSettingsService = _serviceProvider.GetRequiredService<IGeneralSettingsService>();
            var appearanceSettingsService = _serviceProvider.GetRequiredService<IAppearanceSettingsService>();

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
