// AiStudio4/Services/Logging/FileLogger.cs
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace AiStudio4.Services.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Func<FileLoggerConfiguration> _getCurrentConfig;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, Func<FileLoggerConfiguration> getCurrentConfig)
        {
            _categoryName = categoryName;
            _getCurrentConfig = getCurrentConfig;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => _getCurrentConfig().LogLevel <= logLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "Logs");
            Directory.CreateDirectory(logDirectory);

            var logFile = Path.Combine(logDirectory, $"app-{DateTime.UtcNow:yyyy-MM-dd}.log");

            // Ensure thread-safe file access
            lock (_lock)
            {
                var logRecord = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff zzz}] [{logLevel}] [{_categoryName}] - {formatter(state, exception)}";
                if (exception != null)
                {
                    logRecord += Environment.NewLine + exception.ToString();
                }

                File.AppendAllText(logFile, logRecord + Environment.NewLine);
            }
        }
    }
}