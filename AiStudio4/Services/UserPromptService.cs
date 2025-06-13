using AiStudio4.Core.Exceptions;










namespace AiStudio4.Services
{
    public class UserPromptService : IUserPromptService
    {
        private readonly string _userPromptsPath;
        private readonly ILogger<UserPromptService> _logger;
        private bool _isInitialized = false;

        public UserPromptService(ILogger<UserPromptService> logger)
        {
            _logger = logger;
            _userPromptsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "UserPrompts");

            Directory.CreateDirectory(_userPromptsPath);
            _logger.LogInformation("Initialized user prompt storage at {UserPromptsPath}", _userPromptsPath);
        }

        public async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                // Any first-time initialization can go here
                _isInitialized = true;
            }
        }

        public async Task<List<UserPrompt>> GetAllUserPromptsAsync()
        {
            var prompts = new List<UserPrompt>();
            foreach (var file in Directory.GetFiles(_userPromptsPath, "*.prompt.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var prompt = JsonConvert.DeserializeObject<UserPrompt>(json);
                    if (prompt != null)
                    {
                        prompts.Add(prompt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading user prompt file {File}", file);
                }
            }

            return prompts;
        }

        public async Task<UserPrompt> GetUserPromptByIdAsync(string promptId)
        {
            var path = Path.Combine(_userPromptsPath, $"{promptId}.prompt.json");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(path);
                return JsonConvert.DeserializeObject<UserPrompt>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user prompt {PromptId}", promptId);
                throw new UserPromptException($"Failed to get user prompt {promptId}", ex);
            }
        }

        public async Task<UserPrompt> CreateUserPromptAsync(UserPromptFormValues formValues)
        {
            if (string.IsNullOrWhiteSpace(formValues.Title))
                throw new UserPromptException("Prompt title is required");

            if (string.IsNullOrWhiteSpace(formValues.Content))
                throw new UserPromptException("Prompt content is required");

            // Check for shortcut uniqueness if provided
            if (!string.IsNullOrWhiteSpace(formValues.Shortcut))
            {
                var existingPrompts = await GetAllUserPromptsAsync();
                if (existingPrompts.Any(p => p.Shortcut.Equals(formValues.Shortcut, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new UserPromptException($"Shortcut '{formValues.Shortcut}' is already in use");
                }
            }

            var prompt = new UserPrompt
            {
                Title = formValues.Title,
                Content = formValues.Content,
                Description = formValues.Description ?? string.Empty,
                IsFavorite = formValues.IsFavorite,
                Tags = formValues.Tags ?? new List<string>(),
                Shortcut = formValues.Shortcut ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await SavePromptAsync(prompt);
            return prompt;
        }

        public async Task<UserPrompt> UpdateUserPromptAsync(UserPrompt prompt)
        {
            if (prompt == null)
                throw new UserPromptException("Prompt cannot be null");

            if (string.IsNullOrWhiteSpace(prompt.Guid))
                throw new UserPromptException("Prompt ID is required");

            if (string.IsNullOrWhiteSpace(prompt.Title))
                throw new UserPromptException("Prompt title is required");

            if (string.IsNullOrWhiteSpace(prompt.Content))
                throw new UserPromptException("Prompt content is required");

            // Check for shortcut uniqueness if provided
            if (!string.IsNullOrWhiteSpace(prompt.Shortcut))
            {
                var existingPrompts = await GetAllUserPromptsAsync();
                var conflictingPrompt = existingPrompts.FirstOrDefault(p =>
                    p.Shortcut.Equals(prompt.Shortcut, StringComparison.OrdinalIgnoreCase) &&
                    p.Guid != prompt.Guid);

                if (conflictingPrompt != null)
                {
                    throw new UserPromptException($"Shortcut '{prompt.Shortcut}' is already in use");
                }
            }

            var existingPrompt = await GetUserPromptByIdAsync(prompt.Guid);
            if (existingPrompt == null)
            {
                throw new UserPromptException($"User prompt with ID {prompt.Guid} not found");
            }

            // Preserve creation date
            prompt.CreatedDate = existingPrompt.CreatedDate;
            prompt.ModifiedDate = DateTime.UtcNow;

            await SavePromptAsync(prompt);
            return prompt;
        }

        public async Task<bool> DeleteUserPromptAsync(string promptId)
        {
            var path = Path.Combine(_userPromptsPath, $"{promptId}.prompt.json");
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user prompt {PromptId}", promptId);
                throw new UserPromptException($"Failed to delete user prompt {promptId}", ex);
            }
        }

        public async Task<bool> SetFavoriteStatusAsync(string promptId, bool isFavorite)
        {
            var prompt = await GetUserPromptByIdAsync(promptId);
            if (prompt == null)
            {
                return false;
            }

            prompt.IsFavorite = isFavorite;
            prompt.ModifiedDate = DateTime.UtcNow;

            await SavePromptAsync(prompt);
            return true;
        }

        public async Task<List<UserPrompt>> ImportUserPromptsAsync(string json)
        {
            try
            {
                var importedPrompts = JsonConvert.DeserializeObject<List<UserPrompt>>(json);
                if (importedPrompts == null || !importedPrompts.Any())
                {
                    return new List<UserPrompt>();
                }

                var existingPrompts = await GetAllUserPromptsAsync();
                var importedCount = 0;

                foreach (var prompt in importedPrompts)
                {
                    // Validate imported prompt
                    if (string.IsNullOrWhiteSpace(prompt.Title) || string.IsNullOrWhiteSpace(prompt.Content))
                    {
                        continue;
                    }

                    // Check for GUID conflicts
                    if (existingPrompts.Any(p => p.Guid == prompt.Guid))
                    {
                        prompt.Guid = Guid.NewGuid().ToString();
                    }

                    // Check for shortcut conflicts
                    if (!string.IsNullOrWhiteSpace(prompt.Shortcut) &&
                        existingPrompts.Any(p => p.Shortcut.Equals(prompt.Shortcut, StringComparison.OrdinalIgnoreCase)))
                    {
                        prompt.Shortcut = $"{prompt.Shortcut}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                    }

                    // Set/update timestamps
                    if (prompt.CreatedDate == default)
                    {
                        prompt.CreatedDate = DateTime.UtcNow;
                    }
                    prompt.ModifiedDate = DateTime.UtcNow;

                    await SavePromptAsync(prompt);
                    importedCount++;
                }

                _logger.LogInformation("Imported {Count} user prompts", importedCount);
                return importedPrompts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing user prompts");
                throw new UserPromptException("Failed to import user prompts", ex);
            }
        }

        public async Task<string> ExportUserPromptsAsync()
        {
            try
            {
                var prompts = await GetAllUserPromptsAsync();
                return JsonConvert.SerializeObject(prompts, Formatting.Indented);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting user prompts");
                throw new UserPromptException("Failed to export user prompts", ex);
            }
        }

        private async Task SavePromptAsync(UserPrompt prompt)
        {
            var path = Path.Combine(_userPromptsPath, $"{prompt.Guid}.prompt.json");
            var json = JsonConvert.SerializeObject(prompt, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);
        }
    }
}
