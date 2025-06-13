// AiStudio4/InjectedDependencies/RequestHandlers/UserPromptRequestHandler.cs








namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles user prompt-related requests
    /// </summary>
    public class UserPromptRequestHandler : BaseRequestHandler
    {
        private readonly IUserPromptService _userPromptService;

        public UserPromptRequestHandler(IUserPromptService userPromptService)
        {
            _userPromptService = userPromptService ?? throw new ArgumentNullException(nameof(userPromptService));
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getUserPrompts",
            "getUserPrompt",
            "createUserPrompt",
            "updateUserPrompt",
            "deleteUserPrompt",
            "setFavoriteUserPrompt",
            "importUserPrompts",
            "exportUserPrompts"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getUserPrompts" => await HandleGetUserPromptsRequest(),
                    "getUserPrompt" => await HandleGetUserPromptRequest(requestObject),
                    "createUserPrompt" => await HandleCreateUserPromptRequest(requestObject),
                    "updateUserPrompt" => await HandleUpdateUserPromptRequest(requestObject),
                    "deleteUserPrompt" => await HandleDeleteUserPromptRequest(requestObject),
                    "setFavoriteUserPrompt" => await HandleSetFavoriteUserPromptRequest(requestObject),
                    "importUserPrompts" => await HandleImportUserPromptsRequest(requestObject),
                    "exportUserPrompts" => await HandleExportUserPromptsRequest(),
                    _ => SerializeError($"Unsupported request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                return SerializeError($"Error handling {requestType} request: {ex.Message}");
            }
        }

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
    }
}
