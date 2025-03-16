using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using SharedClasses.Providers;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.Services;
using Microsoft.Extensions.Logging;

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
        private readonly IConvStorage _convStorage;
        private readonly IUserPromptService _userPromptService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IChatService _chatService;
        private readonly ClientRequestCancellationService _cancellationService;

        public UiRequestBroker(
            IConfiguration configuration,
            SettingsManager settingsManager,
            WebSocketServer webSocketServer,
            ChatManager chatManager,
            IToolService toolService,
            ISystemPromptService systemPromptService,
            IPinnedCommandService pinnedCommandService,
            IConvStorage convStorage,
            IUserPromptService userPromptService,
            IServiceProvider serviceProvider,
            IChatService chatService,
            ClientRequestCancellationService cancellationService
            )
        {
            _configuration = configuration;
            _settingsManager = settingsManager;
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
        }

        public async Task<string> HandleRequestAsync(string clientId, string requestType, string requestData)
        {
            JObject requestObject = JsonConvert.DeserializeObject<JObject>(requestData);

            try
            {
                return requestType switch
                {
                    "getAllHistoricalConvTrees" => await _chatManager.HandleGetAllHistoricalConvTreesRequest(clientId, requestObject),
                    "getModels" => JsonConvert.SerializeObject(new { success = true, models = _settingsManager.CurrentSettings.ModelList }),
                    "getServiceProviders" => JsonConvert.SerializeObject(new { success = true, providers = _settingsManager.CurrentSettings.ServiceProviders }),
                    "convmessages" => await _chatManager.HandleConvMessagesRequest(clientId, requestObject),
                    "getConv" => await _chatManager.HandleHistoricalConvTreeRequest(clientId, requestObject),
                    "historicalConvTree" => await _chatManager.HandleHistoricalConvTreeRequest(clientId, requestObject),
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
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = requestObject["clientId"]?.ToString();
                    if (string.IsNullOrEmpty(clientId))
                    {
                        return SerializeError("Client ID is required");
                    }
                }

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
    }
}