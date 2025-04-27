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
            return JsonConvert.SerializeObject(new
            {
                success = true,
                models = _generalSettingsService.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray(),
                defaultModel = _generalSettingsService.CurrentSettings.DefaultModel ?? "",
                secondaryModel = _generalSettingsService.CurrentSettings.SecondaryModel ?? ""
            });
        }
    }
}