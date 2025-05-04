// InjectedDependencies/IProjectFileWatcherService.cs
using AiStudio4.Core.Models;
using System;
using System.Collections.Generic;

namespace AiStudio4.InjectedDependencies
{
    /// <summary>
    /// Service that watches for file system changes in the project directory
    /// and maintains a list of all folders and files.
    /// </summary>
    public interface IProjectFileWatcherService
    {
        /// <summary>
        /// Gets the current project root path being watched
        /// </summary>
        string ProjectPath { get; }

        /// <summary>
        /// Gets all directories in the project (filtered by GitIgnore rules)
        /// </summary>
        IReadOnlyList<string> Directories { get; }

        /// <summary>
        /// Gets all files in the project (filtered by GitIgnore rules)
        /// </summary>
        IReadOnlyList<string> Files { get; }

        /// <summary>
        /// Event that is raised when the file system changes (add, delete, rename)
        /// </summary>
        event EventHandler<FileSystemChangedEventArgs> FileSystemChanged;

        /// <summary>
        /// Initializes the file watcher with the specified project path
        /// </summary>
        /// <param name="projectPath">The root path to watch</param>
        void Initialize(string projectPath);

        /// <summary>
        /// Stops watching the current project path and cleans up resources
        /// </summary>
        void Shutdown();
    }
}