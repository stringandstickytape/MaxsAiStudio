// InjectedDependencies/StartupService.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
        private readonly ISystemPromptService _systemPromptService;
        private readonly AiStudio4.Services.LogService _logService;

        public StartupService(IServiceProvider serviceProvider, ILogger<StartupService> logger,
            IProjectFileWatcherService projectFileWatcherService,
            IGeneralSettingsService generalSettingsService,
            FileSystemChangeHandler fileSystemChangeHandler,
            IConversationArchivingService archivingService,
            IGitHubReleaseService gitHubReleaseService, // Added
            IUpdateNotificationService updateNotificationService, // Added
            ISystemPromptService systemPromptService,
            AiStudio4.Services.LogService logService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _projectFileWatcherService = projectFileWatcherService ?? throw new ArgumentNullException(nameof(projectFileWatcherService));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _fileSystemChangeHandler = fileSystemChangeHandler ?? throw new ArgumentNullException(nameof(fileSystemChangeHandler));
            _archivingService = archivingService ?? throw new ArgumentNullException(nameof(archivingService));
            _gitHubReleaseService = gitHubReleaseService ?? throw new ArgumentNullException(nameof(gitHubReleaseService)); // Added
            _updateNotificationService = updateNotificationService ?? throw new ArgumentNullException(nameof(updateNotificationService)); // Added
            _systemPromptService = systemPromptService ?? throw new ArgumentNullException(nameof(systemPromptService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting initialization of services");

            // Cleanup old logs at startup
            try
            {
                await _logService.CleanupOldLogsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Log cleanup failed on startup. This is non-critical.");
            }

            // Wiki System Prompt Sync
            await SyncWikiSystemPromptAsync();

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

        private async Task SyncWikiSystemPromptAsync()
        {
            try
            {
                var settings = _generalSettingsService.CurrentSettings;
                
                // Check if wiki sync is enabled
                if (!settings.EnableWikiSystemPromptSync)
                {
                    _logger.LogInformation("Wiki system prompt sync is disabled. Skipping sync.");
                    return;
                }
                
                // Validate required settings
                if (string.IsNullOrEmpty(settings.WikiSyncAdoOrganization) ||
                    string.IsNullOrEmpty(settings.WikiSyncAdoProject) ||
                    string.IsNullOrEmpty(settings.WikiSyncWikiIdentifier) ||
                    string.IsNullOrEmpty(settings.WikiSyncPagePath) ||
                    string.IsNullOrEmpty(settings.WikiSyncTargetSystemPromptGuid))
                {
                    _logger.LogWarning("Wiki sync is enabled but required configuration settings are missing. Skipping sync.");
                    return;
                }
                
                // Get Azure DevOps PAT
                string pat = _generalSettingsService.GetDecryptedAzureDevOpsPAT();
                if (string.IsNullOrEmpty(pat))
                {
                    _logger.LogWarning("Azure DevOps PAT is not configured. Skipping wiki sync.");
                    return;
                }
                
                _logger.LogInformation("Starting wiki system prompt sync from Azure DevOps...");
                
                // Construct the Azure DevOps Wiki API URL
                string apiUrl = $"https://dev.azure.com/{settings.WikiSyncAdoOrganization}/{settings.WikiSyncAdoProject}/_apis/wiki/wikis/{settings.WikiSyncWikiIdentifier}/pages/{Uri.EscapeDataString(settings.WikiSyncPagePath)}?api-version=7.1&includeContent=true";

                using (var httpClient = new HttpClient())
                {
                    // Set up authentication
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($":{pat}")));
                    
                    // Make the API request
                    var response = await httpClient.GetAsync(apiUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Failed to fetch wiki page content. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                                       response.StatusCode, response.ReasonPhrase);
                        return;
                    }
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var wikiResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseContent);
                    
                    string wikiContent = wikiResponse?.content;
                    if (string.IsNullOrEmpty(wikiContent))
                    {
                        _logger.LogWarning("Wiki page content is empty or could not be parsed.");
                        return;
                    }
                    
                    // Get or create the target system prompt
                    var targetPrompt = await _systemPromptService.GetSystemPromptByIdAsync(settings.WikiSyncTargetSystemPromptGuid);
                    
                    if (targetPrompt != null)
                    {
                        // Update existing prompt if content has changed
                        if (targetPrompt.Content != wikiContent)
                        {
                            targetPrompt.Content = wikiContent;
                            targetPrompt.ModifiedDate = DateTime.UtcNow;
                            await _systemPromptService.UpdateSystemPromptAsync(targetPrompt);
                            _logger.LogInformation("Updated existing system prompt '{Title}' with new wiki content.", targetPrompt.Title);
                        }
                        else
                        {
                            _logger.LogInformation("System prompt '{Title}' content is already up to date.", targetPrompt.Title);
                        }
                    }
                    else
                    {
                        // Create new system prompt
                        var newPrompt = new AiStudio4.Core.Models.SystemPrompt
                        {
                            Guid = settings.WikiSyncTargetSystemPromptGuid,
                            Content = wikiContent,
                            Title = $"Synced from Wiki: {settings.WikiSyncPagePath}",
                            Description = $"Auto-synced from Azure DevOps Wiki page: {settings.WikiSyncPagePath}",
                            IsDefault = false,
                            Tags = new List<string> { "wiki-sync", "auto-generated" },
                            AssociatedTools = new List<string>(),
                            PrimaryModelGuid = string.Empty,
                            SecondaryModelGuid = string.Empty,
                            CreatedDate = DateTime.UtcNow,
                            ModifiedDate = DateTime.UtcNow
                        };
                        
                        await _systemPromptService.CreateSystemPromptAsync(newPrompt);
                        _logger.LogInformation("Created new system prompt '{Title}' with wiki content.", newPrompt.Title);
                    }
                }
                
                _logger.LogInformation("Wiki system prompt sync completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during wiki system prompt sync. Application startup will continue.");
                // Don't re-throw; allow app to continue starting
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping services");
            return Task.CompletedTask;
        }
    }
}