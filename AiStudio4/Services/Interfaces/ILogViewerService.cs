// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Services\Interfaces\ILogViewerService.cs
using System;
using System.Collections.Generic;
using AiStudio4.Models;

namespace AiStudio4.Services.Interfaces
{
    /// <summary>
    /// Provides subscription-based access to application log entries for real-time viewing.
    /// </summary>
    public interface ILogViewerService
    {
        /// <summary>
        /// Raised whenever a new <see cref="LogEntry"/> is produced.
        /// </summary>
        event Action<LogEntry> OnLogReceived;

        /// <summary>
        /// Gets an immutable view of the current log history.
        /// </summary>
        IReadOnlyCollection<LogEntry> GetLogHistory();

        /// <summary>
        /// Adds a new log entry to the history and notifies subscribers.
        /// </summary>
        /// <param name="entry">The log entry to record.</param>
        void Log(LogEntry entry);
    }
}