// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Services\Logging\InMemoryLogger.cs

using AiStudio4.Models;
using AiStudio4.Services.Interfaces;


namespace AiStudio4.Services.Logging
{
    /// <summary>
    /// ILogger implementation that forwards all log messages to <see cref="ILogViewerService"/> so
    /// they can be observed by the in-app log viewer in real-time.
    /// </summary>
    public class InMemoryLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ILogViewerService _logViewerService;

        public InMemoryLogger(string categoryName, ILogViewerService logViewerService)
        {
            _categoryName = categoryName;
            _logViewerService = logViewerService;
        }

        public IDisposable? BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true; // let filtering be done in viewer

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = logLevel,
                Category = _categoryName,
                Message = formatter(state, exception),
                ExceptionMessage = exception?.ToString()
            };

            _logViewerService.Log(entry);
        }
    }
}
