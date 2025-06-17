using AiStudio4.Core.Models;
using AiStudio4.InjectedDependencies;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Core.Interfaces;

/// <summary>
/// Interface for executing tools locally while allowing AI providers to manage the tool loop.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes a single tool call with the given parameters.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute.</param>
    /// <param name="toolParameters">The JSON string of parameters for the tool.</param>
    /// <param name="context">Execution context including client ID, cancellation token, etc.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<BuiltinToolResult> ExecuteToolAsync(string toolName, string toolParameters, ToolExecutionContext context);

    /// <summary>
    /// Gets all tools available for the AI to use.
    /// </summary>
    /// <param name="toolIds">A list of specific tool GUIDs to retrieve.</param>
    /// <returns>A collection of tool definitions.</returns>
    Task<IEnumerable<Tool>> GetAvailableToolsAsync(IEnumerable<string> toolIds);
}

/// <summary>
/// Context information for tool execution.
/// </summary>
public class ToolExecutionContext
{
    public string ClientId { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public v4BranchedConv BranchedConversation { get; set; }
    public LinearConv LinearConversation { get; set; }
    public int CurrentIteration { get; set; }
}