// AiStudio4/InjectedDependencies/RequestHandlers/ProjectRequestHandler.cs









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
