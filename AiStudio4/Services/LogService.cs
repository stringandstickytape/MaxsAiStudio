// AiStudio4/Services/LogService.cs
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class LogService
    {
        private const int LOG_RETENTION_DAYS = 1; // Hardcoded retention period
        private readonly ILogger<LogService> _logger;
        private readonly string _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4");
        private readonly string _logDirectory;

        public LogService(ILogger<LogService> logger)
        {
            _logger = logger;
            _logDirectory = Path.Combine(_appDataPath, "Logs");
        }

        public async Task CleanupOldLogsAsync()
        {
            _logger.LogInformation("Cleaning up log files older than {RetentionDays} day(s).", LOG_RETENTION_DAYS);
            try
            {
                if (!Directory.Exists(_logDirectory)) return;

                var logFiles = Directory.GetFiles(_logDirectory, "app-*.log");
                foreach (var logFile in logFiles)
                {
                    if ((DateTime.UtcNow - new FileInfo(logFile).LastWriteTimeUtc).TotalDays > LOG_RETENTION_DAYS)
                    {
                        try
                        {
                            File.Delete(logFile);
                            _logger.LogInformation("Deleted old log file: {LogFile}", logFile);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not delete old log file: {LogFile}", logFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during log cleanup.");
            }
        }

        public async Task<string> CreateLogPackageAsync(string destinationZipPath)
        {
            _logger.LogInformation("Creating log package at: {DestinationPath}", destinationZipPath);
            try
            {
                if (File.Exists(destinationZipPath))
                {
                    File.Delete(destinationZipPath);
                }

                using (var archive = ZipFile.Open(destinationZipPath, ZipArchiveMode.Create))
                {
                    // 1. Add Log Files (last 7 days, even though we only retain for 1 day)
                    if (Directory.Exists(_logDirectory))
                    {
                        var recentLogs = Directory.GetFiles(_logDirectory, "app-*.log")
                            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                            .Take(7);

                        foreach (var logFile in recentLogs)
                        {
                            archive.CreateEntryFromFile(logFile, Path.Combine("Logs", Path.GetFileName(logFile)));
                        }
                    }

                    // 2. Add Key Configuration Files
                    var configFiles = new[] { "settings.json", "themes.json" };
                    foreach (var configFile in configFiles)
                    {
                        var configPath = Path.Combine(_appDataPath, configFile);
                        if (File.Exists(configPath))
                        {
                            archive.CreateEntryFromFile(configPath, Path.GetFileName(configPath));
                        }
                    }
                    
                    var mcpConfig = Path.Combine(_appDataPath, "Config", "mcpServers.json");
                    if(File.Exists(mcpConfig))
                    {
                        archive.CreateEntryFromFile(mcpConfig, Path.Combine("Config", Path.GetFileName(mcpConfig)));
                    }
                }

                _logger.LogInformation("Log package created successfully.");
                return destinationZipPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create log package.");
                throw;
            }
        }
    }
}