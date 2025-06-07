// AiStudio4/Services/ProjectService.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    public class ProjectService : IProjectService
    {
        private readonly string _projectsPath;
        private readonly ILogger<ProjectService> _logger;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly object _lockObject = new object();
        private bool _isInitialized = false;

        public ProjectService(ILogger<ProjectService> logger, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _projectsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiStudio4",
                "Config",
                "projects.json");

            Directory.CreateDirectory(Path.GetDirectoryName(_projectsPath));
            _logger.LogInformation("Initialized project storage at {ProjectsPath}", _projectsPath);
        }

        public async Task InitializeAsync()
        {
            if (!_isInitialized)
            {
                await InitializeDefaultProjectAsync();
                _isInitialized = true;
            }
        }

        private async Task InitializeDefaultProjectAsync()
        {
            var projects = await ExecuteWithErrorHandlingAsync(() => GetAllProjectsAsync(), "initializing default project");
            if (!projects.Any())
            {
                // Create a default project based on current ProjectPath setting
                var currentProjectPath = _generalSettingsService.CurrentSettings.ProjectPath;
                var defaultProject = new Project
                {
                    Name = "Default Project",
                    Path = currentProjectPath,
                    Description = "Default project created from existing settings"
                };

                await CreateProjectAsync(defaultProject);
                await SetActiveProjectAsync(defaultProject.Guid);
                _logger.LogInformation("Created default project with path {ProjectPath}", currentProjectPath);
            }
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (!File.Exists(_projectsPath))
                {
                    return new List<Project>();
                }

                var json = await File.ReadAllTextAsync(_projectsPath);
                var projects = JsonConvert.DeserializeObject<List<Project>>(json) ?? new List<Project>();
                return projects;
            }, "getting all projects");
        }

        public async Task<Project> GetProjectByIdAsync(string projectId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var projects = await GetAllProjectsAsync();
                return projects.FirstOrDefault(p => p.Guid == projectId);
            }, $"getting project {projectId}");
        }

        public async Task<Project> CreateProjectAsync(Project project)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrEmpty(project.Guid))
                {
                    project.Guid = Guid.NewGuid().ToString();
                }

                project.CreatedDate = DateTime.UtcNow;
                project.ModifiedDate = DateTime.UtcNow;

                var projects = (await GetAllProjectsAsync()).ToList();
                projects.Add(project);

                await SaveProjectsAsync(projects);
                return project;
            }, "creating project");
        }

        public async Task<Project> UpdateProjectAsync(Project project)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var projects = (await GetAllProjectsAsync()).ToList();
                var existingProject = projects.FirstOrDefault(p => p.Guid == project.Guid);
                
                if (existingProject == null)
                {
                    throw new KeyNotFoundException($"Project with ID {project.Guid} not found");
                }

                project.CreatedDate = existingProject.CreatedDate;
                project.ModifiedDate = DateTime.UtcNow;

                var index = projects.IndexOf(existingProject);
                projects[index] = project;

                await SaveProjectsAsync(projects);
                return project;
            }, $"updating project {project.Guid}");
        }

        public async Task<bool> DeleteProjectAsync(string projectId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var projects = (await GetAllProjectsAsync()).ToList();
                var projectToDelete = projects.FirstOrDefault(p => p.Guid == projectId);
                
                if (projectToDelete == null)
                {
                    return false;
                }

                projects.Remove(projectToDelete);
                await SaveProjectsAsync(projects);

                // If this was the active project, set another project as active or clear active project
                var activeProject = await GetActiveProjectAsync();
                if (activeProject?.Guid == projectId)
                {
                    var remainingProjects = await GetAllProjectsAsync();
                    if (remainingProjects.Any())
                    {
                        await SetActiveProjectAsync(remainingProjects.First().Guid);
                    }
                    else
                    {
                        // Clear active project if no projects remain
                        _generalSettingsService.CurrentSettings.ProjectPath = string.Empty;
                        _generalSettingsService.SaveSettings();
                    }
                }

                return true;
            }, $"deleting project {projectId}");
        }

        public async Task<bool> SetActiveProjectAsync(string projectId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var project = await GetProjectByIdAsync(projectId);
                if (project == null)
                {
                    return false;
                }

                // Update the ProjectPath in GeneralSettingsService
                _generalSettingsService.CurrentSettings.ProjectPath = project.Path;
                _generalSettingsService.SaveSettings();

                _logger.LogInformation("Set active project to {ProjectName} with path {ProjectPath}", project.Name, project.Path);
                return true;
            }, $"setting active project {projectId}");
        }

        public async Task<Project> GetActiveProjectAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var currentProjectPath = _generalSettingsService.CurrentSettings.ProjectPath;
                if (string.IsNullOrEmpty(currentProjectPath))
                {
                    return null;
                }

                var projects = await GetAllProjectsAsync();
                return projects.FirstOrDefault(p => p.Path == currentProjectPath);
            }, "getting active project");
        }

        private async Task SaveProjectsAsync(List<Project> projects)
        {
            var json = JsonConvert.SerializeObject(projects, Formatting.Indented);
            await File.WriteAllTextAsync(_projectsPath, json);
        }

        private async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error {OperationName}", operationName);
                throw;
            }
        }
    }
}