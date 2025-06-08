// AiStudio4/Services/Logging/FileLoggerConfiguration.cs
using Microsoft.Extensions.Logging;

namespace AiStudio4.Services.Logging
{
    public class FileLoggerConfiguration
    {
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public int EventId { get; set; } = 0;
    }
}