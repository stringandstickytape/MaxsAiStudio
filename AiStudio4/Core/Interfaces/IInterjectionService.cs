

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Service for managing user interjections during tool loops
    /// </summary>
    public interface IInterjectionService
    {
        /// <summary>
        /// Stores an interjection for a specific client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="interjection">The interjection text</param>
        Task StoreInterjectionAsync(string clientId, string interjection);

        /// <summary>
        /// Retrieves and clears an interjection for a specific client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <returns>The interjection text, or empty string if none exists</returns>
        Task<string> GetAndClearInterjectionAsync(string clientId);

        /// <summary>
        /// Checks if an interjection exists for a specific client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <returns>True if an interjection exists, false otherwise</returns>
        Task<bool> HasInterjectionAsync(string clientId);
    }
}
