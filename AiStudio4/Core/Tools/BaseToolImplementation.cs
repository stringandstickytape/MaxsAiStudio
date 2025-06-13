 

using AiStudio4.Core.Tools.CodeDiff;







namespace AiStudio4.Core.Tools
{
    
    
    
    public abstract class BaseToolImplementation : ITool
    {
    protected readonly ILogger _logger; 
        protected readonly IGeneralSettingsService _generalSettingsService;
        protected readonly IStatusMessageService _statusMessageService;
        private readonly PathSecurityManager _pathSecurityManager;

        
        
        
        protected string _clientId;

        protected string _projectRoot;
        protected BaseToolImplementation(ILogger logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _statusMessageService = statusMessageService;
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
