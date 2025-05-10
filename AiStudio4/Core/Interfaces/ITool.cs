using AiStudio4.Core.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Interface for all tools in the application
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Gets the tool definition
        /// </summary>
        /// <returns>The tool definition</returns>
        Tool GetToolDefinition();


        /// <summary>
        /// Processes a tool call with the given parameters and extra properties
        /// </summary>
        /// <param name="toolParameters">The parameters passed to the tool</param>
        /// <param name="extraProperties">User-edited extra properties for this tool instance</param>
        /// <returns>Result of the tool processing</returns>
        Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties, string projectRootPathOverride = null);
    }
}