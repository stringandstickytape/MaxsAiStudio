using AiStudio4.Core.Models;

namespace AiStudio4.Core.Interfaces
{
    public interface ISystemPromptService
    {
        Task<IEnumerable<SystemPrompt>> GetAllSystemPromptsAsync();
        Task<SystemPrompt> GetSystemPromptByIdAsync(string promptId);
        Task<SystemPrompt> CreateSystemPromptAsync(SystemPrompt prompt);
        Task<SystemPrompt> UpdateSystemPromptAsync(SystemPrompt prompt);
        Task<bool> DeleteSystemPromptAsync(string promptId);
        Task<bool> SetDefaultSystemPromptAsync(string promptId);
        Task<SystemPrompt> GetDefaultSystemPromptAsync();
        Task<SystemPrompt> GetConversationSystemPromptAsync(string conversationId);
        Task<bool> SetConversationSystemPromptAsync(string conversationId, string promptId);
        Task<bool> ClearConversationSystemPromptAsync(string conversationId);
        Task InitializeAsync(); // Add this line

    }
}