// InjectedDependencies/StartupService.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// A hosted service that initializes services during application startup
    /// </summary>
    public class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartupService> _logger;
        private readonly IProjectFileWatcherService _projectFileWatcherService;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly FileSystemChangeHandler _fileSystemChangeHandler;

        public StartupService(IServiceProvider serviceProvider, ILogger<StartupService> logger,
            IProjectFileWatcherService projectFileWatcherService,
            IGeneralSettingsService generalSettingsService,
            FileSystemChangeHandler fileSystemChangeHandler)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _projectFileWatcherService = projectFileWatcherService ?? throw new ArgumentNullException(nameof(projectFileWatcherService));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _fileSystemChangeHandler = fileSystemChangeHandler ?? throw new ArgumentNullException(nameof(fileSystemChangeHandler));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting initialization of services");

            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    // Initialize all services that need initialization
                    _logger.LogInformation("Initializing SystemPromptService...");
                    var systemPromptService = scope.ServiceProvider.GetRequiredService<ISystemPromptService>();
                    await systemPromptService.InitializeAsync();

                    _logger.LogInformation("Initializing ToolService...");
                    var toolService = scope.ServiceProvider.GetRequiredService<IToolService>();
                    await toolService.InitializeAsync();

                    _logger.LogInformation("Initializing McpService...");
                    var mcpService = scope.ServiceProvider.GetRequiredService<IMcpService>();
                    await mcpService.InitializeAsync();

                    _logger.LogInformation("Service initialization completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during service initialization");
                }
            }

            string projectPath = _generalSettingsService.CurrentSettings.ProjectPath;
            if (!string.IsNullOrEmpty(projectPath))
            {
                _logger.LogInformation("Initializing ProjectFileWatcherService with path: {ProjectPath}", projectPath);
                _projectFileWatcherService.Initialize(projectPath);
                
                // FileSystemChangeHandler is already initialized through DI and will automatically
                // receive events from the ProjectFileWatcherService
                _logger.LogInformation("FileSystemChangeHandler is ready to process file system events");
            }

            return;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping services");
            return Task.CompletedTask;
        }
    }
}