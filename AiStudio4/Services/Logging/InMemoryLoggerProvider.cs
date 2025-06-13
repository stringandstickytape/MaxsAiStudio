// C:\Users\maxhe\source\repos\MaxsAiStudio\AiStudio4\Services\Logging\InMemoryLoggerProvider.cs
using System.Collections.Concurrent;
using AiStudio4.Services.Interfaces;


namespace AiStudio4.Services.Logging
{
    /// <summary>
    /// ILoggerProvider that creates <see cref="InMemoryLogger"/> instances and routes them to the
    /// shared <see cref="ILogViewerService"/> singleton.
    /// </summary>
    public class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly ILogViewerService _logViewerService;
        private readonly ConcurrentDictionary<string, InMemoryLogger> _loggers = new();

        public InMemoryLoggerProvider(ILogViewerService logViewerService)
        {
            _logViewerService = logViewerService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, n => new InMemoryLogger(n, _logViewerService));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
