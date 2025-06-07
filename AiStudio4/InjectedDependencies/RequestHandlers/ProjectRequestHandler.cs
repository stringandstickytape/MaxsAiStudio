// AiStudio4/InjectedDependencies/RequestHandlers/ProjectRequestHandler.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    public class ProjectRequestHandler : BaseRequestHandler
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectRequestHandler> _logger;

        public ProjectRequestHandler(IProjectService projectService, ILogger<ProjectRequestHandler> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        protected override IEnumerable<string> SupportedRequestTypes => new[]
        {
            "getProjects",
            "getProject", 
            "createProject",
            "updateProject",
            "deleteProject",
            "setActiveProject",
            "getActiveProject"
        };

        public override async Task<string> HandleAsync(string clientId, string requestType, JObject requestObject)
        {
            try
            {
                return requestType switch
                {
                    "getProjects" => await HandleGetProjectsAsync(),
                    "getProject" => await HandleGetProjectAsync(requestObject),
                    "createProject" => await HandleCreateProjectAsync(requestObject),
                    "updateProject" => await HandleUpdateProjectAsync(requestObject),
                    "deleteProject" => await HandleDeleteProjectAsync(requestObject),
                    "setActiveProject" => await HandleSetActiveProjectAsync(requestObject),
                    "getActiveProject" => await HandleGetActiveProjectAsync(),
                    _ => SerializeError($"Unknown request type: {requestType}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling project request {RequestType}", requestType);
                return SerializeError(ex.Message);
            }
        }

        private async Task<string> HandleGetProjectsAsync()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return SerializeSuccess(projects);
        }

        private async Task<string> HandleGetProjectAsync(JObject requestObject)
        {
            string projectId = requestObject["projectId"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
                return SerializeError("Project ID cannot be empty");
            
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null)
            {
                return SerializeError("Project not found");
            }
            
            return SerializeSuccess(project);
        }

        private async Task<string> HandleCreateProjectAsync(JObject requestObject)
        {
            var project = requestObject.ToObject<Project>();
            if (project == null)
                return SerializeError("Invalid project data");
                
            var createdProject = await _projectService.CreateProjectAsync(project);
            return SerializeSuccess(createdProject);
        }

        private async Task<string> HandleUpdateProjectAsync(JObject requestObject)
        {
            var project = requestObject.ToObject<Project>();
            if (project == null || string.IsNullOrEmpty(project.Guid))
                return SerializeError("Invalid project data or missing project ID");
                
            var updatedProject = await _projectService.UpdateProjectAsync(project);
            return SerializeSuccess(updatedProject);
        }

        private async Task<string> HandleDeleteProjectAsync(JObject requestObject)
        {
            string projectId = requestObject["projectId"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
                return SerializeError("Project ID cannot be empty");
            
            var success = await _projectService.DeleteProjectAsync(projectId);
            return JsonConvert.SerializeObject(new { success = success });
        }

        private async Task<string> HandleSetActiveProjectAsync(JObject requestObject)
        {
            string projectId = requestObject["projectId"]?.ToString();
            if (string.IsNullOrEmpty(projectId))
                return SerializeError("Project ID cannot be empty");
            
            var success = await _projectService.SetActiveProjectAsync(projectId);
            return JsonConvert.SerializeObject(new { success = success });
        }

        private async Task<string> HandleGetActiveProjectAsync()
        {
            var activeProject = await _projectService.GetActiveProjectAsync();
            return SerializeSuccess(activeProject);
        }
    }
}