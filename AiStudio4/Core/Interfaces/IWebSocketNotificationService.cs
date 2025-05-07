using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IWebSocketNotificationService
    {
        /// <summary>
        /// Notifies a client about a conv update
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="update">The update to send</param>
        Task NotifyConvUpdate(string clientId, ConvUpdateDto update);

        /// <summary>
        /// Notifies a client about a streaming update
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="update">The update to send</param>
        Task NotifyStreamingUpdate(string clientId, StreamingUpdateDto update);

        /// <summary>
        /// Notifies a client about a conv list
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="convs">The convs to send</param>
        Task NotifyConvList( ConvListDto convs);

        /// <summary>
        /// Notifies a client about an audio transcription result
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="transcriptionText">The transcribed text to send</param>
        Task NotifyTranscription(string transcriptionText);

        /// <summary>
        /// Notifies a client about a status message
        /// </summary>
        /// <param name="clientId">The ID of the client to notify</param>
        /// <param name="message">The status message to send</param>
        Task NotifyStatusMessage(string clientId, string message);
        
        /// <summary>
        /// Notifies all clients about file system changes
        /// </summary>
        /// <param name="directories">List of directories</param>
        /// <param name="files">List of files</param>
        Task NotifyFileSystemChanges(IReadOnlyList<string> directories, IReadOnlyList<string> files);
    }
}