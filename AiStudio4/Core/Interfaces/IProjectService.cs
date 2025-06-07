// AiStudio4/Core/Interfaces/IProjectService.cs
using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IProjectService
    {
        Task<IEnumerable<Project>> GetAllProjectsAsync();
        Task<Project> GetProjectByIdAsync(string projectId);
        Task<Project> CreateProjectAsync(Project project);
        Task<Project> UpdateProjectAsync(Project project);
        Task<bool> DeleteProjectAsync(string projectId);
        Task<bool> SetActiveProjectAsync(string projectId);
        Task<Project> GetActiveProjectAsync();
        Task InitializeAsync();
    }
}