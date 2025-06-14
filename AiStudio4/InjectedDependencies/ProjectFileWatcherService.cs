

using SharedClasses.Git;





namespace AiStudio4.InjectedDependencies
{
    
    
    
    
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
        
        
        public event EventHandler<FileSystemChangedEventArgs> FileSystemChanged;

        public ProjectFileWatcherService(IGeneralSettingsService generalSettingsService)
        {
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
                       
            
            _generalSettingsService.SettingsChanged += OnSettingsChanged;
        }
        
        private void OnSettingsChanged(object sender, EventArgs e)
        {
            
            string currentProjectPath = _generalSettingsService.CurrentSettings.ProjectPath;
            if (!string.IsNullOrEmpty(currentProjectPath) && currentProjectPath != ProjectPath)
            {
                
                Initialize(currentProjectPath);
            }
        }

        public void Initialize(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
                throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));

            lock (_syncLock)
            {
                
                Shutdown();

                ProjectPath = projectPath;

                
                string gitIgnorePath = Path.Combine(projectPath, ".gitignore");
                string gitIgnoreContent = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath) : string.Empty;
                _gitIgnoreFilter = new GitIgnoreFilterManager(gitIgnoreContent, projectPath);

                
                _watcher = new FileSystemWatcher(projectPath)
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                };

                
                _watcher.Created += OnFileSystemChanged;
                _watcher.Deleted += OnFileSystemChanged;
                _watcher.Renamed += OnFileSystemRenamed;
                _watcher.Changed += OnFileSystemChanged;

                
                RefreshFileAndDirectoryLists();

                
                _watcher.EnableRaisingEvents = true;
                _isInitialized = true;
                
                
                RaiseFileSystemChangedEvent();
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

                
                string normalizedRootPath = ProjectPath.Replace("\\", "/");
                _directories.Add(normalizedRootPath);

                
                ProcessDirectory(normalizedRootPath);
            }
        }

        private void ProcessDirectory(string directoryPath)
        {
            try
            {
                
                var childDirectories = Directory.GetDirectories(directoryPath)
                    .Select(d => d.Replace("\\", "/"))  
                    .ToList();

                
                foreach (var dir in childDirectories)
                {
                    if (!_gitIgnoreFilter.PathIsIgnored($"{dir}/"))
                    {
                        _directories.Add(dir);

                        
                        ProcessDirectory(dir);
                    }
                }

                
                var files = Directory.GetFiles(directoryPath)
                    .Select(f => f.Replace("\\", "/"))  
                    .ToList();

                foreach (var file in files)
                {
                    if (!_gitIgnoreFilter.PathIsIgnored(file))
                    {
                        _files.Add(file);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                
            }
            catch (IOException)
            {
                
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

                if (isDirectory)
                    path = $"{path}/";

                
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
                            
                            _files.RemoveAll(f => f.StartsWith(path + "/"));
                            _directories.RemoveAll(d => d.StartsWith(path + "/"));
                        }
                        else
                        {
                            _files.Remove(path);
                        }
                        break;

                    case WatcherChangeTypes.Changed:
                        
                        
                        if (isDirectory)
                        {
                            
                            
                            RefreshFileAndDirectoryLists();
                        }
                        break;
                }
                
                
                if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Deleted)
                {
                    RaiseFileSystemChangedEvent();
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

                
                if (_gitIgnoreFilter.PathIsIgnored(newPath))
                {
                    
                    if (isDirectory)
                    {
                        _directories.Remove(oldPath);
                        
                        _files.RemoveAll(f => f.StartsWith(oldPath + "/"));
                        _directories.RemoveAll(d => d.StartsWith(oldPath + "/"));
                    }
                    else
                    {
                        _files.Remove(oldPath);
                    }
                    
                    
                    RaiseFileSystemChangedEvent();
                    return;
                }

                
                if (isDirectory)
                {
                    
                    int index = _directories.IndexOf(oldPath);
                    if (index >= 0)
                    {
                        _directories[index] = newPath;
                    }
                    else if (!_directories.Contains(newPath))
                    {
                        _directories.Add(newPath);
                    }

                    
                    
                    RefreshFileAndDirectoryLists();
                }
                else
                {
                    
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
                
                
                RaiseFileSystemChangedEvent();
            }
        }
        
        
        private void RaiseFileSystemChangedEvent()
        {
            FileSystemChanged?.Invoke(this, new FileSystemChangedEventArgs(Directories, Files));
        }

        public void Dispose()
        {
            
            if (_generalSettingsService != null)
            {
                _generalSettingsService.SettingsChanged -= OnSettingsChanged;
            }
            
            
            Shutdown();
        }
    }
}
