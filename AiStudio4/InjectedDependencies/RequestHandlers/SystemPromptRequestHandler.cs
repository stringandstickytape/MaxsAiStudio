// AiStudio4/InjectedDependencies/RequestHandlers/SystemPromptRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles system prompt-related requests
    /// </summary>
    public class SystemPromptRequestHandler : BaseRequestHandler
    {
        private readonly ISystemPromptService _systemPromptService;
        private readonly IGeneralSettingsService _generalSettingsService;

        public SystemPromptRequestHandler(
            ISystemPromptService systemPromptService,
            IGeneralSettingsService generalSettingsService)
        {
            _systemPromptService = systemPromptService ?? throw new ArgumentNullException(nameof(systemPromptService));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getSystemPrompts",
            "getSystemPrompt",
            "createSystemPrompt",
            "updateSystemPrompt",
            "deleteSystemPrompt",
            "setDefaultSystemPrompt",
            "getConvSystemPrompt",
            "setConvSystemPrompt",
            "clearConvSystemPrompt"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getSystemPrompts" => await HandleGetSystemPromptsRequest(),
                    "getSystemPrompt" => await HandleGetSystemPromptRequest(requestObject),
                    "createSystemPrompt" => await HandleCreateSystemPromptRequest(requestObject),
                    "updateSystemPrompt" => await HandleUpdateSystemPromptRequest(requestObject),
                    "deleteSystemPrompt" => await HandleDeleteSystemPromptRequest(requestObject),
                    "setDefaultSystemPrompt" => await HandleSetDefaultSystemPromptRequest(requestObject),
                    "getConvSystemPrompt" => await HandleGetConvSystemPromptRequest(requestObject),
                    "setConvSystemPrompt" => await HandleSetConvSystemPromptRequest(requestObject),
                    "clearConvSystemPrompt" => await HandleClearConvSystemPromptRequest(requestObject),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

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
    }
}