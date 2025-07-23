 

using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.Core.Interfaces;
using AiStudio4.InjectedDependencies;





namespace AiStudio4.Core.Tools
{
    
    
    
    public abstract class BaseToolImplementation : ITool
    {
        protected readonly ILogger _logger; 
        protected readonly IGeneralSettingsService _generalSettingsService;
        protected readonly IStatusMessageService _statusMessageService;
        protected readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;
        private readonly PathSecurityManager _pathSecurityManager;

        protected string _clientId;
        protected string _projectRoot;
        
        protected BaseToolImplementation(ILogger logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService, IBuiltInToolExtraPropertiesService extraPropertiesService = null)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _statusMessageService = statusMessageService;
            _extraPropertiesService = extraPropertiesService;
            if (_generalSettingsService != null)
            {
                UpdateProjectRoot();
            }
            _clientId = null; 
        }

        public void UpdateProjectRoot()
        {
            _projectRoot = _generalSettingsService.CurrentSettings.ProjectPath;
            _logger.LogInformation("Updated project root to: {ProjectRoot}", _projectRoot);
        }

        
        
        
        
        public abstract Tool GetToolDefinition();

        
        
        
        
        
        
        public abstract Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties);

        
        
        
        
        public void SetClientId(string clientId)
        {
            _clientId = clientId;
        }

        
        
        
        
        protected async void SendStatusUpdate(string statusMessage)
        {
            try
            {
                
                if (_statusMessageService != null && !string.IsNullOrEmpty(_clientId))
                {
                    await _statusMessageService.SendStatusMessageAsync(_clientId, statusMessage);
                }
                else
                {
                    _logger.LogDebug("Status update not sent - missing StatusMessageService or clientId: {Message}", statusMessage);
                }
            }
            catch (Exception ex)
            {
                
                _logger.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }

        
        
        
        
        
        protected string FindAlternativeDirectory(string fullPath)
        {
            try
            {
                
                if (!fullPath.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase))
                    return null;
                
                string relativePath = fullPath.Substring(_projectRoot.Length).TrimStart('\\', '/');
                
                
                foreach (var childDir in Directory.GetDirectories(_projectRoot))
                {
                    string possiblePath = Path.Combine(childDir, relativePath);
                    if (Directory.Exists(possiblePath))
                    {
                        return $"Did you mean {possiblePath}";
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding alternative directory for {FullPath}", fullPath);
                return null;
            }
        }

        /// <summary>
        /// Gets the extra properties for this tool, with automatic fallback to creating a new service if needed.
        /// This method handles both DI-injected and MCP contexts automatically.
        /// </summary>
        /// <returns>Dictionary of extra properties for this tool</returns>
        protected Dictionary<string, string> GetExtraProperties()
        {
            try
            {
                IBuiltInToolExtraPropertiesService service = _extraPropertiesService;
                
                // If no service was injected (e.g., in MCP context), create one
                if (service == null)
                {
                    service = new BuiltInToolExtraPropertiesService();
                }
                
                // Convert tool name to the expected format (first letter lowercase)
                var toolName = GetToolDefinition().Name;
                var formattedToolName = $"{toolName.Substring(0, 1).ToLower()}{toolName.Substring(1)}";
                
                return service.GetExtraProperties(formattedToolName);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to get extra properties for tool");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Helper method for MCP server methods to execute the tool with automatic extra properties.
        /// This eliminates the need to manually handle extra properties in each MCP method.
        /// </summary>
        /// <param name="parameters">JSON parameters for the tool</param>
        /// <returns>Tool execution result message</returns>
        protected async Task<string> ExecuteWithExtraProperties(string parameters = "{}")
        {
            try
            {
                var extraProperties = GetExtraProperties();
                var result = await ProcessAsync(parameters, extraProperties);
                
                if (!result.WasProcessed)
                {
                    return "Tool was not processed successfully.";
                }
                
                return result.ResultMessage ?? "Tool executed successfully with no output.";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing tool with extra properties");
                return $"Error executing tool: {ex.Message}";
            }
        }

        protected BuiltinToolResult CreateResult(bool wasProcessed, bool continueProcessing, string resultMessage = null, string statusMessage = null)
        {
            return new BuiltinToolResult
            {
                WasProcessed = wasProcessed,
                ContinueProcessing = continueProcessing,
                ResultMessage = resultMessage,
                StatusMessage = statusMessage
            };
        }
    }
}
