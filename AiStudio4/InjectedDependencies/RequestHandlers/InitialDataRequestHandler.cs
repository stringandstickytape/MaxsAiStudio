// AiStudio4/InjectedDependencies/RequestHandlers/InitialDataRequestHandler.cs
using AiStudio4.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    public class InitialDataRequestHandler : BaseRequestHandler
    {
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IToolService _toolService;
        private readonly IUserPromptService _userPromptService;
        private readonly IPinnedCommandService _pinnedCommandService;
        private readonly IProjectService _projectService;
        private readonly IMcpService _mcpService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly ILogger<InitialDataRequestHandler> _logger;

        public InitialDataRequestHandler(
            IGeneralSettingsService generalSettingsService,
            IToolService toolService,
            IUserPromptService userPromptService,
            IPinnedCommandService pinnedCommandService,
            IProjectService projectService,
            IMcpService mcpService,
            ISystemPromptService systemPromptService,
            ILogger<InitialDataRequestHandler> logger)
        {
            _generalSettingsService = generalSettingsService;
            _toolService = toolService;
            _userPromptService = userPromptService;
            _pinnedCommandService = pinnedCommandService;
            _projectService = projectService;
            _mcpService = mcpService;
            _systemPromptService = systemPromptService;
            _logger = logger;
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[] { "getInitialData" };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            if (requestType != "getInitialData")
            {
                return SerializeError("Unsupported request type");
            }

            try
            {
                _logger.LogInformation("Starting initial data aggregation for client {ClientId}", clientId);

                var generalSettings = _generalSettingsService.CurrentSettings;

                var initialData = new
                {
                    models = generalSettings.ModelList,
                    providers = generalSettings.ServiceProviders,
                    tools = await _toolService.GetAllToolsAsync(),
                    toolCategories = await _toolService.GetToolCategoriesAsync(),
                    systemPrompts = await _systemPromptService.GetAllSystemPromptsAsync(),
                    userPrompts = await _userPromptService.GetAllUserPromptsAsync(),
                    pinnedCommands = await _pinnedCommandService.GetPinnedCommandsAsync(clientId),
                    projects = await _projectService.GetAllProjectsAsync(),
                    mcpServers = await _mcpService.GetAllServerDefinitionsAsync(),
                    config = new 
                    {
                        defaultModelGuid = generalSettings.DefaultModelGuid,
                        secondaryModelGuid = generalSettings.SecondaryModelGuid,
                        temperature = generalSettings.Temperature,
                        topP = generalSettings.TopP
                    }
                };

                _logger.LogInformation("Successfully aggregated initial data for client {ClientId}", clientId);
                return JsonConvert.SerializeObject(new { success = true, data = initialData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching initial application data for client {ClientId}", clientId);
                return SerializeError($"Failed to get initial data: {ex.Message}");
            }
        }
    }
}