using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
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
        protected readonly ILogger _logger;

        protected readonly string _projectRoot;
        protected BaseToolImplementation(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _projectRoot = "C:\\Users\\maxhe\\source\\repos\\CloneTest\\MaxsAiTool\\AiStudio4";
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
        /// <param name="resultMessage">Optional result message</param>
        /// <returns>A BuiltinToolResult object</returns>
        protected BuiltinToolResult CreateResult(bool wasProcessed, bool continueProcessing, string resultMessage = null)
        {
            return new BuiltinToolResult
            {
                WasProcessed = wasProcessed,
                ContinueProcessing = continueProcessing,
                ResultMessage = resultMessage
            };
        }
    }
}