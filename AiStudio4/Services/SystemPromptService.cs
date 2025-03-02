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
using Microsoft.Extensions.Hosting; // Needed for IHostedService
using Microsoft.Extensions.DependencyInjection; //Needed for scope
using System.Threading;

namespace AiStudio4.Services
{
   public class SystemPromptService : ISystemPromptService
   {
       private readonly string _promptsPath;
       private readonly string _conversationPromptsPath;
       private readonly ILogger<SystemPromptService> _logger;
       private readonly object _lockObject = new object();
       private bool _isInitialized = false; // Track initialization

       public SystemPromptService(ILogger<SystemPromptService> logger)
       {
           _logger = logger;
           _promptsPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
               "AiStudio4",
               "systemPrompts");
           _conversationPromptsPath = Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
               "AiStudio4",
               "conversationPrompts");
           
           Directory.CreateDirectory(_promptsPath);
           Directory.CreateDirectory(_conversationPromptsPath);
           
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
           var prompts = await GetAllSystemPromptsAsync();
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
           try
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
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error retrieving all system prompts");
               throw;
           }
       }

       public async Task<SystemPrompt> GetSystemPromptByIdAsync(string promptId)
       {
           try
           {
               var path = Path.Combine(_promptsPath, $"{promptId}.prompt.json");
               if (!File.Exists(path))
               {
                   return null;
               }
               
               var json = await File.ReadAllTextAsync(path);
               return JsonConvert.DeserializeObject<SystemPrompt>(json);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error retrieving system prompt {PromptId}", promptId);
               throw;
           }
       }

       public async Task<SystemPrompt> CreateSystemPromptAsync(SystemPrompt prompt)
       {
           try
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
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error creating system prompt");
               throw;
           }
       }

       public async Task<SystemPrompt> UpdateSystemPromptAsync(SystemPrompt prompt)
       {
           try
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
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error updating system prompt {PromptId}", prompt.Guid);
               throw;
           }
       }

       public async Task<bool> DeleteSystemPromptAsync(string promptId)
       {
           try
           {
               var path = Path.Combine(_promptsPath, $"{promptId}.prompt.json");
               if (!File.Exists(path))
               {
                   return false;
               }
               
               var prompt = await GetSystemPromptByIdAsync(promptId);
               if (prompt.IsDefault)
               {
                   // Find another prompt to set as default
                   var allPrompts = await GetAllSystemPromptsAsync();
                   var nextPrompt = allPrompts.FirstOrDefault(p => p.Guid != promptId);
                   if (nextPrompt != null)
                   {
                       await SetDefaultSystemPromptAsync(nextPrompt.Guid);
                   }
               }
               
               File.Delete(path);
               return true;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error deleting system prompt {PromptId}", promptId);
               throw;
           }
       }

       public async Task<bool> SetDefaultSystemPromptAsync(string promptId)
       {
           try
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
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error setting default system prompt {PromptId}", promptId);
               throw;
           }
       }

       public async Task<SystemPrompt> GetDefaultSystemPromptAsync()
       {
           try
           {
               var prompts = await GetAllSystemPromptsAsync();
               var defaultPrompt = prompts.FirstOrDefault(p => p.IsDefault);
               
               if (defaultPrompt == null && prompts.Any())
               {
                   // If no default is set but prompts exist, set the first one as default
                   defaultPrompt = prompts.First();
                   await SetDefaultSystemPromptAsync(defaultPrompt.Guid);
               }
               
               return defaultPrompt;
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Error getting default system prompt");
               throw;
           }
       }

        public async Task<SystemPrompt> GetConversationSystemPromptAsync(string conversationId)
        {
            try
            {
                var path = Path.Combine(_conversationPromptsPath, $"{conversationId}.json");
                if (!File.Exists(path))
                {
                    return await GetDefaultSystemPromptAsync();
                }
                
                var promptId = await File.ReadAllTextAsync(path);
                var prompt = await GetSystemPromptByIdAsync(promptId);
                
                if (prompt == null)
                {
                    // If the prompt no longer exists, revert to default
                    await ClearConversationSystemPromptAsync(conversationId);
                    return await GetDefaultSystemPromptAsync();
                }
                
                return prompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system prompt for conversation {ConversationId}", conversationId);
                return await GetDefaultSystemPromptAsync();
            }
        }

        public async Task<bool> SetConversationSystemPromptAsync(string conversationId, string promptId)
        {
            try
            {
                var prompt = await GetSystemPromptByIdAsync(promptId);
                if (prompt == null)
                {
                    return false;
                }
                
                var path = Path.Combine(_conversationPromptsPath, $"{conversationId}.json");
                await File.WriteAllTextAsync(path, promptId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting system prompt for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<bool> ClearConversationSystemPromptAsync(string conversationId)
        {
            try
            {
                var path = Path.Combine(_conversationPromptsPath, $"{conversationId}.json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing system prompt for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        private async Task SavePromptAsync(SystemPrompt prompt)
        {
            var path = Path.Combine(_promptsPath, $"{prompt.Guid}.json");
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
    }

    // Example using IHostedService:
    public class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var systemPromptService = scope.ServiceProvider.GetRequiredService<ISystemPromptService>();
                await systemPromptService.InitializeAsync(); // NOW await the initialization
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}