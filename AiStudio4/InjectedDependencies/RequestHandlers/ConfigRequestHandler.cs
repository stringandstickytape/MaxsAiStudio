// AiStudio4/InjectedDependencies/RequestHandlers/ConfigRequestHandler.cs
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        public ConfigRequestHandler(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getConfig"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getConfig" => HandleGetConfigRequest(),
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
                secondaryModelGuid = secondaryModelGuid ?? ""
            });
        }
    }
}