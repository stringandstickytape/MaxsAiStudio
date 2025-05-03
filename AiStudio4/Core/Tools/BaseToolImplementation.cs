using AiStudio4.Core.Interfaces; // Main interfaces namespace
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Tools
{
    /// <summary>
    /// Base abstract class for tool implementations
    /// </summary>
    public abstract class BaseToolImplementation : ITool
    {
    protected readonly ILogger _logger; // Logger for diagnostic information
        protected readonly IGeneralSettingsService _generalSettingsService;
        protected readonly IStatusMessageService _statusMessageService;
        private readonly PathSecurityManager _pathSecurityManager;

        /// <summary>
        /// Client ID for sending status messages directly via StatusMessageService
        /// </summary>
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
            _clientId = null; // Initialize to null (no client ID by default)
        }

        public void UpdateProjectRoot()
        {
            _projectRoot = _generalSettingsService.CurrentSettings.ProjectPath;
            _logger.LogInformation("Updated project root to: {ProjectRoot}", _projectRoot);
        }

        /// <summary>
        /// Gets the tool definition
        /// </summary>
        /// <returns>The tool definition</returns>
        public abstract Tool GetToolDefinition();

        /// <summary>
        /// Processes a tool call with the given parameters and extra properties
        /// </summary>
        /// <param name="toolParameters">The parameters passed to the tool</param>
        /// <param name="extraProperties">User-edited extra properties for this tool instance</param>
        /// <returns>Result of the tool processing</returns>
        public abstract Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties);

        /// <summary>
        /// Sets the client ID for sending status messages directly via StatusMessageService
        /// </summary>
        /// <param name="clientId">The client ID to send status messages to</param>
        public void SetClientId(string clientId)
        {
            _clientId = clientId;
        }

        /// <summary>
        /// Sends a status update using StatusMessageService if available
        /// </summary>
        /// <param name="statusMessage">The status message to send</param>
        protected async void SendStatusUpdate(string statusMessage)
        {
            try
            {
                // Send via StatusMessageService if available and clientId is set
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
                // Log but don't throw - status updates should never break tool execution
                _logger.LogWarning(ex, "Failed to send status update: {Message}", statusMessage);
            }
        }

        /// <summary>
        /// Creates a standard result for a tool execution
        /// </summary>
        /// <param name="wasProcessed">Whether the tool was processed</param>
        /// <param name="continueProcessing">Whether to continue processing</param>
        /// <param name="resultMessage">Optional result message explaining the outcome</param>
        /// <returns>A BuiltinToolResult object</returns>
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