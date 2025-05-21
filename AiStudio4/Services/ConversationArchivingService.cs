/*
// AiStudio4/Services/ConversationArchivingService.cs
*/
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class ConversationArchivingService : IConversationArchivingService
    {
        private readonly ILogger<ConversationArchivingService> _logger;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly string _convsPath;
        private readonly string _archiveSubDirName = "archive";
        private readonly string _archivePath;

        public ConversationArchivingService(ILogger<ConversationArchivingService> logger, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _convsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "convs");
            _archivePath = Path.Combine(_convsPath, _archiveSubDirName);
        }

        public async Task ArchiveAndPruneConversationsAsync()
        {
            _logger.LogInformation("Starting conversation archiving and pruning process.");
            try
            {
                var settings = _generalSettingsService.CurrentSettings;
                int zipRetentionDays = settings.ConversationZipRetentionDays;
                int deleteZippedRetentionDays = settings.ConversationDeleteZippedRetentionDays;

                Directory.CreateDirectory(_archivePath); // Ensure archive directory exists

                await ZipOldConversationsAsync(zipRetentionDays);
                await DeleteOldZippedConversationsAsync(deleteZippedRetentionDays);

                _logger.LogInformation("Conversation archiving and pruning process completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during the overall archiving and pruning process.");
                // Depending on desired behavior, re-throw or handle. For now, just log.
            }
        }

        private async Task ZipOldConversationsAsync(int zipRetentionDays)
        {
            _logger.LogInformation("Starting zipping old conversations. Retention: {ZipRetentionDays} days.", zipRetentionDays);
            if (zipRetentionDays <= 0)
            {
                _logger.LogInformation("Conversation zipping is disabled as retention days is {ZipRetentionDays}.", zipRetentionDays);
                return;
            }
            /*
            try
            {
                var jsonFiles = Directory.EnumerateFiles(_convsPath, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var convJsonFile in jsonFiles)
                {
                    try
                    {
                        var fileLastModifiedDate = File.GetLastWriteTimeUtc(convJsonFile);
                        if ((DateTime.UtcNow - fileLastModifiedDate).TotalDays > zipRetentionDays)
                        {
                            string convId = Path.GetFileNameWithoutExtension(convJsonFile);
                            string zipFileName = $"{convId}_{fileLastModifiedDate:yyyyMMddHHmmss}.zip";
                            string zipFilePath = Path.Combine(_archivePath, zipFileName);

                            if (File.Exists(zipFilePath))
                            {
                                _logger.LogWarning("Archive file {ZipFilePath} already exists, skipping zipping for {ConvId}.", zipFilePath, convId);
                                continue;
                            }

                            _logger.LogInformation("Zipping conversation {ConvId} (LastModified: {FileLastModifiedDate}) to {ZipFilePath}.", convId, fileLastModifiedDate, zipFilePath);
                            
                            // Use a temporary file to avoid issues with ZipFile.CreateFromDirectory if the source file is locked
                            string tempZipFilePath = Path.Combine(Path.GetTempPath(), zipFileName);
                            if(File.Exists(tempZipFilePath)) File.Delete(tempZipFilePath); 

                            using (var zipArchive = ZipFile.Open(tempZipFilePath, ZipArchiveMode.Create))
                            {
                                zipArchive.CreateEntryFromFile(convJsonFile, Path.GetFileName(convJsonFile));
                            }
                            File.Move(tempZipFilePath, zipFilePath);

                            _logger.LogInformation("Successfully zipped conversation {ConvId} to {ZipFilePath}.", convId, zipFilePath);
                            
                            File.Delete(convJsonFile);
                            _logger.LogInformation("Deleted original JSON file for conversation {ConvId}: {ConvJsonFile}.", convId, convJsonFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing or zipping file {ConvJsonFile}. Skipping this file.", convJsonFile);
                        // Continue to the next file
                    }
                    await Task.Delay(10); // Small delay to prevent tight loop on many files, can be adjusted
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while enumerating JSON files for zipping.");
            }*/
            _logger.LogInformation("Zipping old conversations completed.");
        }

        private async Task DeleteOldZippedConversationsAsync(int deleteZippedRetentionDays)
        {
            _logger.LogInformation("Starting deletion of old zipped conversations. Retention: {DeleteZippedRetentionDays} days.", deleteZippedRetentionDays);
            if (deleteZippedRetentionDays <= 0)
            {
                _logger.LogInformation("Deletion of zipped conversations is disabled as retention days is {DeleteZippedRetentionDays}.", deleteZippedRetentionDays);
                return;
            }

            try
            {/*
                var zipFiles = Directory.EnumerateFiles(_archivePath, "*.zip", SearchOption.TopDirectoryOnly);
                Regex timestampRegex = new Regex(@"_(?<timestamp>
\d{14})
.zip$", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

                foreach (var zipFile in zipFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(zipFile);
                        Match match = timestampRegex.Match(fileName);

                        if (match.Success)
                        {
                            string timestampStr = match.Groups["timestamp"].Value;
                            if (DateTime.TryParseExact(timestampStr, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime originalTimestamp))
                            {
                                if ((DateTime.UtcNow - originalTimestamp).TotalDays > deleteZippedRetentionDays)
                                {
                                    _logger.LogInformation("Deleting old zipped archive {ZipFile} (Original Timestamp: {OriginalTimestamp}).", zipFile, originalTimestamp);
                                    File.Delete(zipFile);
                                    _logger.LogInformation("Successfully deleted old zipped archive {ZipFile}.", zipFile);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Could not parse timestamp from zip file name: {ZipFile}. Timestamp string: {TimestampStr}", zipFile, timestampStr);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not extract timestamp from zip file name (does not match pattern *_YYYYMMDDHHMMSS.zip): {ZipFile}.", zipFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing or deleting zip file {ZipFile}. Skipping this file.", zipFile);
                        // Continue to the next file
                    }
                    await Task.Delay(10); // Small delay
                }*/
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while enumerating ZIP files for deletion.");
            }
            _logger.LogInformation("Deletion of old zipped conversations completed.");
        }
    }
}