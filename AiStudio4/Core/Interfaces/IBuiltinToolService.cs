using AiStudio4.Core.Models;
using System.Collections.Generic;

namespace AiStudio4.Core.Interfaces
{
    public interface IBuiltinToolService
    {
        List<Tool> GetBuiltinTools();
        
        /// <summary>
        /// Processes a built-in tool call and determines if it requires special handling
        /// </summary>
        /// <param name="toolName">Name of the tool being called</param>
        /// <param name="toolParameters">The parameters passed to the tool</param>
        /// <returns>Result indicating if the tool was processed and if further processing should continue</returns>
        Task<BuiltinToolResult> ProcessBuiltinToolAsync(string toolName, string toolParameters, Dictionary<string, string> extraProperties);
        void UpdateProjectRoot();
    }
}
