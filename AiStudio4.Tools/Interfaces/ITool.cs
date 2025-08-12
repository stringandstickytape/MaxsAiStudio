using AiStudio4.Tools.Models;

namespace AiStudio4.Tools.Interfaces
{
    /// <summary>
    /// Interface for all tool implementations
    /// </summary>
    public interface ITool
    {
        /// <summary>
        /// Gets the tool definition including metadata and schema
        /// </summary>
        Tool GetToolDefinition();

        /// <summary>
        /// Processes the tool with the given parameters
        /// </summary>
        Task<BuiltinToolResult> ProcessAsync(string toolParameters, Dictionary<string, string> extraProperties);

        /// <summary>
        /// Updates the project root path
        /// </summary>
        void UpdateProjectRoot();
    }
}