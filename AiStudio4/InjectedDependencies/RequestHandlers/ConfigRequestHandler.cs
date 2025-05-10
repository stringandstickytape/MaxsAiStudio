// AiStudio4/InjectedDependencies/RequestHandlers/ConfigRequestHandler.cs
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO; // Added for Path operations
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles configuration-related requests
    /// </summary>
    public class ConfigRequestHandler : BaseRequestHandler
    {
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IProjectHistoryService _projectHistoryService; // Added

        public ConfigRequestHandler(IGeneralSettingsService generalSettingsService, IProjectHistoryService projectHistoryService) // Added projectHistoryService
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            _projectHistoryService = projectHistoryService ?? throw new ArgumentNullException(nameof(projectHistoryService)); // Added
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getConfig",
            "setTemperature",
            "projectFolders/getAll" // Added
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getConfig" => HandleGetConfigRequest(),
                    "setTemperature" => await HandleSetTemperatureRequest(requestObject),
                    "projectFolders/getAll" => await HandleGetProjectFoldersRequest(), // Added
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private string HandleGetConfigRequest()
        {
            // Since there's only one user, we don't need complex migration
            // Just use the current settings directly
            
            // Get the model objects for default and secondary models
            var defaultModelGuid = _generalSettingsService.CurrentSettings.DefaultModelGuid;
            var secondaryModelGuid = _generalSettingsService.CurrentSettings.SecondaryModelGuid;
            
            // Return both model names and GUIDs for compatibility
            return JsonConvert.SerializeObject(new
            {
                success = true,
                // Return full model objects instead of just names
                models = _generalSettingsService.CurrentSettings.ModelList.Select(x => new {
                    guid = x.Guid,
                    name = x.ModelName,
                    friendlyName = x.FriendlyName
                }).ToArray(),
                // Return both name and GUID for backward compatibility
                defaultModel = _generalSettingsService.CurrentSettings.DefaultModel ?? "",
                defaultModelGuid = defaultModelGuid ?? "",
                secondaryModel = _generalSettingsService.CurrentSettings.SecondaryModel ?? "",
                secondaryModelGuid = secondaryModelGuid ?? "",
                temperature = _generalSettingsService.CurrentSettings.Temperature // <-- ADD THIS LINE
            });
        }

        private async Task<string> HandleSetTemperatureRequest(JObject requestObject)
        {
            try
            {
                float? temperature = requestObject["temperature"]?.Value<float?>();
                if (temperature == null) 
                    return SerializeError("Temperature value is required and must be a number.");
                
                // Validate temperature range (e.g., 0.0 to 2.0)
                if (temperature < 0.0f || temperature > 2.0f) 
                    return SerializeError("Temperature must be between 0.0 and 2.0.");

                _generalSettingsService.CurrentSettings.Temperature = temperature.Value;
                _generalSettingsService.SaveSettings(); // Persist the change
                
                // Optionally, notify other connected clients if temperature changes should be real-time for all
                // await _webSocketNotificationService.NotifyGeneralSettingsUpdate(_generalSettingsService.CurrentSettings);

                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (JsonException jsonEx)
            {
                return SerializeError($"Invalid temperature format: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting temperature: {ex.Message}");
            }
        }

        private async Task<string> HandleGetProjectFoldersRequest()
        {
            try
            {
                var folders = await _projectHistoryService.GetKnownProjectFoldersAsync();
                // Send only necessary info to client: id, name, and a path snippet
                var clientFolders = folders.Select(f => new 
                {
                    id = f.Id,
                    name = f.Name,
                    pathSnippet = GetPathSnippet(f.Path) // Helper to create a user-friendly snippet
                }).ToList();
                return JsonConvert.SerializeObject(new { success = true, folders = clientFolders });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving project folders: {ex.Message}");
            }
        }

        private string GetPathSnippet(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return string.Empty;
            var parts = fullPath.Split(new[] { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 2) return fullPath;
            return $"...{System.IO.Path.DirectorySeparatorChar}{parts[parts.Length - 2]}{System.IO.Path.DirectorySeparatorChar}{parts[parts.Length - 1]}";
        }
    }
}