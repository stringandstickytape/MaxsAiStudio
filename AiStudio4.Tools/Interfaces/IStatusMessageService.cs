namespace AiStudio4.Tools.Interfaces
{
    /// <summary>
    /// Service for sending status messages during tool execution
    /// </summary>
    public interface IStatusMessageService
    {
        /// <summary>
        /// Sends a status message asynchronously
        /// </summary>
        Task SendStatusMessageAsync(string clientId, string message);
    }
}