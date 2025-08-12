using AiStudio4.Tools.Interfaces;
using AiStudio4.Tools.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AiStudio4.Tools
{
    /// <summary>
    /// Base implementation for all tools in the shared library
    /// </summary>
    public abstract class BaseToolImplementation : ITool
    {
        protected readonly ILogger _logger;
        protected readonly IGeneralSettingsService _generalSettingsService;
        protected readonly IStatusMessageService _statusMessageService;
        protected readonly IBuiltInToolExtraPropertiesService _extraPropertiesService;

        protected string _clientId;
        protected string _projectRoot;

        protected BaseToolImplementation(
            ILogger logger,
            IGeneralSettingsService generalSettingsService,
            IStatusMessageService statusMessageService,
            IBuiltInToolExtraPropertiesService extraPropertiesService = null)
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
            _projectRoot = _generalSettingsService?.GetProjectPath();
            _logger?.LogInformation("Updated project root to: {ProjectRoot}", _projectRoot);
        }

        /// <summary>
        /// Gets the tool definition including metadata and schema
        /// </summary>
        public abstract Tool GetToolDefinition();

        /// <summary>
        /// Processes the tool with the given parameters
        /// </summary>
        public abstract Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties);

        /// <summary>
        /// Sets the client ID for status message routing
        /// </summary>
        public void SetClientId(string clientId)
        {
            _clientId = clientId;
        }

        /// <summary>
        /// Sends a status update message
        /// </summary>
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
                    _logger?.LogDebug("Status update not sent - missing StatusMessageService or clientId: {Message}", statusMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }

        /// <summary>
        /// Creates a standard tool result
        /// </summary>
        protected BuiltinToolResult CreateResult(bool wasProcessed, bool continueProcessing, string resultMessage, string outputFileType = null)
        {
            return new BuiltinToolResult
            {
                WasProcessed = wasProcessed,
                ContinueProcessing = continueProcessing,
                ResultMessage = resultMessage,
                OutputFileType = outputFileType ?? GetToolDefinition().OutputFileType
            };
        }

        /// <summary>
        /// Helper method for tools that support MCP server execution
        /// </summary>
        protected async Task<string> ExecuteWithExtraProperties(string parameters)
        {
            try
            {
                var result = await ProcessAsync(parameters, new Dictionary<string, string>());
                return result.ResultMessage;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing tool");
                return $"Error: {ex.Message}";
            }
        }
    }
}