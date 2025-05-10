using AiStudio4.Core.Interfaces; // Main interfaces namespace
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools.CodeDiff;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Client ID for sending status messages directly via StatusMessageService
        /// </summary>
        protected string _clientId;

        protected BaseToolImplementation(ILogger logger, IGeneralSettingsService generalSettingsService, IStatusMessageService statusMessageService)
        {
            _logger = logger;
            _generalSettingsService = generalSettingsService;
            _statusMessageService = statusMessageService;
            _clientId = null; // Initialize to null (no client ID by default)
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
        /// <param name="projectRootPathOverride">Optional override for the project root path</param>
        /// <returns>Result of the tool processing</returns>
        public abstract Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties, string projectRootPathOverride);

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

        protected string GetActiveProjectRoot(string projectRootPathOverride)
        {
            // Priority: Override > Server Default from GeneralSettings
            return !string.IsNullOrEmpty(projectRootPathOverride) ? projectRootPathOverride : _generalSettingsService.CurrentSettings.ProjectPath;
        }

        /// <summary>
        /// Checks if a directory exists in any immediate child of the project root
        /// </summary>
        /// <param name="fullPath">The full path that was not found</param>
        /// <param name="activeRoot">The active project root for the current request</param>
        /// <returns>A suggestion message if an alternative path is found, otherwise null</returns>
        protected string FindAlternativeDirectory(string fullPath, string activeRoot)
        {
            try
            {
                // Extract the relative path by removing the activeRoot
                if (string.IsNullOrEmpty(activeRoot) || !fullPath.StartsWith(activeRoot, StringComparison.OrdinalIgnoreCase))
                    return null;
                
                string relativePath = fullPath.Substring(activeRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                // Check each immediate child directory of the activeRoot
                foreach (var childDir in Directory.GetDirectories(activeRoot))
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