using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IUserPromptService
    {
        
        
        
        Task InitializeAsync();

        
        
        
        Task<List<UserPrompt>> GetAllUserPromptsAsync();

        
        
        
        Task<UserPrompt> GetUserPromptByIdAsync(string promptId);

        
        
        
        Task<UserPrompt> CreateUserPromptAsync(UserPromptFormValues formValues);

        
        
        
        Task<UserPrompt> UpdateUserPromptAsync(UserPrompt prompt);

        
        
        
        Task<bool> DeleteUserPromptAsync(string promptId);

        
        
        
        Task<bool> SetFavoriteStatusAsync(string promptId, bool isFavorite);

        
        
        
        Task<List<UserPrompt>> ImportUserPromptsAsync(string json);

        
        
        
        Task<string> ExportUserPromptsAsync();
    }
}
