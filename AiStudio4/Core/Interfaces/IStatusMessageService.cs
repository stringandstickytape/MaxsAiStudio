// AiStudio4/Core/Interfaces/IStatusMessageService.cs


namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Interface for the Status Message Service which provides a centralized mechanism
    /// for sending status messages throughout the application
    /// </summary>
    public interface IStatusMessageService
    {
        /// <summary>
        /// Sends a status message to a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client to send the message to</param>
        /// <param name="message">The status message to send</param>
        Task SendStatusMessageAsync(string clientId, string message);

        /// <summary>
        /// Clears the status message for a specific client
        /// </summary>
        /// <param name="clientId">The ID of the client to clear the message for</param>
        Task ClearStatusMessageAsync(string clientId);
    }
}
