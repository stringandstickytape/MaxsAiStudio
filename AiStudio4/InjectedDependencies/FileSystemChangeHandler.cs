// InjectedDependencies/FileSystemChangeHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Handles file system change events and notifies clients via WebSockets
    /// </summary>
    public class FileSystemChangeHandler : IDisposable
    {
        private readonly IProjectFileWatcherService _fileWatcherService;
        private readonly IWebSocketNotificationService _notificationService;
        private readonly ILogger<FileSystemChangeHandler> _logger;
        
        public FileSystemChangeHandler(
            IProjectFileWatcherService fileWatcherService,
            IWebSocketNotificationService notificationService,
            ILogger<FileSystemChangeHandler> logger)
        {
            _fileWatcherService = fileWatcherService ?? throw new ArgumentNullException(nameof(fileWatcherService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Subscribe to file system changes
            _fileWatcherService.FileSystemChanged += OnFileSystemChanged;
        }
        
        private async void OnFileSystemChanged(object sender, FileSystemChangedEventArgs e)
        {
            try
            {
                await _notificationService.NotifyFileSystemChanges(e.Directories, e.Files);
                _logger.LogDebug("Notified clients of file system changes: {DirectoryCount} directories, {FileCount} files", 
                    e.Directories.Count, e.Files.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling file system change notification");
            }
        }
        
        public void Dispose()
        {
            // Unsubscribe from events
            if (_fileWatcherService != null)
            {
                _fileWatcherService.FileSystemChanged -= OnFileSystemChanged;
            }
        }
    }
}