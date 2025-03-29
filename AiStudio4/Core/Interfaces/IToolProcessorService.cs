using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces
{
    /// <summary>
    /// Defines operations for processing tool/function calls within a chat context.
    /// </summary>
    public interface IToolProcessorService
    {
        /// <summary>
        /// Processes tools from an AI response and determines if further processing is needed
        /// </summary>
        /// <param name="response">The AI response containing potential tool calls</param>
        /// <param name="conv">The current conversation state</param>
        /// <param name="collatedResponse">Builder to accumulate tool execution outputs</param>
        /// <returns>Whether to continue the tool processing loop</returns>
        Task<ToolExecutionResult> ProcessToolsAsync(AiResponse response, LinearConv conv, StringBuilder collatedResponse, CancellationToken cancellationToken = default);
    }
}
