// AiStudio4/InjectedDependencies/RequestHandlers/ConfigRequestHandler.cs








namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles configuration-related requests
    /// </summary>
    public class ConfigRequestHandler : BaseRequestHandler
    {
        private readonly IGeneralSettingsService _generalSettingsService;

        public ConfigRequestHandler(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getConfig",
            "setTemperature",
            "setTopP" // Added setTopP
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getConfig" => HandleGetConfigRequest(),
                    "setTemperature" => await HandleSetTemperatureRequest(requestObject), 
                    "setTopP" => await HandleSetTopPRequest(requestObject), // Added setTopP handler
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
                temperature = _generalSettingsService.CurrentSettings.Temperature,
                topP = _generalSettingsService.CurrentSettings.TopP, // Added TopP
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

        private async Task<string> HandleSetTopPRequest(JObject requestObject)
        {
            try
            {
                float? topP = requestObject["topP"]?.Value<float?>();
                if (topP == null) 
                    return SerializeError("TopP value is required and must be a number.");
                
                // Validate TopP range (e.g., 0.0 to 1.0)
                if (topP < 0.0f || topP > 1.0f) 
                    return SerializeError("TopP must be between 0.0 and 1.0.");

                _generalSettingsService.UpdateTopP(topP.Value); // Use the new service method
                
                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (JsonException jsonEx)
            {
                return SerializeError($"Invalid TopP format: {jsonEx.Message}");
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting TopP: {ex.Message}");
            }
        }
    }
}
