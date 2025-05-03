// InjectedDependencies/ProjectFileWatcherService.cs
using SharedClasses.Git;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Service that watches for file system changes in the project directory
    /// and maintains a list of all folders and files.
    /// </summary>
    public class ProjectFileWatcherService : IProjectFileWatcherService, IDisposable
    {
        private readonly IGeneralSettingsService _generalSettingsService;
        private FileSystemWatcher _watcher;
        private GitIgnoreFilterManager _gitIgnoreFilter;
        private readonly List<string> _directories = new();
        private readonly List<string> _files = new();
        private readonly object _syncLock = new();
        private bool _isInitialized = false;

        public string ProjectPath { get; private set; }
        public IReadOnlyList<string> Directories => _directories.AsReadOnly();
        public IReadOnlyList<string> Files => _files.AsReadOnly();

        public ProjectFileWatcherService(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            
            // Initialize with the current project path from settings
            if (!string.IsNullOrEmpty(_generalSettingsService.CurrentSettings.ProjectPath))
            {
                Initialize(_generalSettingsService.CurrentSettings.ProjectPath);
            }
        }

        public void Initialize(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));

            lock (_syncLock)
            {
                // Clean up existing watcher if any
                Shutdown();

                ProjectPath = projectPath;

                // Initialize GitIgnore filter
                string gitIgnorePath = Path.Combine(projectPath, ".gitignore");
                string gitIgnoreContent = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath) : string.Empty;
                _gitIgnoreFilter = new GitIgnoreFilterManager(gitIgnoreContent, projectPath);

                // Set up file system watcher
                _watcher = new FileSystemWatcher(projectPath)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };

                // Register event handlers
                _watcher.Created += OnFileSystemChanged;
                _watcher.Deleted += OnFileSystemChanged;
                _watcher.Renamed += OnFileSystemRenamed;
                _watcher.Changed += OnFileSystemChanged;

                // Populate initial file and directory lists
                RefreshFileAndDirectoryLists();

                // Start watching
                _watcher.EnableRaisingEvents = true;
                _isInitialized = true;
            }
        }

        public void Shutdown()
        {
            lock (_syncLock)
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Created -= OnFileSystemChanged;
                    _watcher.Deleted -= OnFileSystemChanged;
                    _watcher.Renamed -= OnFileSystemRenamed;
                    _watcher.Changed -= OnFileSystemChanged;
                    _watcher.Dispose();
                    _watcher = null;
                }

                _directories.Clear();
                _files.Clear();
                _isInitialized = false;
            }
        }

        private void RefreshFileAndDirectoryLists()
        {
            lock (_syncLock)
            {
                _directories.Clear();
                _files.Clear();

                if (!Directory.Exists(ProjectPath))
                    return;

                // Get all directories
                var allDirectories = Directory.GetDirectories(ProjectPath, "*", SearchOption.AllDirectories)
                    .Select(d => d.Replace("\\", "/"))  // Normalize path separators
                    .ToList();

                // Filter directories using GitIgnore rules
                foreach (var dir in allDirectories)
                {
                    if (!_gitIgnoreFilter.PathIsIgnored(dir))
                    {
                        _directories.Add(dir);
                    }
                }

                // Add the project root directory
                _directories.Add(ProjectPath.Replace("\\", "/"));

                // Get all files
                var allFiles = Directory.GetFiles(ProjectPath, "*", SearchOption.AllDirectories)
                    .Select(f => f.Replace("\\", "/"))  // Normalize path separators
                    .ToList();

                // Filter files using GitIgnore rules
                foreach (var file in allFiles)
                {
                    if (!_gitIgnoreFilter.PathIsIgnored(file))
                    {
                        _files.Add(file);
                    }
                }
            }
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (!_isInitialized)
                return;

            lock (_syncLock)
            {
                string path = e.FullPath.Replace("\\", "/");
                bool isDirectory = Directory.Exists(path);

                // Check if the path should be ignored
                if (_gitIgnoreFilter.PathIsIgnored(path))
                    return;

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        if (isDirectory)
                        {
                            if (!_directories.Contains(path))
                                _directories.Add(path);
                        }
                        else
                        {
                            if (!_files.Contains(path))
                                _files.Add(path);
                        }
                        break;

                    case WatcherChangeTypes.Deleted:
                        if (isDirectory)
                        {
                            _directories.Remove(path);
                            // Remove all files and directories under this path
                            _files.RemoveAll(f => f.StartsWith(path + "/"));
                            _directories.RemoveAll(d => d.StartsWith(path + "/"));
                        }
                        else
                        {
                            _files.Remove(path);
                        }
                        break;

                    case WatcherChangeTypes.Changed:
                        // For files, we don't need to do anything special for content changes
                        // For directories, we might need to refresh if attributes changed
                        if (isDirectory)
                        {
                            // This is a rare case, but we might need to refresh if directory attributes changed
                            // that affect whether it should be included or not
                            RefreshFileAndDirectoryLists();
                        }
                        break;
                }
            }
        }

        private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
        {
            if (!_isInitialized)
                return;

            lock (_syncLock)
            {
                string oldPath = e.OldFullPath.Replace("\\", "/");
                string newPath = e.FullPath.Replace("\\", "/");
                bool isDirectory = Directory.Exists(newPath);

                // Check if the new path should be ignored
                if (_gitIgnoreFilter.PathIsIgnored(newPath))
                {
                    // If the new path should be ignored, remove the old path
                    if (isDirectory)
                    {
                        _directories.Remove(oldPath);
                        // Remove all files and directories under this path
                        _files.RemoveAll(f => f.StartsWith(oldPath + "/"));
                        _directories.RemoveAll(d => d.StartsWith(oldPath + "/"));
                    }
                    else
                    {
                        _files.Remove(oldPath);
                    }
                    return;
                }

                // Handle the rename
                if (isDirectory)
                {
                    // Update the directory itself
                    int index = _directories.IndexOf(oldPath);
                    if (index >= 0)
                    {
                        _directories[index] = newPath;
                    }
                    else if (!_directories.Contains(newPath))
                    {
                        _directories.Add(newPath);
                    }

                    // Update all files and directories under this path
                    // This is complex, so we'll just refresh everything
                    RefreshFileAndDirectoryLists();
                }
                else
                {
                    // Update the file
                    int index = _files.IndexOf(oldPath);
                    if (index >= 0)
                    {
                        _files[index] = newPath;
                    }
                    else if (!_files.Contains(newPath))
                    {
                        _files.Add(newPath);
                    }
                }
            }
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}