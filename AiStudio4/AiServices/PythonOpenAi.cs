using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using SharedClasses.Providers;
using System.Diagnostics;
using System.Text;
using System.Net.Http;

namespace AiStudio4.AiServices
{
    public class PythonOpenAi : AiServiceBase, IDisposable
    {
        private Process _pythonProcess;
        private StreamWriter _processWriter;
        private readonly object _processLock = new object();
        private bool _isDisposed = false;
        private static PythonEnvironmentValidator.ValidationResult _validationResult;
        private static bool _validationChecked = false;
        private bool _processStarted = false;

        public PythonOpenAi()
        {
            EnsureEnvironmentValidated();
        }

        private static void EnsureEnvironmentValidated()
        {
            if (!_validationChecked)
            {
                _validationResult = PythonEnvironmentValidator.ValidateEnvironment();
                _validationChecked = true;
            }
        }

        public static bool IsEnvironmentValid => _validationResult?.IsValid ?? false;
        public static string GetSetupInstructions() => _validationResult?.SetupInstructions ?? "";

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            // Check environment before attempting to use
            if (!IsEnvironmentValid)
            {
                throw new InvalidOperationException(
                    $"PythonOpenAI provider is not available.\n\n{GetSetupInstructions()}");
            }

            // Ensure the long-running Python process is started
            await EnsurePythonProcessIsRunningAsync();

            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = await CreatePythonRequestPayload(options, forceNoTools);
            string jsonRequest = JsonConvert.SerializeObject(requestPayload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            // Send request to the long-running Python process
            lock (_processLock)
            {
                if (_processWriter == null || _pythonProcess?.HasExited == true)
                {
                    throw new InvalidOperationException("Python bridge process is not running");
                }

                _processWriter.WriteLine(jsonRequest);
                _processWriter.Flush();
            }

            return await ReadResponseFromProcess(options);
        }

        private async Task<object> CreatePythonRequestPayload(AiRequestOptions options, bool forceNoTools)
        {
            var messages = new JArray();

            // Add system message if present
            if (!string.IsNullOrWhiteSpace(options.Conv.systemprompt))
            {
                messages.Add(new JObject
                {
                    ["role"] = "system",
                    ["content"] = options.Conv.SystemPromptWithDateTime()
                });
            }

            // Add conversation messages
            foreach (var message in options.Conv.messages)
            {
                var messageObj = CreatePythonMessageObject(message);
                messages.Add(messageObj);
            }

            // Parse additional parameters
            var additionalParams = ParseAdditionalParams(options.Model.AdditionalParams);

            var payload = new Dictionary<string, object>
            {
                ["api_key"] = options.ServiceProvider.ApiKey,
                ["base_url"] = !string.IsNullOrWhiteSpace(options.ServiceProvider.Url) ? options.ServiceProvider.Url : null,
                ["model"] = options.Model.ModelName,
                ["messages"] = messages,
                ["temperature"] = options.Model.Requires1fTemp ? 1f : options.ApiSettings.Temperature,
                ["max_tokens"] = additionalParams.GetValueOrDefault("max_tokens"),
                ["stream"] = true,
                ["tools"] = await CreateToolsArray(options.ToolIds, forceNoTools),
                ["tool_choice"] = forceNoTools ? "none" : "auto",
                ["web_search_options"] = ShouldUseWebSearch(options.Model.ModelName) ? new { } : null
            };

            if(options.Model.AllowsTopP)
            {
                payload["top_p"] = options.TopP ?? null;
            }





            return payload;
        }

        private JObject CreatePythonMessageObject(LinearConvMessage message)
        {
            var messageObj = new JObject
            {
                ["role"] = message.role,
            };

            var contentArray = new JArray();

            // Add text content
            if (!string.IsNullOrWhiteSpace(message.content))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = message.content
                });
            }

            // Add image content (legacy single image)
            if (!string.IsNullOrWhiteSpace(message.base64image))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JObject
                    {
                        ["url"] = $"data:{message.base64type};base64,{message.base64image}"
                    }
                });
            }

            // Add multiple attachments
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                    {
                        contentArray.Add(new JObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JObject
                            {
                                ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                            }
                        });
                    }
                }
            }

            // Use content array if we have multiple content types, otherwise use simple string
            if (contentArray.Count > 1 || (contentArray.Count == 1 && contentArray[0]["type"]?.ToString() != "text"))
            {
                messageObj["content"] = contentArray;
            }
            else if (contentArray.Count == 1 && contentArray[0]["type"]?.ToString() == "text")
            {
                messageObj["content"] = contentArray[0]["text"]?.ToString();
            }
            else
            {
                messageObj["content"] = message.content ?? "";
            }

            return messageObj;
        }

        private async Task<JArray> CreateToolsArray(List<string> toolIds, bool forceNoTools)
        {
            var toolsArray = new JArray();

            if (forceNoTools || toolIds == null || !toolIds.Any())
                return toolsArray;

            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);
            var tempRequest = new JObject();

            // Add built-in tools
            foreach (var toolId in toolIds)
            {
                await toolRequestBuilder.AddToolToRequestAsync(tempRequest, toolId, ToolFormat.OpenAI);
            }

            // Add MCP tools
            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(tempRequest, ToolFormat.OpenAI);

            // Extract tools from the temp request
            if (tempRequest["tools"] is JArray tools)
            {
                foreach (var tool in tools)
                {
                    toolsArray.Add(tool);
                }
            }

            return toolsArray;
        }

        private async Task<AiResponse> ReadResponseFromProcess(AiRequestOptions options)
        {
            var responseBuilder = new StringBuilder();
            var tokenUsage = new TokenUsage("0", "0");
            var toolCalls = new List<object>();
            string finishReason = "stop";

            try
            {
                while (!_pythonProcess.StandardOutput.EndOfStream)
                {
                    var line = await _pythonProcess.StandardOutput.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var responseObj = JObject.Parse(line);
                    var type = responseObj["type"]?.ToString();

                    switch (type)
                    {
                        case "ready":
                            // Bridge is ready, continue processing
                            continue;

                        case "pong":
                            // Response to ping, continue processing
                            continue;

                        case "chunk":
                            var chunkType = responseObj["chunk_type"]?.ToString();
                            var data = responseObj["data"];

                            if (chunkType == "content" && data != null)
                            {
                                var contentChunk = data.ToString();
                                if (!string.IsNullOrEmpty(contentChunk))
                                {
                                    options.OnStreamingUpdate?.Invoke(contentChunk);
                                    responseBuilder.Append(contentChunk);
                                }
                            }
                            break;

                        case "end":
                            var success = responseObj["success"]?.Value<bool>() ?? false;
                            if (!success)
                            {
                                throw new Exception("Python bridge reported failure");
                            }

                            var content = responseObj["content"]?.ToString() ?? "";
                            var finalToolCalls = responseObj["tool_calls"]?.ToObject<List<object>>();
                            finishReason = responseObj["finish_reason"]?.ToString() ?? "stop";

                            if (finalToolCalls != null)
                            {
                                toolCalls = finalToolCalls;
                            }

                            var usage = responseObj["token_usage"];
                            if (usage != null)
                            {
                                tokenUsage = new TokenUsage(
                                    usage["input_tokens"]?.Value<int>().ToString() ?? "0",
                                    usage["output_tokens"]?.Value<int>().ToString() ?? "0"
                                );
                            }

                            // If we have tool calls, handle them
                            if (toolCalls.Any())
                            {
                                return await HandleToolCalls(options, responseBuilder.ToString(), toolCalls, tokenUsage);
                            }

                            goto EndProcessing;

                        case "error":
                            var errorMessage = responseObj["message"]?.ToString() ?? "Unknown error";
                            var errorCode = responseObj["error_code"]?.ToString();
                            
                            if (errorCode == "MISSING_OPENAI_PACKAGE")
                            {
                                throw new InvalidOperationException(
                                    "OpenAI package not installed.\n\n" +
                                    "Please run: pip install openai\n" +
                                    "Then restart AiStudio4.");
                            }
                            
                            throw new Exception($"Python bridge error: {errorMessage}");
                    }
                }

                EndProcessing:
                options.OnStreamingComplete?.Invoke();

                return new AiResponse
                {
                    Success = true,
                    ContentBlocks = new List<ContentBlock> 
                    { 
                        new ContentBlock 
                        { 
                            Content = responseBuilder.ToString(), 
                            ContentType = ContentType.Text 
                        } 
                    },
                    TokenUsage = tokenUsage,
                    ChosenTool = toolCalls.Any() ? "function_calls" : null
                };

            }
            catch (Exception ex)
            {
                options.OnStreamingComplete?.Invoke();
                throw;
            }
        }

        private async Task<AiResponse> HandleToolCalls(AiRequestOptions options, string assistantMessage, 
                                                      List<object> toolCalls, TokenUsage tokenUsage)
        {
            // Add the assistant's message with tool calls to the conversation
            var assistantMsg = new LinearConvMessage
            {
                role = "assistant",
                content = assistantMessage
            };
            options.Conv.messages.Add(assistantMsg);

            // Process each tool call
            foreach (var toolCallObj in toolCalls)
            {
                var toolCall = JObject.FromObject(toolCallObj);
                var toolCallId = toolCall["id"]?.ToString();
                var functionName = toolCall["function"]?["name"]?.ToString();
                var argumentsJson = toolCall["function"]?["arguments"]?.ToString();

                try
                {
                    // Execute the tool using existing infrastructure
                    var toolResult = await ExecuteTool(functionName, argumentsJson);
                    
                    // Add tool result to conversation
                    var toolResultMsg = new LinearConvMessage
                    {
                        role = "tool",
                        content = toolResult
                    };
                    options.Conv.messages.Add(toolResultMsg);
                }
                catch (Exception ex)
                {
                    var errorMsg = new LinearConvMessage
                    {
                        role = "tool",
                        content = $"Error executing tool {functionName}: {ex.Message}"
                    };
                    options.Conv.messages.Add(errorMsg);
                }
            }

            // Make another request to get the final response
            return await FetchResponseInternal(options, forceNoTools: true);
        }

        private async Task<string> ExecuteTool(string functionName, string argumentsJson)
        {
            try
            {
                // Parse arguments to Dictionary<string, object>
                var argumentsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(argumentsJson) 
                                   ?? new Dictionary<string, object>();
                
                // Try to find and execute built-in tool
                var allTools = await ToolService.GetAllToolsAsync();
                var tool = allTools.FirstOrDefault(t => t.SchemaName == functionName);
                
                if (tool != null)
                {
                    // For built-in tools, we need to use the ToolProcessorService
                    // This is a simplified approach - in practice you might need more sophisticated tool execution
                    return $"Built-in tool '{functionName}' would be executed with args: {argumentsJson}";
                }
                
                // Try MCP tools
                var mcpServers = await McpService.GetAllServerDefinitionsAsync();
                foreach (var server in mcpServers.Where(s => s.IsEnabled))
                {
                    var serverTools = await McpService.ListToolsAsync(server.Id);
                    var mcpTool = serverTools.FirstOrDefault(t => t.Name == functionName);
                    
                    if (mcpTool != null)
                    {
                        var result = await McpService.CallToolAsync(server.Id, functionName, argumentsDict);
                        return result?.Content?.FirstOrDefault()?.Text ?? "MCP tool executed successfully";
                    }
                }
                
                return $"Tool '{functionName}' not found";
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }

        private async Task EnsurePythonProcessIsRunningAsync()
        {
            if (_processStarted && _pythonProcess != null && !_pythonProcess.HasExited)
            {
                // Process is already running, test if it's responsive
                if (await TestProcessResponsiveness())
                {
                    return; // Process is healthy
                }
                
                // Process is unresponsive, restart it
                Debug.WriteLine("Python process unresponsive, restarting...");
                StopPythonProcess();
            }

            await StartPythonProcessAsync();
        }

        private async Task<bool> TestProcessResponsiveness()
        {
            try
            {
                lock (_processLock)
                {
                    if (_processWriter == null || _pythonProcess?.HasExited == true)
                        return false;

                    _processWriter.WriteLine("PING");
                    _processWriter.Flush();
                }

                // Wait for pong response with timeout
                var timeoutTask = Task.Delay(5000);
                var responseTask = Task.Run(async () =>
                {
                    while (!_pythonProcess.StandardOutput.EndOfStream)
                    {
                        var line = await _pythonProcess.StandardOutput.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) continue;

                        var response = JObject.Parse(line);
                        if (response["type"]?.ToString() == "pong")
                            return true;
                    }
                    return false;
                });

                var completedTask = await Task.WhenAny(responseTask, timeoutTask);
                return completedTask == responseTask && await responseTask;
            }
            catch
            {
                return false;
            }
        }

        private async Task StartPythonProcessAsync()
        {
            string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Core", "Tools", "Python", "openai_python_bridge.py");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Python bridge script not found at: {scriptPath}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"-u \"{scriptPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            lock (_processLock)
            {
                _pythonProcess = new Process { StartInfo = startInfo };
                
                _pythonProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Debug.WriteLine($"[PythonOpenAI-Error] {args.Data}");
                    }
                };

                try
                {
                    _pythonProcess.Start();
                    _pythonProcess.BeginErrorReadLine();
                    _processWriter = new StreamWriter(_pythonProcess.StandardInput.BaseStream, new UTF8Encoding(false));
                    _processStarted = true;

                    Debug.WriteLine("Python bridge process started successfully");
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to start Python bridge process.\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        $"Please ensure Python is installed and accessible via the 'python' command.\n" +
                        $"Installation guide: {GetSetupInstructions()}");
                }
            }

            // Wait for ready signal
            try
            {
                var readyTask = Task.Run(async () =>
                {
                    while (!_pythonProcess.StandardOutput.EndOfStream)
                    {
                        var line = await _pythonProcess.StandardOutput.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) continue;

                        var response = JObject.Parse(line);
                        if (response["type"]?.ToString() == "ready")
                            return true;
                    }
                    return false;
                });

                var timeoutTask = Task.Delay(10000); // 10 second timeout
                var completedTask = await Task.WhenAny(readyTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    StopPythonProcess();
                    throw new TimeoutException("Python bridge did not respond within the timeout period");
                }

                var isReady = await readyTask;
                if (!isReady)
                {
                    StopPythonProcess();
                    throw new InvalidOperationException("Python bridge failed to start properly");
                }

                Debug.WriteLine("Python bridge is ready and responsive");
            }
            catch (Exception ex)
            {
                StopPythonProcess();
                throw new InvalidOperationException($"Failed to initialize Python bridge: {ex.Message}");
            }
        }

        private void StopPythonProcess()
        {
            lock (_processLock)
            {
                try
                {
                    if (_processWriter != null)
                    {
                        _processWriter.WriteLine("EXIT");
                        _processWriter.Flush();
                        _processWriter.Close();
                        _processWriter = null;
                    }

                    if (_pythonProcess != null && !_pythonProcess.HasExited)
                    {
                        _pythonProcess.Kill();
                        _pythonProcess.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping Python process: {ex.Message}");
                }
                finally
                {
                    _pythonProcess?.Dispose();
                    _pythonProcess = null;
                    _processStarted = false;
                }
            }
        }

        private static Dictionary<string, object> ParseAdditionalParams(string additionalParams)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(additionalParams)) return result;

            var pairs = additionalParams.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // Try to parse as number
                    if (int.TryParse(value, out int intValue))
                    {
                        result[key] = intValue;
                    }
                    else if (double.TryParse(value, out double doubleValue))
                    {
                        result[key] = doubleValue;
                    }
                    else
                    {
                        result[key] = value;
                    }
                }
            }
            return result;
        }

        private static bool ShouldUseWebSearch(string modelName)
        {
            return modelName.Contains("search", StringComparison.OrdinalIgnoreCase) || 
                   modelName.Contains("gpt-4o", StringComparison.OrdinalIgnoreCase);
        }

        protected override ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI;
        }

        // Required abstract method implementations (not used in Python bridge)
        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            return new JObject();
        }

        protected override Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken, 
                                                                   Action<string> onStreamingUpdate, Action onStreamingComplete)
        {
            throw new NotImplementedException("Python bridge handles streaming internally");
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            StopPythonProcess();
            _isDisposed = true;
        }
    }
}