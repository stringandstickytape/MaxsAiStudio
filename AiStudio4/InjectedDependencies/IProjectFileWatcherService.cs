
using AiStudio4.Core.Models;
using System;
using System.Collections.Generic;

namespace AiStudio4.InjectedDependencies
{
    
    
    
    
    public interface IProjectFileWatcherService
    {
        
        
        
        string ProjectPath { get; }

        
        
        
        IReadOnlyList<string> Directories { get; }

        
        
        
        IReadOnlyList<string> Files { get; }

        
        
        
        event EventHandler<FileSystemChangedEventArgs> FileSystemChanged;

        
        
        
        
        void Initialize(string projectPath);

        
        
        
        void Shutdown();
    }
}
