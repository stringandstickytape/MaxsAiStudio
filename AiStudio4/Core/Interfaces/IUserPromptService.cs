using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IUserPromptService
    {
        /// <summary>
        /// Initializes the user prompt service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Gets all user prompts
        /// </summary>
        Task<List<UserPrompt>> GetAllUserPromptsAsync();

        /// <summary>
        /// Gets a specific user prompt by ID
        /// </summary>
        Task<UserPrompt> GetUserPromptByIdAsync(string promptId);

        /// <summary>
        /// Creates a new user prompt
        /// </summary>
        Task<UserPrompt> CreateUserPromptAsync(UserPromptFormValues formValues);

        /// <summary>
        /// Updates an existing user prompt
        /// </summary>
        Task<UserPrompt> UpdateUserPromptAsync(UserPrompt prompt);

        /// <summary>
        /// Deletes a user prompt
        /// </summary>
        Task<bool> DeleteUserPromptAsync(string promptId);

        /// <summary>
        /// Sets the favorite status of a user prompt
        /// </summary>
        Task<bool> SetFavoriteStatusAsync(string promptId, bool isFavorite);

        /// <summary>
        /// Imports user prompts from JSON
        /// </summary>
        Task<List<UserPrompt>> ImportUserPromptsAsync(string json);

        /// <summary>
        /// Exports user prompts to JSON
        /// </summary>
        Task<string> ExportUserPromptsAsync();
    }
}