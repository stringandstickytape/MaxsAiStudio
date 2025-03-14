using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using SharedClasses.Providers;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsManager _settingsManager;
        private readonly WebSocketServer _webSocketServer;
        private readonly ChatManager _chatManager;
        private readonly IToolService _toolService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly IPinnedCommandService _pinnedCommandService;

        public UiRequestBroker(
            IConfiguration configuration,
            SettingsManager settingsManager,
            WebSocketServer webSocketServer,
            ChatManager chatManager,
            IToolService toolService,
            ISystemPromptService systemPromptService,
            IPinnedCommandService pinnedCommandService)
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
            _webSocketServer = webSocketServer;
            _chatManager = chatManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _pinnedCommandService = pinnedCommandService;
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
                    "getConversation" => await _chatManager.HandleHistoricalConversationTreeRequest(clientId, requestObject),
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
                    "getSystemPrompts" => await HandleGetSystemPromptsRequest(),
                    "getSystemPrompt" => await HandleGetSystemPromptRequest(requestObject),
                    "createSystemPrompt" => await HandleCreateSystemPromptRequest(requestObject),
                    "updateSystemPrompt" => await HandleUpdateSystemPromptRequest(requestObject),
                    "deleteSystemPrompt" => await HandleDeleteSystemPromptRequest(requestObject),
                    "setDefaultSystemPrompt" => await HandleSetDefaultSystemPromptRequest(requestObject),
                    "getConversationSystemPrompt" => await HandleGetConversationSystemPromptRequest(requestObject),
                    "setConversationSystemPrompt" => await HandleSetConversationSystemPromptRequest(requestObject),
                    "clearConversationSystemPrompt" => await HandleClearConversationSystemPromptRequest(requestObject),
                    "pinnedCommands/get" => await HandleGetPinnedCommandsRequest(clientId, requestObject),
                    "pinnedCommands/save" => await HandleSavePinnedCommandsRequest(clientId, requestObject),
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
                    "getAppearanceSettings" => await HandleGetAppearanceSettingsRequest(clientId, requestObject),
                    "saveAppearanceSettings" => await HandleSaveAppearanceSettingsRequest(clientId, requestObject),
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

        #region Pinned Commands Request Handlers
        private async Task<string> HandleGetPinnedCommandsRequest(string clientId, JObject requestObject)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

                var pinnedCommands = await _pinnedCommandService.GetPinnedCommandsAsync(clientId);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    pinnedCommands
                });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving pinned commands: {ex.Message}");
            }
        }

        private async Task<string> HandleSavePinnedCommandsRequest(string clientId, JObject requestObject)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

                var pinnedCommands = requestObject["pinnedCommands"]?.ToObject<List<PinnedCommand>>();
                if (pinnedCommands == null)
                {
                    return SerializeError("Pinned commands data is invalid");
                }

                await _pinnedCommandService.SavePinnedCommandsAsync(clientId, pinnedCommands);
                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error saving pinned commands: {ex.Message}");
            }
        }


        #endregion


        #region Appearance Settings Request Handlers
        private async Task<string> HandleGetAppearanceSettingsRequest(string clientId, JObject requestObject)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

                var settings = _settingsManager.GetAppearanceSettings(clientId);
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    fontSize = settings.FontSize,
                    isDarkMode = settings.IsDarkMode
                });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving appearance settings: {ex.Message}");
            }
        }

        private async Task<string> HandleSaveAppearanceSettingsRequest(string clientId, JObject requestObject)
        {
            try
            {
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

                var fontSize = requestObject["fontSize"]?.Value<int>() ?? 16;
                var isDarkMode = requestObject["isDarkMode"]?.Value<bool>() ?? true;

                var settings = new AppearanceSettings
                {
                    FontSize = fontSize,
                    IsDarkMode = isDarkMode
                };

                _settingsManager.UpdateAppearanceSettings(clientId, settings);
                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error saving appearance settings: {ex.Message}");
            }
        }
        #endregion

        #region System Prompt Request Handlers
        private async Task<string> HandleGetSystemPromptsRequest()
        {
            try
            {
                var prompts = await _systemPromptService.GetAllSystemPromptsAsync();
                return JsonConvert.SerializeObject(new { success = true, prompts });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving system prompts: {ex.Message}");
            }
        }

        private async Task<string> HandleGetSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string promptId = requestObject["promptId"]?.ToString();
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var prompt = await _systemPromptService.GetSystemPromptByIdAsync(promptId);
                if (prompt == null) return SerializeError($"System prompt with ID {promptId} not found");
                
                return JsonConvert.SerializeObject(new { success = true, prompt });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleCreateSystemPromptRequest(JObject requestObject)
        {
            try
            {
                var prompt = requestObject.ToObject<Core.Models.SystemPrompt>();
                if (prompt == null) return SerializeError("Invalid system prompt data");
                
                var result = await _systemPromptService.CreateSystemPromptAsync(prompt);
                return JsonConvert.SerializeObject(new { success = true, prompt = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error creating system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateSystemPromptRequest(JObject requestObject)
        {
            try
            {
                var prompt = requestObject.ToObject<Core.Models.SystemPrompt>();
                if (prompt == null || string.IsNullOrEmpty(prompt.Guid)) 
                    return SerializeError("Invalid system prompt data or missing prompt ID");
                
                var result = await _systemPromptService.UpdateSystemPromptAsync(prompt);
                return JsonConvert.SerializeObject(new { success = true, prompt = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string promptId = requestObject["promptId"]?.ToString();
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var success = await _systemPromptService.DeleteSystemPromptAsync(promptId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleSetDefaultSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string promptId = requestObject["promptId"]?.ToString();
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var success = await _systemPromptService.SetDefaultSystemPromptAsync(promptId);
                if (success)
                {
                    _settingsManager.CurrentSettings.DefaultSystemPromptId = promptId;
                    _settingsManager.SaveSettings();
                }
                
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting default system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleGetConversationSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string conversationId = requestObject["conversationId"]?.ToString();
                if (string.IsNullOrEmpty(conversationId)) return SerializeError("Conversation ID cannot be empty");
                
                var prompt = await _systemPromptService.GetConversationSystemPromptAsync(conversationId);
                return JsonConvert.SerializeObject(new { success = true, prompt });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving conversation system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleSetConversationSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string conversationId = requestObject["conversationId"]?.ToString();
                string promptId = requestObject["promptId"]?.ToString();
                
                if (string.IsNullOrEmpty(conversationId)) return SerializeError("Conversation ID cannot be empty");
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var success = await _systemPromptService.SetConversationSystemPromptAsync(conversationId, promptId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting conversation system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleClearConversationSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string conversationId = requestObject["conversationId"]?.ToString();
                if (string.IsNullOrEmpty(conversationId)) return SerializeError("Conversation ID cannot be empty");
                
                var success = await _systemPromptService.ClearConversationSystemPromptAsync(conversationId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error clearing conversation system prompt: {ex.Message}");
            }
        }


        #endregion
    }
}