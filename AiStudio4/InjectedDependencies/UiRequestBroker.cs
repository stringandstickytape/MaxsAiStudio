using AiTool3.AiServices;
using AiTool3.Conversations;
using AiTool3.DataModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using SharedClasses.Providers;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;
        private readonly ChatManager _chatManager;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer, ChatManager chatManager)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
            _chatManager = chatManager;
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            JObject requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

            try
            {
                return requestType switch
                {
                    "getAllHistoricalConversationTrees" => await _chatManager.HandleGetAllHistoricalConversationTreesRequest(clientId, requestObject),
                    "getModels" => JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList }),
                    "getServiceProviders" => JsonConvert.SerializeObject(new { success = true, providers = _settingsManager.CurrentSettings.ServiceProviders }),
                    "conversationmessages" => await _chatManager.HandleConversationMessagesRequest(clientId, requestObject),
                    "historicalConversationTree" => await _chatManager.HandleHistoricalConversationTreeRequest(clientId, requestObject),
                    "chat" => await _chatManager.HandleChatRequest(clientId, requestObject),
                    "getConfig" => JsonConvert.SerializeObject(new
                    {
                        success = true,
                        models = _settingsManager.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray(),
                        defaultModel = _settingsManager.DefaultSettings?.DefaultModel ?? "",
                        secondaryModel = _settingsManager.DefaultSettings?.SecondaryModel ?? ""
                    }),
                    "setDefaultModel" => await SetModel(_settingsManager.UpdateDefaultModel, requestObject),
                    "setSecondaryModel" => await SetModel(_settingsManager.UpdateSecondaryModel, requestObject),
                    "addModel" => await AddOrUpdateModel(requestObject, _settingsManager.AddModel),
                    "updateModel" => await AddOrUpdateModel(requestObject, _settingsManager.UpdateModel, true),
                    "deleteModel" => await DeleteByGuid(_settingsManager.DeleteModel, requestObject, "modelGuid"),
                    "addServiceProvider" => await AddOrUpdateProvider(requestObject, _settingsManager.AddServiceProvider),
                    "updateServiceProvider" => await AddOrUpdateProvider(requestObject, _settingsManager.UpdateServiceProvider, true),
                    "deleteServiceProvider" => await DeleteByGuid(_settingsManager.DeleteServiceProvider, requestObject, "providerGuid"),
                    _ => throw new NotImplementedException()
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing {requestType} request: {ex.Message}");
                return SerializeError("Error processing request: " + ex.Message);
            }
        }

        private string SerializeError(string message) => JsonConvert.SerializeObject(new { success = false, error = message });

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