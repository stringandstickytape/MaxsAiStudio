// AiStudio4/InjectedDependencies/UiRequestBroker.cs
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SharedClasses.Providers;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Services;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace AiStudio4.InjectedDependencies
{
    public class UiRequestBroker
    {
        private readonly IBuiltInToolExtraPropertiesService _builtInToolExtraPropertiesService;
        private readonly IConfiguration _configuration;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly IAppearanceSettingsService _appearanceSettingsService;
        private readonly IProjectHistoryService _projectHistoryService;
        private readonly WebSocketServer _webSocketServer;
        private readonly ChatManager _chatManager;
        private readonly IToolService _toolService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly IPinnedCommandService _pinnedCommandService;
        private readonly IConvStorage _convStorage;
        private readonly IUserPromptService _userPromptService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IChatService _chatService;
        private readonly ClientRequestCancellationService _cancellationService;
        private readonly IMcpService _mcpService;
        private readonly IThemeService _themeService;

        public UiRequestBroker(
            IConfiguration configuration,
            IGeneralSettingsService generalSettingsService,
            IAppearanceSettingsService appearanceSettingsService,
            IProjectHistoryService projectHistoryService,
            WebSocketServer webSocketServer,
            ChatManager chatManager,
            IToolService toolService,
            ISystemPromptService systemPromptService,
            IPinnedCommandService pinnedCommandService,
            IConvStorage convStorage,
            IUserPromptService userPromptService,
            IServiceProvider serviceProvider,
            IChatService chatService,
            ClientRequestCancellationService cancellationService,
            IMcpService mcpService,
            IThemeService themeService,
            IBuiltInToolExtraPropertiesService builtInToolExtraPropertiesService
            )
        {
            _builtInToolExtraPropertiesService = builtInToolExtraPropertiesService;
            _configuration = configuration;
            _generalSettingsService = generalSettingsService;
            _appearanceSettingsService = appearanceSettingsService;
            _projectHistoryService = projectHistoryService;
            _webSocketServer = webSocketServer;
            _chatManager = chatManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _pinnedCommandService = pinnedCommandService;
            _convStorage = convStorage;
            _userPromptService = userPromptService;
            _serviceProvider = serviceProvider;
            _chatService = chatService;
            _cancellationService = cancellationService;
            _mcpService = mcpService;
            _themeService = themeService;
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            if (!requestData.StartsWith("{"))
                requestData = $"{{param:{requestData}}}";
            JObject requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

            if (requestType == "exitApplication")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });
                return "";
            }
            try
            {
                return requestType switch
                {
                    "getAllHistoricalConvTrees" => await _chatManager.HandleGetAllHistoricalConvTreesRequest(clientId, requestObject),
                    "getModels" => JsonConvert.SerializeObject(new { success = true, models = _generalSettingsService.CurrentSettings.ModelList }),
                    "getServiceProviders" => JsonConvert.SerializeObject(new { success = true, providers = _generalSettingsService.CurrentSettings.ServiceProviders }),
                    "convmessages" => await _chatManager.HandleConvMessagesRequest(clientId, requestObject),
                    "getConv" => await _chatManager.HandleHistoricalConvTreeRequest(clientId, requestObject),
                    "historicalConvTree" => await _chatManager.HandleHistoricalConvTreeRequest(clientId, requestObject),
                    "deleteMessageWithDescendants" => await _chatManager.HandleDeleteMessageWithDescendantsRequest(clientId, requestObject),
                    "deleteConv" => await _chatManager.HandleDeleteConvRequest(clientId, requestObject),
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
                    "getConvSystemPrompt" => await HandleGetConvSystemPromptRequest(requestObject),
                    "setConvSystemPrompt" => await HandleSetConvSystemPromptRequest(requestObject),
                    "clearConvSystemPrompt" => await HandleClearConvSystemPromptRequest(requestObject),
                    "pinnedCommands/get" => await HandleGetPinnedCommandsRequest(clientId, requestObject),
                    "pinnedCommands/save" => await HandleSavePinnedCommandsRequest(clientId, requestObject),
                    "getConfig" => JsonConvert.SerializeObject(new
                    {
                        success = true,
                        models = _generalSettingsService.CurrentSettings.ModelList.Select(x => x.ModelName).ToArray(),
                        defaultModel = _generalSettingsService.CurrentSettings.DefaultModel ?? "",
                        secondaryModel = _generalSettingsService.CurrentSettings.SecondaryModel ?? ""
                      }),
                    "setDefaultModel" => await SetModel(_generalSettingsService.UpdateDefaultModel, requestObject),
                    "setSecondaryModel" => await SetModel(_generalSettingsService.UpdateSecondaryModel, requestObject),
                    "addModel" => await AddOrUpdateModel(requestObject, _generalSettingsService.AddModel),
                    "updateModel" => await AddOrUpdateModel(requestObject, _generalSettingsService.UpdateModel, true),
                    "deleteModel" => await DeleteByGuid(_generalSettingsService.DeleteModel, requestObject, "promptId"),
                    "addServiceProvider" => await AddOrUpdateProvider(requestObject, _generalSettingsService.AddServiceProvider),
                    "updateServiceProvider" => await AddOrUpdateProvider(requestObject, _generalSettingsService.UpdateServiceProvider, true),
                    "deleteServiceProvider" => await DeleteByGuid(_generalSettingsService.DeleteServiceProvider, requestObject, "providerGuid"),
                    "getAppearanceSettings" => await HandleGetAppearanceSettingsRequest(clientId, requestObject),
                    "saveAppearanceSettings" => await HandleSaveAppearanceSettingsRequest(clientId, requestObject),
                    "updateMessage" => await HandleUpdateMessageRequest(clientId, requestObject),
                    "getUserPrompts" => await HandleGetUserPromptsRequest(),
                    "getUserPrompt" => await HandleGetUserPromptRequest(requestObject),
                    "createUserPrompt" => await HandleCreateUserPromptRequest(requestObject),
                    "updateUserPrompt" => await HandleUpdateUserPromptRequest(requestObject),
                    "deleteUserPrompt" => await HandleDeleteUserPromptRequest(requestObject),
                    "setFavoriteUserPrompt" => await HandleSetFavoriteUserPromptRequest(requestObject),
                    "importUserPrompts" => await HandleImportUserPromptsRequest(requestObject),
                    "exportUserPrompts" => await HandleExportUserPromptsRequest(),
                    "simpleChat" => await HandleSimpleChatRequest(requestObject),
                    "cancelRequest" => await HandleCancelRequestAsync(clientId, requestObject),
                    "mcpServers/getAll" => await HandleGetAllMcpServersRequest(),
                    "mcpServers/getById" => await HandleGetMcpServerByIdRequest(requestObject),
                    "mcpServers/add" => await HandleAddMcpServerRequest(requestObject),
                    "mcpServers/update" => await HandleUpdateMcpServerRequest(requestObject),
                    "mcpServers/delete" => await HandleDeleteMcpServerRequest(requestObject),
                    "mcpServers/getTools" => await HandleGetMcpServerToolsRequest(requestObject),
                    "themes/getAll" => await HandleGetAllThemesRequest(clientId, requestObject),
                    "themes/getById" => await HandleGetThemeByIdRequest(clientId, requestObject),
                    "themes/add" => await HandleAddThemeRequest(clientId, requestObject),
                    "themes/update" => await HandleUpdateThemeRequest(clientId, requestObject),
                    "themes/delete" => await HandleDeleteThemeRequest(requestObject),
                    "themes/setActive" => await HandleSetActiveThemeRequest(requestObject),
                    "themes/getActive" => await HandleGetActiveThemeRequest(clientId, requestObject),
                    _ => throw new NotImplementedException()
                }; ;
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

                _builtInToolExtraPropertiesService.SaveExtraProperties(tool.Name, tool.ExtraProperties);

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
                string toolId = requestObject["guid"]?.ToString();
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

                var settings = _appearanceSettingsService.GetAppearanceSettings(clientId);
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

                _appearanceSettingsService.UpdateAppearanceSettings(clientId, settings);
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
                var prompts = (await _systemPromptService.GetAllSystemPromptsAsync()).OrderBy(x => x.Title);
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
                    _generalSettingsService.CurrentSettings.DefaultSystemPromptId = promptId;
                    _generalSettingsService.SaveSettings();
                }
                
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting default system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleGetConvSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string convId = requestObject["convId"]?.ToString();
                if (string.IsNullOrEmpty(convId)) return SerializeError("Conv ID cannot be empty");
                
                var prompt = await _systemPromptService.GetConvSystemPromptAsync(convId);
                return JsonConvert.SerializeObject(new { success = true, prompt });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving conv system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleSetConvSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string convId = requestObject["convId"]?.ToString();
                string promptId = requestObject["promptId"]?.ToString();
                
                if (string.IsNullOrEmpty(convId)) return SerializeError("Conv ID cannot be empty");
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var success = await _systemPromptService.SetConvSystemPromptAsync(convId, promptId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting conv system prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleClearConvSystemPromptRequest(JObject requestObject)
        {
            try
            {
                string convId = requestObject["convId"]?.ToString();
                if (string.IsNullOrEmpty(convId)) return SerializeError("Conv ID cannot be empty");
                
                var success = await _systemPromptService.ClearConvSystemPromptAsync(convId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error clearing conv system prompt: {ex.Message}");
            }
        }


        #endregion

        #region Message Editing Request Handlers
        private async Task<string> HandleUpdateMessageRequest(string clientId, JObject requestObject)
        {
            try
            {
                string convId = requestObject["convId"]?.ToString();
                string messageId = requestObject["messageId"]?.ToString();
                string content = requestObject["content"]?.ToString();

                if (string.IsNullOrEmpty(convId))
                    return SerializeError("Conversation ID cannot be empty");

                if (string.IsNullOrEmpty(messageId))
                    return SerializeError("Message ID cannot be empty");

                if (content == null) // Allow empty content but not null
                    return SerializeError("Message content cannot be null");

                // Load the conversation
                var conv = await _convStorage.LoadConv(convId);
                if (conv == null)
                    return SerializeError($"Conversation with ID {convId} not found");

                // Find the message in the conversation
                var allMessages = conv.GetAllMessages();
                var messageToUpdate = allMessages.FirstOrDefault(m => m.Id == messageId);

                if (messageToUpdate == null)
                    return SerializeError($"Message with ID {messageId} not found in conversation {convId}");

                // Update the message content
                messageToUpdate.UserMessage = content;

                // Save the updated conversation
                await _convStorage.SaveConv(conv);

                // Notify the client about the update
                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    type = "conv:update",
                    content = new
                    {
                        messageId,
                        content,
                        convId,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                }));

                return JsonConvert.SerializeObject(new { success = true });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating message: {ex.Message}");
            }
        }
        #endregion

        #region User Prompt Request Handlers
        private async Task<string> HandleGetUserPromptsRequest()
        {
            try
            {
                var prompts = await _userPromptService.GetAllUserPromptsAsync();
                return JsonConvert.SerializeObject(new { success = true, prompts });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving user prompts: {ex.Message}");
            }
        }

        private async Task<string> HandleGetUserPromptRequest(JObject requestObject)
        {
            try
            {
                string promptId = requestObject["promptId"]?.ToString();
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var prompt = await _userPromptService.GetUserPromptByIdAsync(promptId);
                if (prompt == null) return SerializeError($"User prompt with ID {promptId} not found");
                
                return JsonConvert.SerializeObject(new { success = true, prompt });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving user prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleCreateUserPromptRequest(JObject requestObject)
        {
            try
            {
                var formValues = requestObject.ToObject<UserPromptFormValues>();
                if (formValues == null) return SerializeError("Invalid user prompt data");
                
                var prompt = await _userPromptService.CreateUserPromptAsync(formValues);
                return JsonConvert.SerializeObject(new { success = true, prompt });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error creating user prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateUserPromptRequest(JObject requestObject)
        {
            try
            {
                var prompt = requestObject.ToObject<UserPrompt>();
                if (prompt == null || string.IsNullOrEmpty(prompt.Guid)) 
                    return SerializeError("Invalid user prompt data or missing prompt ID");
                
                var result = await _userPromptService.UpdateUserPromptAsync(prompt);
                return JsonConvert.SerializeObject(new { success = true, prompt = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating user prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteUserPromptRequest(JObject requestObject)
        {
            try
            {
                string promptId = requestObject["promptId"]?.ToString();
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var success = await _userPromptService.DeleteUserPromptAsync(promptId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting user prompt: {ex.Message}");
            }
        }

        private async Task<string> HandleSetFavoriteUserPromptRequest(JObject requestObject)
        {
            try
            {
                string promptId = requestObject["promptId"]?.ToString();
                bool isFavorite = requestObject["isFavorite"]?.Value<bool>() ?? false;
                
                if (string.IsNullOrEmpty(promptId)) return SerializeError("Prompt ID cannot be empty");
                
                var success = await _userPromptService.SetFavoriteStatusAsync(promptId, isFavorite);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting favorite status: {ex.Message}");
            }
        }

        private async Task<string> HandleImportUserPromptsRequest(JObject requestObject)
        {
            try
            {
                string jsonData = requestObject["jsonData"]?.ToString();
                if (string.IsNullOrEmpty(jsonData)) return SerializeError("JSON data cannot be empty");
                
                var prompts = await _userPromptService.ImportUserPromptsAsync(jsonData);
                return JsonConvert.SerializeObject(new { success = true, count = prompts.Count });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error importing user prompts: {ex.Message}");
            }
        }

        private async Task<string> HandleExportUserPromptsRequest()
        {
            try
            {
                var json = await _userPromptService.ExportUserPromptsAsync();
                return JsonConvert.SerializeObject(new { success = true, json });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error exporting user prompts: {ex.Message}");
            }
        }
        #endregion

        #region Cancel Request Handler
        private async Task<string> HandleCancelRequestAsync(string clientId, JObject requestObject)
        {
            try
            {
                bool anyCancelled = _cancellationService.CancelAllRequests(clientId);
                
                // Notify the client about the cancellation
                await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(new
                {
                    type = "request:cancelled",
                    content = new
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }
                }));

                return JsonConvert.SerializeObject(new { success = true, cancelled = anyCancelled });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error cancelling requests: {ex.Message}");
            }
        }
        #endregion

        #region Simple Chat Request Handler
        private async Task<string> HandleSimpleChatRequest(JObject requestObject)
        {
            try
            {
                string chatMessage = requestObject["chat"]?.ToString();
                if (string.IsNullOrEmpty(chatMessage))
                    return SerializeError("Chat message cannot be empty");

                var response = await _chatService.ProcessSimpleChatRequest(chatMessage);

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {
                return SerializeError($"Error processing simple chat request: {ex.Message}");
            }
        }
        #endregion

        #region MCP Server Request Handlers
        private async Task<string> HandleGetAllMcpServersRequest()
        {
            try
            {
                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var servers = await _mcpService.GetAllServerDefinitionsAsync();
                return JsonConvert.SerializeObject(new { success = true, servers });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving MCP servers: {ex.Message}");
            }
        }

        private async Task<string> HandleGetMcpServerByIdRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                if (string.IsNullOrEmpty(serverId)) return SerializeError("Server ID cannot be empty");
                
                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var server = await _mcpService.GetServerDefinitionByIdAsync(serverId);
                if (server == null) return SerializeError($"MCP server with ID {serverId} not found");
                
                return JsonConvert.SerializeObject(new { success = true, server });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleAddMcpServerRequest(JObject requestObject)
        {
            try
            {
                var server = requestObject.ToObject<McpServerDefinition>();
                if (server == null) return SerializeError("Invalid MCP server data");
                
                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var result = await _mcpService.AddServerDefinitionAsync(server);
                return JsonConvert.SerializeObject(new { success = true, server = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateMcpServerRequest(JObject requestObject)
        {
            try
            {
                var server = requestObject.ToObject<McpServerDefinition>();
                if (server == null || string.IsNullOrEmpty(server.Id))
                    return SerializeError("Invalid MCP server data or missing server ID");

                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var result = await _mcpService.UpdateServerDefinitionAsync(server);
                return JsonConvert.SerializeObject(new { success = true, server = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteMcpServerRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                if (string.IsNullOrEmpty(serverId)) return SerializeError("Server ID cannot be empty");

                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var success = await _mcpService.DeleteServerDefinitionAsync(serverId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting MCP server: {ex.Message}");
            }
        }

        private async Task<string> HandleGetMcpServerToolsRequest(JObject requestObject)
        {
            try
            {
                string serverId = requestObject["serverId"]?.ToString();
                if (string.IsNullOrEmpty(serverId)) return SerializeError("Server ID cannot be empty");

                await _mcpService.InitializeAsync(); // Ensure service is initialized
                var tools = await _mcpService.ListToolsAsync(serverId);
                return JsonConvert.SerializeObject(new { success = true, tools });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving MCP server tools: {ex.Message}");
            }
        }
        #endregion

        #region Theme Request Handlers
        private async Task<string> HandleGetAllThemesRequest(string clientId, JObject requestObject)
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

                var themes = _themeService.GetAllThemes();
                return JsonConvert.SerializeObject(new { success = true, themes });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving themes: {ex.Message}");
            }
        }

        private async Task<string> HandleGetThemeByIdRequest(string clientId, JObject requestObject)
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

                string themeId = requestObject["themeId"]?.ToString();
                if (string.IsNullOrEmpty(themeId))
                {
                    return SerializeError("Theme ID is required");
                }

                var theme = _themeService.GetThemeById(themeId);
                if (theme == null)
                {
                    return SerializeError($"Theme with ID {themeId} not found");
                }

                return JsonConvert.SerializeObject(new { success = true, theme });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving theme: {ex.Message}");
            }
        }

        private async Task<string> HandleAddThemeRequest(string clientId, JObject requestObject)
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

                var theme = requestObject.ToObject<Theme>();
                if (theme == null)
                {
                    return SerializeError("Invalid theme data");
                }

                var result = _themeService.AddTheme(theme);
                return JsonConvert.SerializeObject(new { success = true, theme = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error adding theme: {ex.Message}");
            }
        }

        private async Task<string> HandleUpdateThemeRequest(string clientId, JObject requestObject)
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

                var theme = requestObject.ToObject<Theme>();
                if (theme == null || string.IsNullOrEmpty(theme.Guid))
                {
                    return SerializeError("Invalid theme data or missing theme ID");
                }

                var result = _themeService.UpdateTheme(theme);
                return JsonConvert.SerializeObject(new { success = true, theme = result });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error updating theme: {ex.Message}");
            }
        }

        private async Task<string> HandleDeleteThemeRequest(JObject requestObject)
        {
            try
            {
                string themeId = requestObject["themeId"]?.ToString();
                if (string.IsNullOrEmpty(themeId))
                {
                    return SerializeError("Theme ID is required");
                }

                var success = _themeService.DeleteTheme(themeId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error deleting theme: {ex.Message}");
            }
        }

        private async Task<string> HandleSetActiveThemeRequest(JObject requestObject)
        {
            try
            {
                string themeId = requestObject["themeId"]?.ToString();
                if (string.IsNullOrEmpty(themeId))
                {
                    return SerializeError("Theme ID is required");
                }

                var success = _themeService.SetActiveTheme(themeId);
                return JsonConvert.SerializeObject(new { success });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error setting active theme: {ex.Message}");
            }
        }

        private async Task<string> HandleGetActiveThemeRequest(string clientId, JObject requestObject)
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

                var themeId = _themeService.GetActiveThemeId();
                return JsonConvert.SerializeObject(new { success = true, themeId });
            }
            catch (Exception ex)
            {
                return SerializeError($"Error retrieving active theme: {ex.Message}");
            }
        }
        #endregion

    }
}