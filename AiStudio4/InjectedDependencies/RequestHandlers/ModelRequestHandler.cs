// AiStudio4/InjectedDependencies/RequestHandlers/ModelRequestHandler.cs
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles model and service provider-related requests
    /// </summary>
    public class ModelRequestHandler : BaseRequestHandler
    {
        private readonly IGeneralSettingsService _generalSettingsService;

        public ModelRequestHandler(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "setDefaultModel",
            "setSecondaryModel",
            "addModel",
            "updateModel",
            "deleteModel",
            "addServiceProvider",
            "updateServiceProvider",
            "deleteServiceProvider"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "setDefaultModel" => await SetModel(_generalSettingsService.UpdateDefaultModel, requestObject),
                    "setSecondaryModel" => await SetModel(_generalSettingsService.UpdateSecondaryModel, requestObject),
                    "addModel" => await AddOrUpdateModel(requestObject, _generalSettingsService.AddModel),
                    "updateModel" => await AddOrUpdateModel(requestObject, _generalSettingsService.UpdateModel, true),
                    "deleteModel" => await DeleteByGuid(_generalSettingsService.DeleteModel, requestObject, "promptId"),
                    "addServiceProvider" => await AddOrUpdateProvider(requestObject, _generalSettingsService.AddServiceProvider),
                    "updateServiceProvider" => await AddOrUpdateProvider(requestObject, _generalSettingsService.UpdateServiceProvider, true),
                    "deleteServiceProvider" => await DeleteByGuid(_generalSettingsService.DeleteServiceProvider, requestObject, "providerGuid"),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

        private async Task<string> SetModel(Action<string> updateAction, JObject requestObject)
        {
            string modelName = requestObject["modelName"]?.ToString();
            if (string.IsNullOrEmpty(modelName)) return SerializeError("Model name cannot be empty");
            updateAction(modelName);
            return JsonConvert.SerializeObject(new { success = true });
        }

        private async Task<string> AddOrUpdateModel(JObject requestObject, Action<Model> action, bool requireGuid = false)
        {
            Model model = requestObject.ToObject<Model>();
            if (model == null || (requireGuid && string.IsNullOrEmpty(model.Guid))) return SerializeError("Invalid model data" + (requireGuid ? " or missing model ID" : ""));
            action(model);
            return JsonConvert.SerializeObject(new { success = true });
        }

        private async Task<string> AddOrUpdateProvider(JObject requestObject, Action<ServiceProvider> action, bool requireGuid = false)
        {
            ServiceProvider provider = requestObject.ToObject<ServiceProvider>();
            if (provider == null || (requireGuid && string.IsNullOrEmpty(provider.Guid))) return SerializeError("Invalid service provider data" + (requireGuid ? " or missing provider ID" : ""));
            action(provider);
            return JsonConvert.SerializeObject(new { success = true });
        }

        private async Task<string> DeleteByGuid(Action<string> deleteAction, JObject requestObject, string guidName)
        {
            string guid = requestObject[guidName]?.ToString();
            if (string.IsNullOrEmpty(guid)) return SerializeError($"{guidName.Replace("Guid", " ID")} cannot be empty");
            deleteAction(guid);
            return JsonConvert.SerializeObject(new { success = true });
        }
    }
}