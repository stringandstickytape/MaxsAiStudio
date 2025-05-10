// C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/InjectedDependencies/IProjectHistoryService.cs
using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies
{
    public interface IProjectHistoryService
    {
        Task<List<ProjectFolderEntry>> GetKnownProjectFoldersAsync();
        Task<string> GetProjectPathByIdAsync(string id);
        Task AddOrUpdateProjectFolderAsync(string path, string name = null);
        void LoadSettings();
        void SaveSettings();
    }
}