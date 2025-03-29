using Microsoft.Extensions.Logging;
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using AiStudio4.Convs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AiStudio4.Services
{
    /// <summary>
    /// Service responsible for processing tool/function calls.
    /// </summary>
    public class ToolProcessorService : IToolProcessorService
    {
        private readonly ILogger<ToolProcessorService> _logger;
        private readonly IToolService _toolService;
        private readonly IMcpService _mcpService;

        public ToolProcessorService(ILogger<ToolProcessorService> logger, IToolService toolService, IMcpService mcpService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolService = toolService ?? throw new ArgumentNullException(nameof(toolService));
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
        }

        /// <summary>
        /// Processes tools from an AI response and determines if further processing is needed
        /// </summary>
        /// <param name="response">The AI response containing potential tool calls</param>
        /// <param name="conv">The current conversation state</param>
        /// <param name="collatedResponse">Builder to accumulate tool execution outputs</param>
        /// <returns>Tool execution result with success status and updated content</returns>
        public async Task<ToolExecutionResult> ProcessToolsAsync(AiResponse response, LinearConv conv, StringBuilder collatedResponse, CancellationToken cancellationToken = default)
        {
            bool continueLoop = true;
            List<Attachment> attachments = new List<Attachment>();
            TokenCost costInfo = new TokenCost();

            var serverDefinitions = await _mcpService.GetAllServerDefinitionsAsync();

            // Check if tools were called
            if (response.ToolResponseSet == null || !response.ToolResponseSet.Tools.Any())
            {
                _logger.LogInformation("No tools called or no enabled servers, exiting loop.");
                continueLoop = false; // Exit loop if no tools are called
            }
            else
            {
                _logger.LogInformation("Tools called: {ToolCount}", response.ToolResponseSet.Tools.Count);
                bool stopToolCalled = false;
                var toolResultMessages = new List<LinearConvMessage>();

                foreach (var toolResponse in response.ToolResponseSet.Tools)
                {
                    // Check for the Stop tool


                    string toolResultMessageContent = "";
                    string toolIdToReport = toolResponse.ToolName; // Use ToolCallId if available, otherwise fallback

                    try
                    {
                        // Check if it's an MCP tool
                        if (toolResponse.ToolName.Contains("_") && serverDefinitions.Any(x => x.IsEnabled && toolResponse.ToolName.StartsWith(x.Id + "_")))
                        {
                            var serverDefinitionId = toolResponse.ToolName.Split('_')[0];
                            var actualToolName = string.Join("_", toolResponse.ToolName.Split('_').Skip(1));

                            var setsOfToolParameters = string.IsNullOrEmpty(toolResponse.ResponseText)
                                ? new List<Dictionary<string, object>>()
                                : ExtractMultipleJsonObjects(toolResponse.ResponseText)
                                    .Select(json => CustomJsonParser.ParseJson(json))
                                    .ToList();

                            foreach (var toolParameterSet in setsOfToolParameters)
                            {
                                _logger.LogDebug("Calling MCP tool: {ServerId} -> {ToolName}", serverDefinitionId, actualToolName);
                                var retVal = await _mcpService.CallToolAsync(serverDefinitionId, actualToolName, toolParameterSet);

                                // Format the tool result for the model
                                if (retVal.Content.Count == 0)
                                {
                                    toolResultMessageContent += "\nTool executed successfully with no return content.\n";
                                }
                                else
                                {
                                    toolResultMessageContent += $"Tool Use: {actualToolName}\n\n";
                                    toolResultMessageContent += $"\n\nParameters:\n{string.Join("\n", toolParameterSet.Select(x => $"{x.Key} : {x.Value.ToString()}"))}\n\n";
                                    toolResultMessageContent += $"```json\n{JsonConvert.SerializeObject(retVal.Content)}\n```\n\n"; // Serialize the result content
                                }
                                _logger.LogDebug("MCP tool result: {Result}", toolResultMessageContent);

                                // Add any attachments from the tool result
                                //if (retVal.Attachments != null && retVal.Attachments.Any())
                                //{
                                //    attachments.AddRange(retVal.Attachments);
                                //}
                            }
                        }
                        else
                        {
                            // Handle non-MCP tools or tools where the server definition is missing/disabled
                            _logger.LogWarning("Tool '{ToolName}' is not an enabled MCP tool.", toolResponse.ToolName);

                            var tool = await _toolService.GetToolByToolNameAsync(toolResponse.ToolName);

                            toolResultMessageContent += $"Tool Use: {toolResponse.ToolName}\n\n```json{tool.Filetype}\n{toolResponse.ResponseText}\n```\n\n"; // Serialize the result content

                            if (toolResponse.ToolName.Equals("stop", StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation("'{StopToolName}' tool called, signalling loop end.", "Stop");
                                stopToolCalled = true;
                                // We still add a result for the stop tool if needed, but signal loop termination
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing tool {ToolName}", toolResponse.ToolName);
                        toolResultMessageContent = $"Error executing tool '{toolResponse.ToolName}': {ex.Message}";
                    }

                    // Add tool result message to conversation history
                    toolResultMessages.Add(new LinearConvMessage
                    {
                        role = "tool",
                        content = toolResultMessageContent
                    });

                    collatedResponse.AppendLine(toolResultMessageContent);
                }

                foreach (var message in toolResultMessages)
                {
                    conv.messages.Last().content += message.content;
                }

                if (stopToolCalled)
                {
                    continueLoop = false; // Exit loop after processing results if Stop was called
                }
            }

            return new ToolExecutionResult
            {
                ResponseText = collatedResponse.ToString(),
                CostInfo = costInfo,
                Attachments = attachments,
                Success = true,
                IterationCount = 1, // This is incremented at the caller level
                // ContinueProcessing flag to indicate whether the tool loop should continue
                ContinueProcessing = continueLoop
            };
        }

        private static List<string> ExtractMultipleJsonObjects(string jsonText)
        {
            var result = new List<string>();
            var textReader = new StringReader(jsonText);
            var jsonReader = new JsonTextReader(textReader)
            {
                SupportMultipleContent = true
            };

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType == JsonToken.StartObject)
                {
                    // Read a complete JSON object
                    JObject obj = JObject.Load(jsonReader);
                    result.Add(obj.ToString(Formatting.None));
                }
            }

            return result;
        }
    }
}
