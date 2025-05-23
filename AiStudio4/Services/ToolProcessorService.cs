﻿using Microsoft.Extensions.Logging;
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
using SharedClasses;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Cryptography;

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
        private readonly IBuiltinToolService _builtinToolService;
        private readonly TimeSpan _minimumRequestInterval = TimeSpan.FromSeconds(1);
        private DateTime _lastRequestTime = DateTime.MinValue;
        private readonly object _rateLimitLock = new object(); // Lock object for thread safety
        private readonly Services.Interfaces.INotificationFacade _notificationFacade;

        public ToolProcessorService(
            ILogger<ToolProcessorService> logger,
            IToolService toolService,
            IMcpService mcpService,
            IBuiltinToolService builtinToolService,
            Services.Interfaces.INotificationFacade notificationFacade)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toolService = toolService ?? throw new ArgumentNullException(nameof(toolService));
            _mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
            _builtinToolService = builtinToolService ?? throw new ArgumentNullException(nameof(builtinToolService));
            _notificationFacade = notificationFacade ?? throw new ArgumentNullException(nameof(notificationFacade));
        }

        /// <summary>
        /// Processes tools from an AI response and determines if further processing is needed
        /// </summary>
        /// <param name="response">The AI response containing potential tool calls</param>
        /// <param name="conv">The current conversation state</param>
        /// <param name="collatedResponse">Builder to accumulate tool execution outputs</param>
        /// <returns>Tool execution result with success status and updated content</returns>
        public async Task<ToolExecutionResult> ProcessToolsAsync(AiResponse response, LinearConv conv, StringBuilder collatedResponse, CancellationToken cancellationToken = default, string clientId = null)
        {
            // Rate limiting with lock for thread safety
            lock (_rateLimitLock)
            {
                var currentTime = DateTime.Now;
                var timeSinceLastRequest = currentTime - _lastRequestTime;

                if (timeSinceLastRequest < _minimumRequestInterval)
                {
                    var delayTime = _minimumRequestInterval - timeSinceLastRequest;
                    _logger.LogInformation($"Rate limiting: Waiting for {delayTime.TotalSeconds:F1} seconds before next processing cycle (inside lock)");
                    // Note: Task.Delay cannot be awaited inside a lock.
                    // If significant delays are common, consider a different rate-limiting approach (e.g., SemaphoreSlim or async lock).
                    // For short delays, Thread.Sleep might be acceptable, but blocks the thread.
                    // Choosing Thread.Sleep for simplicity assuming delays are infrequent/short.
                    Thread.Sleep(delayTime);
                }

                // Update the last request time after any delay
                _lastRequestTime = DateTime.Now;
            }

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
                await _notificationFacade.SendStatusMessageAsync(clientId, "Tools being processed");
                _logger.LogInformation("Tools called: {ToolCount}", response.ToolResponseSet.Tools.Count);
                bool shouldStopProcessing = false;
                var toolResultMessages = new List<LinearConvMessage>();

                foreach (var toolResponse in response.ToolResponseSet.Tools)
                {
                    string toolResultMessageContent = "";
                    string toolIdToReport = toolResponse.ToolName; // Use ToolCallId if available, otherwise fallback
                    string clientIdForTool = clientId; // Store client ID for tool status updates

                    try
                    {
                        var nonMcpTool = await _toolService.GetToolByToolNameAsync(toolResponse.ToolName);

                        if (nonMcpTool == null)
                        {
                            // Check if it's an MCP tool
                            if (toolResponse.ToolName.Contains("_") && serverDefinitions.Any(x => x.IsEnabled && toolResponse.ToolName.StartsWith(x.Id + "_")))
                            {
                                var serverDefinitionId = toolResponse.ToolName.Split('_')[0];
                                var actualToolName = string.Join("_", toolResponse.ToolName.Split('_').Skip(1));

                                toolResultMessageContent = await ProcessMcpTool(response, toolResponse, toolResultMessageContent, serverDefinitionId, actualToolName);
                            }
                            else
                            {

                                // sometimes claude spontaneously stops using the prefix, probably because the unprefixed version has appeared in the chat log so many times.
                                foreach(var serverDefinition in serverDefinitions.Where(x=>x.IsEnabled))
                                {
                                    var tools = await _mcpService.ListToolsAsync(serverDefinition.Id);
                                    var mcpTool = tools.FirstOrDefault(x => x.Name == toolResponse.ToolName);

                                    if(mcpTool != null)
                                    {
                                        toolResultMessageContent = await ProcessMcpTool(response, toolResponse, toolResultMessageContent, serverDefinition.Id, toolResponse.ToolName);
                                        break;
                                    }

                                }
                            }
                        }
                        else
                        {
                            // Process built-in tools first
                            // Retrieve the Tool object to get user-edited ExtraProperties
                            var tool = await _toolService.GetToolByToolNameAsync(toolResponse.ToolName);
                            var extraProps = tool?.ExtraProperties ?? new Dictionary<string, string>();

                            // Pass the extraProps to the tool processor
                            var builtinToolResult = await _builtinToolService.ProcessBuiltinToolAsync(toolResponse.ToolName, toolResponse.ResponseText, extraProps, clientId);

                            if (builtinToolResult.WasProcessed)
                            {
                                response.ResponseText += $"\n\n{toolResponse.ToolName}\n\n";
                                // tool already retrieved above

                                var builtIn = _builtinToolService.GetBuiltinTools().First(x => x.Name == toolResponse.ToolName);

                                _logger.LogInformation("Built-in tool '{ToolName}' was processed.", toolResponse.ToolName);

                                if (!string.IsNullOrEmpty(toolResponse.ToolName))
                                {
                                    toolResultMessageContent += $"{toolResponse.ToolName}";
                                }

                                if (!string.IsNullOrEmpty(builtinToolResult.ResultMessage))
                                {
                                    if (string.IsNullOrEmpty(tool.OutputFileType))
                                        toolResultMessageContent += $": {builtinToolResult.ResultMessage}\n\n";
                                    else toolResultMessageContent += $" Output:\n\n```{tool.OutputFileType}\n{builtinToolResult.ResultMessage}\n```\n\n";
                                }

                                if (!string.IsNullOrEmpty(builtinToolResult.StatusMessage))
                                {
                                    toolResultMessageContent += $"Result: {builtinToolResult.StatusMessage}\n\n";
                                }

                                // If the built-in tool indicates processing should stop
                                if (!builtinToolResult.ContinueProcessing)
                                {
                                    shouldStopProcessing = true;
                                }

                                // Add any attachments from the built-in tool
                                if (builtinToolResult.Attachments != null && builtinToolResult.Attachments.Any())
                                {
                                    attachments.AddRange(builtinToolResult.Attachments);
                                }
                            }
                            else
                            {
                                // Handle non-MCP, non-built-in tools or tools where the server definition is missing/disabled
                                _logger.LogWarning("Tool '{ToolName}' is not an enabled MCP tool or recognized built-in tool.", toolResponse.ToolName);

                                toolResultMessageContent += $"Tool used: {toolResponse.ToolName}\n\n```{tool?.Filetype ?? "json"}\n{toolResponse.ResponseText}\n```\n\n"; // Serialize the result content

                                // since the tool was not processed, the user must process it...
                                continueLoop = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing tool {ToolName}", toolResponse.ToolName);
                        toolResultMessageContent = $"Error executing tool '{toolResponse.ToolName}': {ex.Message}";
                    }

                    // Add tool result message to conversation history
                    conv.messages[conv.messages.Count - 1].content += $"\n{toolResultMessageContent}\n";

                    collatedResponse.AppendLine(toolResultMessageContent);
                }



                if (shouldStopProcessing)
                {
                    continueLoop = false; // Exit loop after processing results if Stop was called
                }
            }

            // Prepare tool request and result information
            StringBuilder toolRequestInfo = new StringBuilder();
            string toolResultInfo = collatedResponse.ToString();

            if (response.ToolResponseSet != null && response.ToolResponseSet.Tools.Any())
            {
                // Collect all tool names and parameters for the ToolRequested property
                foreach (var tool in response.ToolResponseSet.Tools)
                {
                    if (toolRequestInfo.Length > 0)
                        toolRequestInfo.AppendLine();
                    toolRequestInfo.Append($"Tool use requested: {tool.ToolName}");

                    // Include parameters hash to ensure we can detect duplicate calls with different parameters
                    // without storing potentially large parameter text
                    if (!string.IsNullOrEmpty(tool.ResponseText))
                    {
                        string paramHash = ComputeSha256Hash(tool.ResponseText);
                        toolRequestInfo.Append($" [{paramHash.Substring(0,7)}]");
                    }
                }
            }

            var toolRequestInfoOut = continueLoop ? toolRequestInfo.ToString() : $"{toolRequestInfo.ToString()}\n\n{toolResultInfo}";

            return new ToolExecutionResult
            {
                AggregatedToolOutput = collatedResponse.ToString(),
                Attachments = attachments,
                Success = true,
                // ContinueProcessing flag to indicate whether the tool loop should continue
                ShouldContinueToolLoop = continueLoop,
                RequestedToolsSummary = toolRequestInfoOut
            };
        }

        private async Task<string> ProcessMcpTool(AiResponse response, ToolResponseItem toolResponse, string toolResultMessageContent, string serverDefinitionId, string actualToolName)
        {
            response.ResponseText += $"\n\n{actualToolName}\n\n";
            var setsOfToolParameters = string.IsNullOrEmpty(toolResponse.ResponseText)
                ? new List<Dictionary<string, object>>()
                : ExtractMultipleJsonObjects(toolResponse.ResponseText)
                    .Select(json => CustomJsonParser.ParseJson(json))
                    .ToList();

            if (!setsOfToolParameters.Any())
            {
                setsOfToolParameters.Add(new Dictionary<string, object>());
            }

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
            }

            return toolResultMessageContent;
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


        /// <summary>
        /// Computes SHA256 hash of the input string
        /// </summary>
        /// <param name="text">Text to hash</param>
        /// <returns>Hexadecimal string representation of the hash</returns>
        private string ComputeSha256Hash(string text)
        {
            // Create a SHA256 hash from the input string
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));

                // Convert byte array to a string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}