using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using System.Threading;


namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Defines operations for processing tool/function calls within a chat context.
    /// NOTE: Most tool processing is now handled by provider-managed tool loops.
    /// This interface primarily exists for tool re-application functionality.
    /// </summary>
    public interface IToolProcessorService
    {
         /// <summary>
        /// Re-applies a built-in tool with its original parameters
        /// </summary>
        /// <param name="toolName">The name of the tool to re-apply</param>
        /// <param name="toolParameters">The JSON parameters for the tool</param>
        /// <param name="clientId">The client ID making the request</param>
        /// <returns>The result of the tool execution</returns>
        Task<BuiltinToolResult> ReapplyToolAsync(string toolName, string toolParameters, string clientId);
    }
}
