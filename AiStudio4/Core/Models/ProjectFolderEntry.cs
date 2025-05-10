/*
C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/Core/Models/ProjectFolderEntry.cs
*/
using System;

namespace AiStudio4.Core.Models
{
    public class ProjectFolderEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } // User-friendly name (e.g., folder name)
        public string Path { get; set; } // Full path to the folder
        public DateTime LastAccessedDate { get; set; } = DateTime.UtcNow;
    }
}