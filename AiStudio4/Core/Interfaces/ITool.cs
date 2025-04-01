using AiStudio4.Core.Models;
using System.Threading.Tasks;

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
        /// Processes a tool call with the given parameters
        /// </summary>
        /// <param name="toolParameters">The parameters passed to the tool</param>
        /// <returns>Result of the tool processing</returns>
        Task<BuiltinToolResult> ProcessAsync(string toolParameters);
        void UpdateProjectRoot();
    }
}