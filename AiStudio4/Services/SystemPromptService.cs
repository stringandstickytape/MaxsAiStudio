using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace AiStudio4.Services
{
    public class SystemPromptService : ISystemPromptService
    {
        private readonly string _promptsPath;
        private readonly string _convPromptsPath;
        private readonly ILogger<SystemPromptService> _logger;
        private readonly object _lockObject = new object();
        private bool _isInitialized = false;

        public SystemPromptService(ILogger<SystemPromptService> logger)
        {
            _logger = logger;
            _promptsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "systemPrompts");
            _convPromptsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "convPrompts");

            Directory.CreateDirectory(_promptsPath);
            Directory.CreateDirectory(_convPromptsPath);

            _logger.LogInformation("Initialized system prompt storage at {PromptsPath}", _promptsPath);
        }

        public async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                await InitializeDefaultPromptAsync();
                _isInitialized = true;
            }
        }

        private async Task InitializeDefaultPromptAsync()
        {
            var prompts = await ExecuteWithErrorHandlingAsync(() => GetAllSystemPromptsAsync(), "initializing default prompt");
            if (!prompts.Any())
            {
                var defaultPrompt = new SystemPrompt
                {
                    Title = "Default Assistant",
                    Content = "You are a helpful assistant. Answer as concisely as possible.",
                    Description = "Standard helpful assistant prompt",
                    IsDefault = true,
                    Tags = new List<string> { "general", "default" }
                };

                await CreateSystemPromptAsync(defaultPrompt);
                _logger.LogInformation("Created default system prompt");
            }
        }

        public async Task<IEnumerable<SystemPrompt>> GetAllSystemPromptsAsync()
        {
            var prompts = new List<SystemPrompt>();
            foreach (var file in Directory.GetFiles(_promptsPath, "*.prompt.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var prompt = JsonConvert.DeserializeObject<SystemPrompt>(json);
                    if (prompt != null)
                    {
                        prompts.Add(prompt);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading system prompt file {File}", file);
                }
            }

            return prompts;
        }

        public Task<SystemPrompt> GetSystemPromptByIdAsync(string promptId)
        {
            return ExecuteWithErrorHandlingAsync<SystemPrompt>(async () =>
            {
                var path = Path.Combine(_promptsPath, $"{promptId}.prompt.json");
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(path);
                return JsonConvert.DeserializeObject<SystemPrompt>(json);
            }, $"retrieving system prompt {promptId}");
        }

        public Task<SystemPrompt> CreateSystemPromptAsync(SystemPrompt prompt)
        {
            return ExecuteWithErrorHandlingAsync<SystemPrompt>(async () =>
            {
                if (string.IsNullOrEmpty(prompt.Guid))
                {
                    prompt.Guid = Guid.NewGuid().ToString();
                }

                prompt.CreatedDate = DateTime.UtcNow;
                prompt.ModifiedDate = DateTime.UtcNow;

                await SavePromptAsync(prompt);

                if (prompt.IsDefault)
                {
                    await SetAllPromptsAsNonDefault(prompt.Guid);
                }

                return prompt;
            }, "creating system prompt");
        }

        public Task<SystemPrompt> UpdateSystemPromptAsync(SystemPrompt prompt)
        {
            return ExecuteWithErrorHandlingAsync<SystemPrompt>(async () =>
            {
                var existingPrompt = await GetSystemPromptByIdAsync(prompt.Guid);
                if (existingPrompt == null)
                {
                    throw new KeyNotFoundException($"System prompt with ID {prompt.Guid} not found");
                }

                prompt.CreatedDate = existingPrompt.CreatedDate;
                prompt.ModifiedDate = DateTime.UtcNow;

                await SavePromptAsync(prompt);

                if (prompt.IsDefault)
                {
                    await SetAllPromptsAsNonDefault(prompt.Guid);
                }

                return prompt;
            }, $"updating system prompt {prompt.Guid}");
        }

        public Task<bool> DeleteSystemPromptAsync(string promptId)
        {
            return ExecuteWithErrorHandlingAsync<bool>(async () =>
            {
                var path = Path.Combine(_promptsPath, $"{promptId}.prompt.json");
                if (!File.Exists(path))
                {
                    return false;
                }

                var prompt = await GetSystemPromptByIdAsync(promptId);
                if (prompt.IsDefault)
                {
                    var allPrompts = await GetAllSystemPromptsAsync();
                    var nextPrompt = allPrompts.FirstOrDefault(p => p.Guid != promptId);
                    if (nextPrompt != null)
                    {
                        await SetDefaultSystemPromptAsync(nextPrompt.Guid);
                    }
                }

                File.Delete(path);
                return true;
            }, $"deleting system prompt {promptId}");
        }

        public Task<bool> SetDefaultSystemPromptAsync(string promptId)
        {
            return ExecuteWithErrorHandlingAsync<bool>(async () =>
            {
                var prompt = await GetSystemPromptByIdAsync(promptId);
                if (prompt == null)
                {
                    return false;
                }

                prompt.IsDefault = true;
                await SavePromptAsync(prompt);
                await SetAllPromptsAsNonDefault(promptId);

                return true;
            }, $"setting default system prompt {promptId}");
        }

        public Task<SystemPrompt> GetDefaultSystemPromptAsync()
        {
            return ExecuteWithErrorHandlingAsync<SystemPrompt>(async () =>
            {
                var prompts = await GetAllSystemPromptsAsync();
                var defaultPrompt = prompts.FirstOrDefault(p => p.IsDefault);

                if (defaultPrompt == null && prompts.Any())
                {
                    defaultPrompt = prompts.First();
                    await SetDefaultSystemPromptAsync(defaultPrompt.Guid);
                }

                return defaultPrompt;
            }, "getting default system prompt");
        }

        public async Task<SystemPrompt> GetConvSystemPromptAsync(string convId)
        {
            try
            {
                var path = Path.Combine(_convPromptsPath, $"{convId}.systemprompt.json");
                if (!File.Exists(path))
                {
                    return await GetDefaultSystemPromptAsync();
                }

                var promptId = await File.ReadAllTextAsync(path);
                var prompt = await GetSystemPromptByIdAsync(promptId);

                if (prompt == null)
                {
                    await ClearConvSystemPromptAsync(convId);
                    return await GetDefaultSystemPromptAsync();
                }

                return prompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system prompt for conv {ConvId}", convId);
                return await GetDefaultSystemPromptAsync();
            }
        }

        public Task<bool> SetConvSystemPromptAsync(string convId, string promptId)
        {
            return ExecuteWithErrorHandlingAsync<bool>(async () =>
            {
                var prompt = await GetSystemPromptByIdAsync(promptId);
                if (prompt == null)
                {
                    return false;
                }

                var path = Path.Combine(_convPromptsPath, $"{convId}.systemprompt.json");

                try
                {
                    await File.WriteAllTextAsync(path, promptId);
                }
                catch
                {

                }

                return true;
            }, $"setting system prompt for conv {convId}");
        }

        public Task<bool> ClearConvSystemPromptAsync(string convId)
        {
            return ExecuteWithErrorHandlingAsync<bool>(() =>
            {
                var path = Path.Combine(_convPromptsPath, $"{convId}.systemprompt.json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return Task.FromResult(true);
            }, $"clearing system prompt for conv {convId}");
        }

        private async Task SavePromptAsync(SystemPrompt prompt)
        {
            var path = Path.Combine(_promptsPath, $"{prompt.Guid}.prompt.json");
            var json = JsonConvert.SerializeObject(prompt, Formatting.Indented);
            await File.WriteAllTextAsync(path, json);
        }

        private async Task SetAllPromptsAsNonDefault(string exceptPromptId)
        {
            var prompts = await GetAllSystemPromptsAsync();
            foreach (var prompt in prompts.Where(p => p.Guid != exceptPromptId && p.IsDefault))
            {
                prompt.IsDefault = false;
                await SavePromptAsync(prompt);
            }
        }

        private async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error {OperationName}", operationName);
                throw;
            }
        }
    }

    public class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartupService> _logger;

        public StartupService(IServiceProvider serviceProvider, ILogger<StartupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting initialization of services");

            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    // Initialize all services that need initialization
                    _logger.LogInformation("Initializing SystemPromptService...");
                    var systemPromptService = scope.ServiceProvider.GetRequiredService<ISystemPromptService>();
                    await systemPromptService.InitializeAsync();

                    _logger.LogInformation("Initializing ToolService...");
                    var toolService = scope.ServiceProvider.GetRequiredService<IToolService>();
                    await toolService.InitializeAsync();

                    _logger.LogInformation("Initializing McpService...");
                    var mcpService = scope.ServiceProvider.GetRequiredService<IMcpService>();
                    await mcpService.InitializeAsync();

                    _logger.LogInformation("Service initialization completed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during service initialization");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping services");
            return Task.CompletedTask;
        }
    }
}