using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    public interface IBuiltinToolService
    {
        /// <summary>
        /// Gets all built-in tools
        /// </summary>
        /// <returns>A list of built-in tools</returns>
        List<Tool> GetBuiltinTools();

        /// <summary>
        /// Processes a built-in tool with the given parameters
        /// </summary>
        /// <param name="toolName">The name of the tool to process</param>
        /// <param name="toolParameters">The parameters to pass to the tool</param>
        /// <param name="extraProperties">Extra properties for the tool</param>
        /// <param name="statusUpdateCallback">Optional callback for status updates</param>
        /// <param name="clientId">Optional client ID for status messages</param>
        /// <returns>The result of processing the tool</returns>
        Task<BuiltinToolResult> ProcessBuiltinToolAsync(string toolName, string toolParameters, Dictionary<string, string> extraProperties = null, Action<string> statusUpdateCallback = null, string clientId = null);
        void UpdateProjectRoot();
        void SaveBuiltInToolExtraProperties(string toolName, Dictionary<string, string> extraProperties);
    }
}