// AiStudio4/Services/Logging/FileLoggerProvider.cs
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AiStudio4.Services.Logging
{
    public sealed class FileLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new ConcurrentDictionary<string, FileLogger>();
        private readonly FileLoggerConfiguration _config;

        public FileLoggerProvider(FileLoggerConfiguration config)
        {
            _config = config;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new FileLogger(name, () => _config));

        public void Dispose() => _loggers.Clear();
    }
}