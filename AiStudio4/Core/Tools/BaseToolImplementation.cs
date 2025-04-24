using AiStudio4.Core.Interfaces; // Main interfaces namespace
using AiStudio4.Core.Models;
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
        
        /// <summary>
        /// Optional callback for sending status updates during tool execution
        /// </summary>
        protected Action<string> _statusUpdateCallback;

        protected string _projectRoot;
        protected BaseToolImplementation(ILogger logger, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            if (_generalSettingsService != null)
            {
                UpdateProjectRoot();
            }
            _statusUpdateCallback = null; // Initialize to null (no status updates by default)
        }

        public void UpdateProjectRoot()
        {
            _projectRoot = _generalSettingsService.CurrentSettings.ProjectPath;
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
        /// Sets the status update callback for this tool instance
        /// </summary>
        /// <param name="statusUpdateCallback">Callback action that takes a status message string</param>
        public void SetStatusUpdateCallback(Action<string> statusUpdateCallback)
        {
            _statusUpdateCallback = statusUpdateCallback;
        }

        /// <summary>
        /// Sends a status update if a callback is registered
        /// </summary>
        /// <param name="statusMessage">The status message to send</param>
        protected void SendStatusUpdate(string statusMessage)
        {
            try
            {
                // Only send if callback is registered
                _statusUpdateCallback?.Invoke(statusMessage);
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