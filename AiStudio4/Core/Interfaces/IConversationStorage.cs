using AiStudio4.InjectedDependencies;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IConvStorage
    {
        /// <summary>
        /// Loads a conv by its ID
        /// </summary>
        /// <param name="convId">The ID of the conv to load</param>
        /// <returns>The conv, or a new one if not found</returns>
        Task<v4BranchedConv> LoadConv(string convId);

        /// <summary>
        /// Saves a conv
        /// </summary>
        /// <param name="conv">The conv to save</param>
        Task SaveConv(v4BranchedConv conv);

        /// <summary>
        /// Finds a conv containing a specific message
        /// </summary>
        /// <param name="messageId">The ID of the message to find</param>
        /// <returns>The conv containing the message, or null if not found</returns>
        Task<v4BranchedConv> FindConvByMessageId(string messageId);

        /// <summary>
        /// Gets all convs
        /// </summary>
        /// <returns>All convs</returns>
        Task<IEnumerable<v4BranchedConv>> GetAllConvs();

        /// <summary>
        /// Deletes a conversation by its ID
        /// </summary>
        /// <param name="convId">The ID of the conversation to delete</param>
        /// <returns>True if the conversation was successfully deleted, false otherwise</returns>
        Task<bool> DeleteConv(string convId);
    }
}