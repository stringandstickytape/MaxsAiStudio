// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Services\LogViewerService.cs

using System.Collections.Concurrent;



using AiStudio4.Models;
using AiStudio4.Services.Interfaces;

namespace AiStudio4.Services
{
    /// <summary>
    /// Thread-safe, in-memory store of recent <see cref="LogEntry"/> instances and dispatcher-safe event publisher.
    /// </summary>
    public class LogViewerService : ILogViewerService
    {
        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private const int MaxLogHistory = 5_000;

        public event Action<LogEntry>? OnLogReceived;

        public IReadOnlyCollection<LogEntry> GetLogHistory()
        {
            return _logQueue.ToList().AsReadOnly();
        }

        public void Log(LogEntry entry)
        {
            _logQueue.Enqueue(entry);
            while (_logQueue.Count > MaxLogHistory)
            {
                _logQueue.TryDequeue(out _);
            }

            // Ensure any UI subscribers are updated on the UI thread to avoid cross-thread exceptions.
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() => OnLogReceived?.Invoke(entry));
            }
            else
            {
                // If no application exists (e.g. during unit tests), invoke directly.
                OnLogReceived?.Invoke(entry);
            }
        }
    }
}
