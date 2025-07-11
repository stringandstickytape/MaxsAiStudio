using AiStudio4.AiServices;
using AiStudio4.Convs;

using AiStudio4.DataModels;
using AiStudio4.Core.Interfaces;
using SharedClasses.Providers;

namespace AiStudio4.Services.Interfaces
{
    public interface IAiService
    {
        /// <summary>
        /// Fetches a response from the AI provider with full tool loop management.
        /// The provider handles all tool calling, execution coordination, and loop control.
        /// </summary>
        /// <param name="options">Request options including conversation, model, settings, etc.</param>
        /// <param name="toolExecutor">Interface for executing tools locally</param>
        /// <param name="branchedConv">The branched conversation to update during tool execution</param>
        /// <param name="parentMessageId">The parent message ID for new messages</param>
        /// <param name="assistantMessageId">The assistant message ID being created</param>
        /// <param name="clientId">Client ID for status updates and interjections</param>
        /// <returns>Final AI response after all tool executions are complete</returns>
        Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId);

        // New method that uses the options pattern
        Task<AiResponse> FetchResponse(AiRequestOptions options, bool forceNoTools = false);
        

        IToolService ToolService { get; set; }
        IMcpService McpService { get; set; }
        // Events removed
        // public event EventHandler<string> StreamingTextReceived;
        // public event EventHandler<string> StreamingComplete;
        public string ChosenTool { get; set; }


    }
}
