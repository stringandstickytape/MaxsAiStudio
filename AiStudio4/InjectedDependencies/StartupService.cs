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
        private readonly IConversationArchivingService _archivingService;
        private readonly IGitHubReleaseService _gitHubReleaseService; // Added
        private readonly IUpdateNotificationService _updateNotificationService; // Added

        public StartupService(IServiceProvider serviceProvider, ILogger<StartupService> logger,
            IProjectFileWatcherService projectFileWatcherService,
            IGeneralSettingsService generalSettingsService,
            FileSystemChangeHandler fileSystemChangeHandler,
            IConversationArchivingService archivingService,
            IGitHubReleaseService gitHubReleaseService, // Added
            IUpdateNotificationService updateNotificationService) // Added
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _projectFileWatcherService = projectFileWatcherService ?? throw new ArgumentNullException(nameof(projectFileWatcherService));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _fileSystemChangeHandler = fileSystemChangeHandler ?? throw new ArgumentNullException(nameof(fileSystemChangeHandler));
            _archivingService = archivingService ?? throw new ArgumentNullException(nameof(archivingService));
            _gitHubReleaseService = gitHubReleaseService ?? throw new ArgumentNullException(nameof(gitHubReleaseService)); // Added
            _updateNotificationService = updateNotificationService ?? throw new ArgumentNullException(nameof(updateNotificationService)); // Added
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting initialization of services");

            _logger.LogInformation("Attempting conversation archiving and pruning...");
            try
            {
                await _archivingService.ArchiveAndPruneConversationsAsync();
                _logger.LogInformation("Conversation archiving and pruning completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the conversation archiving and pruning process. Application startup will continue.");
                // Do not re-throw; allow the app to continue starting.
            }

            _logger.LogInformation("Checking for latest GitHub release...");
            try
            {
                string owner = "stringandstickytape";
                string repo = "MaxsAiStudio";
                var updateResult = await _gitHubReleaseService.CheckForUpdatesAsync(owner, repo);
                
                if (updateResult.CheckSuccessful)
                {
                    _updateNotificationService.SetUpdateInfo(updateResult);
                    if (updateResult.IsUpdateAvailable)
                    {
                        _logger.LogInformation("Update available: {Version} at {Url}", updateResult.LatestVersion, updateResult.ReleaseUrl);
                    }
                    else
                    {
                        _logger.LogInformation("Application is up to date.");
                    }
                }
                else
                {
                    _logger.LogWarning("GitHub release check failed: {Error}", updateResult.ErrorMessage);
                }
                
                _logger.LogInformation("GitHub release check completed.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check for GitHub releases during startup. This is non-critical.");
                // Do not re-throw; allow app to continue starting.
            }

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