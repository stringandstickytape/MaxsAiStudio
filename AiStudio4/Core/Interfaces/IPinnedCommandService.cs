using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Service interface for managing user pinned commands
    /// </summary>
    public interface IPinnedCommandService
    {
        /// <summary>
        /// Gets all pinned commands for a client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <returns>A list of pinned commands</returns>
        Task<List<PinnedCommand>> GetPinnedCommandsAsync(string clientId);

        /// <summary>
        /// Saves pinned commands for a client
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="pinnedCommands">The pinned commands to save</param>
        Task SavePinnedCommandsAsync(string clientId, List<PinnedCommand> pinnedCommands);
    }
}