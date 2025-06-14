

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
        Task<SystemPrompt> GetConvSystemPromptAsync(string convId);
        Task<bool> SetConvSystemPromptAsync(string convId, string promptId);
        Task<bool> ClearConvSystemPromptAsync(string convId);
        Task InitializeAsync();
        
        // User prompt association methods
        Task<bool> SetAssociatedUserPromptAsync(string systemPromptId, string userPromptId);
        Task<bool> ClearAssociatedUserPromptAsync(string systemPromptId);
        Task<UserPrompt> GetAssociatedUserPromptAsync(string systemPromptId);
    }
}
