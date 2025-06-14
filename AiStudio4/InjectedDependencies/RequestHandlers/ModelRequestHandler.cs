// AiStudio4/InjectedDependencies/RequestHandlers/ModelRequestHandler.cs



using SharedClasses.Providers;




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
            // Check for modelGuid first (new approach)
            string modelGuid = requestObject["modelGuid"]?.ToString();
            if (!string.IsNullOrEmpty(modelGuid))
            {
                // Verify the GUID exists in the model list
                var model = _generalSettingsService.CurrentSettings.ModelList.FirstOrDefault(m => m.Guid == modelGuid);
                if (model == null) return SerializeError($"Model with GUID {modelGuid} not found");
                
                updateAction(modelGuid);
                return JsonConvert.SerializeObject(new { success = true });
            }
            
            // Fall back to modelName for backward compatibility
            string modelName = requestObject["modelName"]?.ToString();
            if (string.IsNullOrEmpty(modelName)) return SerializeError("Model identifier (GUID or name) cannot be empty");
            
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
            if (string.IsNullOrEmpty(guid))
            {
                guid = requestObject["promptId"]?.ToString();
                if (string.IsNullOrEmpty(guid)) return SerializeError($"{guidName.Replace("Guid", " ID")} cannot be empty");
            }
                
                
            deleteAction(guid);
            return JsonConvert.SerializeObject(new { success = true });
        }
    }
}
