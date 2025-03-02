using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using SharedClasses.Providers;
using AiStudio4.Core.Interfaces;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;
        private readonly ChatManager _chatManager;
        private readonly IToolService _toolService;

        public UiRequestBroker(IConfiguration configuration, SettingsManager settingsManager, WebSocketServer webSocketServer, ChatManager chatManager, IToolService toolService)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
            _chatManager = chatManager;
            _toolService = toolService;
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
                    "getTools" => await HandleGetToolsRequest(),
                    "getTool" => await HandleGetToolRequest(requestObject),
                    "addTool" => await HandleAddToolRequest(requestObject),
                    "updateTool" => await HandleUpdateToolRequest(requestObject),
                    "deleteTool" => await HandleDeleteToolRequest(requestObject),
                    "getToolCategories" => await HandleGetToolCategoriesRequest(),
                    "addToolCategory" => await HandleAddToolCategoryRequest(requestObject),
                    "updateToolCategory" => await HandleUpdateToolCategoryRequest(requestObject),
                    "deleteToolCategory" => await HandleDeleteToolCategoryRequest(requestObject),
                    "validateToolSchema" => await HandleValidateToolSchemaRequest(requestObject),
                    "importTools" => await HandleImportToolsRequest(requestObject),
                    "exportTools" => await HandleExportToolsRequest(requestObject),
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

        #region Tool Request Handlers
        private async Task<string> HandleGetToolsRequest()
        {
            try
            {
                var tools = await _toolService.GetAllToolsAsync();
                return JsonConvert.SerializeObject(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving tools: {ex.Message}");
            }
        }

        private async Task<string> HandleGetToolRequest(JObject requestObject)
        {
            try
            {
                string toolId = requestObject["toolId"]?.ToString();
                if (string.IsNullOrEmpty(toolId)) return SerializeError("Tool ID cannot be empty");
                
                var tool = await _toolService.GetToolByIdAsync(toolId);
                if (tool == null) return SerializeError($"Tool with ID {toolId} not found");
                
                return JsonConvert.SerializeObject(new { success = true, tool });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving tool: {ex.Message}");
            }
        }

        private async Task<string> HandleAddToolRequest(JObject requestObject)
        {
            try
            {
                var tool = requestObject.ToObject<Core.Models.Tool>();
                if (tool == null) return SerializeError("Invalid tool data");
                
                var result = await _toolService.AddToolAsync(tool);
                return JsonConvert.SerializeObject(new { success = true, tool = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding tool: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateToolRequest(JObject requestObject)
        {
            try
            {
                var tool = requestObject.ToObject<Core.Models.Tool>();
                if (tool == null || string.IsNullOrEmpty(tool.Guid)) 
                    return SerializeError("Invalid tool data or missing tool ID");
                
                var result = await _toolService.UpdateToolAsync(tool);
                return JsonConvert.SerializeObject(new { success = true, tool = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating tool: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteToolRequest(JObject requestObject)
        {
            try
            {
                string toolId = requestObject["toolId"]?.ToString();
                if (string.IsNullOrEmpty(toolId)) return SerializeError("Tool ID cannot be empty");
                
                var success = await _toolService.DeleteToolAsync(toolId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting tool: {ex.Message}");
            }
        }

        private async Task<string> HandleGetToolCategoriesRequest()
        {
            try
            {
                var categories = await _toolService.GetToolCategoriesAsync();
                return JsonConvert.SerializeObject(new { success = true, categories });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving tool categories: {ex.Message}");
            }
        }

        private async Task<string> HandleAddToolCategoryRequest(JObject requestObject)
        {
            try
            {
                var category = requestObject.ToObject<Core.Models.ToolCategory>();
                if (category == null) return SerializeError("Invalid category data");
                
                var result = await _toolService.AddToolCategoryAsync(category);
                return JsonConvert.SerializeObject(new { success = true, category = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding tool category: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateToolCategoryRequest(JObject requestObject)
        {
            try
            {
                var category = requestObject.ToObject<Core.Models.ToolCategory>();
                if (category == null || string.IsNullOrEmpty(category.Id)) 
                    return SerializeError("Invalid category data or missing category ID");
                
                var result = await _toolService.UpdateToolCategoryAsync(category);
                return JsonConvert.SerializeObject(new { success = true, category = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating tool category: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteToolCategoryRequest(JObject requestObject)
        {
            try
            {
                string categoryId = requestObject["categoryId"]?.ToString();
                if (string.IsNullOrEmpty(categoryId)) return SerializeError("Category ID cannot be empty");
                
                var success = await _toolService.DeleteToolCategoryAsync(categoryId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting tool category: {ex.Message}");
            }
        }

        private async Task<string> HandleValidateToolSchemaRequest(JObject requestObject)
        {
            try
            {
                string schema = requestObject["schema"]?.ToString();
                if (string.IsNullOrEmpty(schema)) return SerializeError("Schema cannot be empty");
                
                var isValid = await _toolService.ValidateToolSchemaAsync(schema);
                return JsonConvert.SerializeObject(new { success = true, isValid });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error validating tool schema: {ex.Message}");
            }
        }

        private async Task<string> HandleImportToolsRequest(JObject requestObject)
        {
            try
            {
                string json = requestObject["json"]?.ToString();
                if (string.IsNullOrEmpty(json)) return SerializeError("Import data cannot be empty");
                
                var tools = await _toolService.ImportToolsAsync(json);
                return JsonConvert.SerializeObject(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error importing tools: {ex.Message}");
            }
        }

        private async Task<string> HandleExportToolsRequest(JObject requestObject)
        {
            try
            {
                var toolIds = requestObject["toolIds"]?.ToObject<List<string>>();
                var json = await _toolService.ExportToolsAsync(toolIds);
                return JsonConvert.SerializeObject(new { success = true, json });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error exporting tools: {ex.Message}");
            }
        }
        #endregion
    }
}