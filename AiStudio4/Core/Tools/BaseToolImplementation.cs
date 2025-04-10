using AiStudio4.Core.Interfaces; // Main interfaces namespace
using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using Microsoft.Extensions.Logging;
using System;
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


        protected string _projectRoot;
        protected BaseToolImplementation(ILogger logger, IGeneralSettingsService generalSettingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _generalSettingsService = generalSettingsService ?? throw new ArgumentNullException(nameof(generalSettingsService));
            UpdateProjectRoot();
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
        /// Processes a tool call with the given parameters
        /// </summary>
        /// <param name="toolParameters">The parameters passed to the tool</param>
        /// <returns>Result of the tool processing</returns>
        public abstract Task<BuiltinToolResult> ProcessAsync(string toolParameters);

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