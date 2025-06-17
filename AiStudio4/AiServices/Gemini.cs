using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using AiStudio4.InjectedDependencies;
using Azure.Core;


using SharedClasses.Providers;


using System.Net.Http;

using System.Text.RegularExpressions;

namespace AiStudio4.AiServices
{
    
    internal class GenImage
    {
        public string MimeType { get; set; }
        public string Base64Data { get; set; }
    }

    internal class Gemini : AiServiceBase
    {
        private readonly List<GenImage> _generatedImages = new List<GenImage>();
        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        public Gemini()
        {
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            
            _generatedImages.Clear();
            
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);

            
            if (options.Model.IsTtsModel)
            {
                return await HandleTtsRequestAsync(options);
            }

            var url = $"{ApiUrl}{ApiModel}:streamGenerateContent?key={ApiKey}";

            
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conv, options.ApiSettings);

            
            // Add tools to request if not forcing no tools
            // Special handling for GEMINI_INTERNAL_GOOGLE_SEARCH directive
            bool hasGoogleSearchDirective = options.ToolIds?.Contains("GEMINI_INTERNAL_GOOGLE_SEARCH") == true;
            
            if (!forceNoTools)
            {
                if (hasGoogleSearchDirective)
                {
                    // When GEMINI_INTERNAL_GOOGLE_SEARCH is specified, add Google Search tool
                    
                    // Add Google Search tool to the request
                    requestPayload["tools"] = new JArray
                    {
                        new JObject
                        {
                            ["google_search"] = new JObject()
                        }
                    };
                }
                else
                {
                    await AddToolsToRequestAsync(requestPayload, options.ToolIds);
                }
            }

            
            var contentsArray = new JArray();
            foreach (var message in options.Conv.messages)
            {
                var messageObj = CreateMessageObject(message);
                contentsArray.Add(messageObj);
            }




            
            if (ApiModel == "gemini-2.0-flash-exp-image-generation")
            {
                requestPayload["generationConfig"] = new JObject
                {
                    ["responseModalities"] = new JArray { "Text", "Image" },
                    ["temperature"] = options.ApiSettings.Temperature
                };
                // Add TopP from options.TopP (which was populated from ApiSettings)
                // The value in options.TopP should be pre-validated (0.0 to 1.0).
                if (options.Model.AllowsTopP && options.TopP.HasValue && options.TopP.Value > 0.0f && options.TopP.Value <= 1.0f)
                {
                    ((JObject)requestPayload["generationConfig"])["topP"] = options.TopP.Value;
                }
            }
            else
            {
                requestPayload["system_instruction"] = new JObject
                {
                    ["parts"] = new JObject
                    {
                        ["text"] = options.Conv.SystemPromptWithDateTime()
                    }
                };
                requestPayload["generationConfig"] = new JObject
                {
                    ["temperature"] = options.ApiSettings.Temperature
                };
                // Add TopP from options.TopP (which was populated from ApiSettings)
                // The value in options.TopP should be pre-validated (0.0 to 1.0).
                if (options.Model.AllowsTopP && options.TopP.HasValue && options.TopP.Value > 0.0f && options.TopP.Value <= 1.0f)
                {
                    ((JObject)requestPayload["generationConfig"])["topP"] = options.TopP.Value;
                }
            }



            if (requestPayload["contents"] != null)
            {
                requestPayload.Remove("contents");
            }

            
            requestPayload.Add("contents", contentsArray);

            
            
            
            
            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            {
                return await HandleResponse(options, content); 
            }
        }

        public override async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);

            // Handle TTS models
            if (options.Model.IsTtsModel)
            {
                return await HandleTtsRequestAsync(options);
            }

            var linearConv = options.Conv;
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                linearConv.systemprompt = options.CustomSystemPrompt;
            
            var maxIterations = options.MaxToolIterations ?? 10;
            
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // 1. Reset tool response set for this iteration
                ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
                _generatedImages.Clear();

                // 2. Prepare the request payload with all available tools
                var requestPayload = CreateRequestPayload(ApiModel, linearConv, options.ApiSettings);
                
                // Special handling for GEMINI_INTERNAL_GOOGLE_SEARCH directive
                bool hasGoogleSearchDirective = options.ToolIds?.Contains("GEMINI_INTERNAL_GOOGLE_SEARCH") == true;
                
                // Add all available tools to the request
                var availableTools = await toolExecutor.GetAvailableToolsAsync(options.ToolIds);
                if (availableTools.Any() || hasGoogleSearchDirective)
                {
                    if (hasGoogleSearchDirective)
                    {
                        // Add Google Search tool to the request
                        requestPayload["tools"] = new JArray
                        {
                            new JObject
                            {
                                ["google_search"] = new JObject()
                            }
                        };
                    }
                    else
                    {
                        await AddToolsToRequestAsync(requestPayload, options.ToolIds);
                    }
                }

                // Build contents array
                var contentsArray = new JArray();
                foreach (var message in linearConv.messages)
                {
                    var messageObj = CreateMessageObject(message);
                    contentsArray.Add(messageObj);
                }

                // Configure generation settings
                if (ApiModel == "gemini-2.0-flash-exp-image-generation")
                {
                    requestPayload["generationConfig"] = new JObject
                    {
                        ["responseModalities"] = new JArray { "Text", "Image" },
                        ["temperature"] = options.ApiSettings.Temperature
                    };
                    if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
                    {
                        ((JObject)requestPayload["generationConfig"])["topP"] = options.ApiSettings.TopP;
                    }
                }
                else
                {
                    requestPayload["system_instruction"] = new JObject
                    {
                        ["parts"] = new JObject
                        {
                            ["text"] = linearConv.SystemPromptWithDateTime()
                        }
                    };
                    requestPayload["generationConfig"] = new JObject
                    {
                        ["temperature"] = options.ApiSettings.Temperature
                    };
                    if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
                    {
                        ((JObject)requestPayload["generationConfig"])["topP"] = options.ApiSettings.TopP;
                    }
                }

                if (requestPayload["contents"] != null)
                {
                    requestPayload.Remove("contents");
                }
                requestPayload.Add("contents", contentsArray);

                var jsonPayload = JsonConvert.SerializeObject(requestPayload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // 3. Call the Gemini API
                var response = await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);

                // 4. Check for final answer (no tool calls)
                if (response.ToolResponseSet == null || !response.ToolResponseSet.Tools.Any())
                {
                    // This is the final response with no tool calls - add it to branched conversation
                    if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
                    {
                        Debug.WriteLine($"🤖 GEMINI: Creating FINAL ASSISTANT message (no tools) - MessageId: {options.AssistantMessageId}, ParentId: {options.ParentMessageId}, ContentBlocks: {response.ContentBlocks?.Count ?? 0}");
                        
                        var message = options.BranchedConversation.AddOrUpdateMessage(
                            v4BranchedConvMessageRole.Assistant,
                            options.AssistantMessageId,
                            response.ContentBlocks,
                            options.ParentMessageId,
                            response.Attachments);
                        
                        await options.OnAssistantMessageCreated(message);
                    }
                    
                    return response; // We're done, return the final text response
                }

                // 4a. Build complete content blocks including tool calls
                var contentBlocks = new List<ContentBlock>();
                
                // Add any existing content blocks first
                if (response.ContentBlocks != null)
                {
                    contentBlocks.AddRange(response.ContentBlocks);
                }
                
                // Add tool call blocks for Gemini format
                foreach (var toolCall in response.ToolResponseSet.Tools)
                {
                    contentBlocks.Add(new ContentBlock
                    {
                        ContentType = Core.Models.ContentType.Tool,
                        Content = JsonConvert.SerializeObject(new
                        {
                            toolName = toolCall.ToolName,
                            parameters = toolCall.ResponseText
                        })
                    });
                }
                
                // Notify about assistant message with tool calls
                if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
                {
                    Debug.WriteLine($"🤖 GEMINI: Creating ASSISTANT message - MessageId: {options.AssistantMessageId}, ParentId: {options.ParentMessageId}, ContentBlocks: {contentBlocks.Count}");
                    
                    var message = options.BranchedConversation.AddOrUpdateMessage(
                        v4BranchedConvMessageRole.Assistant,
                        options.AssistantMessageId,
                        contentBlocks,
                        options.ParentMessageId,
                        response.Attachments);
                    
                    await options.OnAssistantMessageCreated(message);
                }

                // 4b. Notify about tool calls
                if (options.OnToolCallsGenerated != null)
                {
                    await options.OnToolCallsGenerated(
                        options.AssistantMessageId,
                        contentBlocks,
                        response.ToolResponseSet.Tools);
                }

                // 5. Add the AI's response with tool calls to conversation history in Gemini format
                var assistantParts = new JArray();
                
                // Add any text content first
                var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == Core.Models.ContentType.Text)?.Content;
                if (!string.IsNullOrEmpty(textContent))
                {
                    assistantParts.Add(new JObject 
                    { 
                        ["text"] = textContent 
                    });
                }

                // Add function call parts in Gemini format
                foreach (var toolCall in response.ToolResponseSet.Tools)
                {
                    assistantParts.Add(new JObject
                    {
                        ["functionCall"] = new JObject
                        {
                            ["name"] = toolCall.ToolName,
                            ["args"] = JObject.Parse(toolCall.ResponseText)
                        }
                    });
                }

                linearConv.messages.Add(new LinearConvMessage
                {
                    role = "model",
                    content = assistantParts.ToString()
                });

                // 6. Execute tools and collect results
                var functionResponseParts = new JArray();
                var shouldStopLoop = false;

                foreach (var toolCall in response.ToolResponseSet.Tools)
                {
                    var context = new Core.Interfaces.ToolExecutionContext
                    {
                        ClientId = clientId,
                        CancellationToken = options.CancellationToken,
                        BranchedConversation = options.BranchedConversation,
                        LinearConversation = linearConv,
                        CurrentIteration = iteration,
                        AssistantMessageId = options.AssistantMessageId,
                        ParentMessageId = options.ParentMessageId
                    };

                    var executionResult = await toolExecutor.ExecuteToolAsync(toolCall.ToolName, toolCall.ResponseText, context);

                    // Notify about tool execution
                    if (options.OnToolExecuted != null)
                    {
                        await options.OnToolExecuted(options.AssistantMessageId, toolCall.ToolName, executionResult);
                    }
                    
                    // Check if tool execution indicates we should stop the loop
                    if (!executionResult.ContinueProcessing || executionResult.UserInterjection != null)
                    {
                        shouldStopLoop = true;
                        
                        // If there's a user interjection, add it to the conversation
                        if (executionResult.UserInterjection != null)
                        {
                            var interjectionId = Guid.NewGuid().ToString();
                            
                            // Notify about interjection
                            if (options.OnUserInterjection != null)
                            {
                                await options.OnUserInterjection(interjectionId, executionResult.UserInterjection);
                            }
                            
                            // Add to linear conversation in Gemini format
                            linearConv.messages.Add(new LinearConvMessage
                            {
                                role = "user",
                                content = new JArray { new JObject { ["text"] = executionResult.UserInterjection } }.ToString()
                            });
                            
                            // Update parent for next iteration
                            if (options.BranchedConversation != null)
                            {
                                options.ParentMessageId = interjectionId;
                                options.AssistantMessageId = Guid.NewGuid().ToString();
                            }
                            
                            break; // Don't execute remaining tools, process the interjection
                        }
                    }

                    // Add function response in Gemini format
                    functionResponseParts.Add(new JObject
                    {
                        ["functionResponse"] = new JObject
                        {
                            ["name"] = toolCall.ToolName,
                            ["response"] = new JObject
                            {
                                ["content"] = executionResult.ResultMessage,
                                ["success"] = executionResult.WasProcessed
                            }
                        }
                    });
                }
                
                // 7. Add tool results to both linear and branched conversations
                if (functionResponseParts.Any())
                {
                    // Add to linear conversation for API in Gemini format
                    linearConv.messages.Add(new LinearConvMessage
                    {
                        role = "user",
                        content = functionResponseParts.ToString()
                    });
                    
                    // Add to branched conversation as a user message containing tool results
                    if (options.BranchedConversation != null)
                    {
                        var toolResultBlocks = new List<ContentBlock>();
                        
                        foreach (var toolResult in functionResponseParts)
                        {
                            var functionResponse = toolResult["functionResponse"];
                            var toolName = functionResponse["name"]?.ToString() ?? "Unknown Tool";
                            var result = functionResponse["response"]["content"]?.ToString();
                            var success = (bool)(functionResponse["response"]["success"] ?? false);
                            
                            toolResultBlocks.Add(new ContentBlock
                            {
                                ContentType = Core.Models.ContentType.ToolResponse,
                                Content = JsonConvert.SerializeObject(new
                                {
                                    toolName = toolName,
                                    result = result,
                                    success = success
                                })
                            });
                        }
                        
                        // Create a user message with tool results
                        var toolResultMessageId = Guid.NewGuid().ToString();
                        Debug.WriteLine($"👤 GEMINI: Creating USER message (tool results) - MessageId: {toolResultMessageId}, ParentId: {options.AssistantMessageId}, ToolResults: {toolResultBlocks.Count}");
                        
                        var toolResultMessage = options.BranchedConversation.AddOrUpdateMessage(
                            v4BranchedConvMessageRole.User,
                            toolResultMessageId,
                            toolResultBlocks,
                            options.AssistantMessageId); // Parent is the assistant message that made the tool calls

                        // Notify client about the tool result message via the proper callback
                        if (options.OnUserMessageCreated != null)
                        {
                            await options.OnUserMessageCreated(toolResultMessage);
                        }
                        
                        // Store the tool result message ID to update parent for next iteration
                        options.ParentMessageId = toolResultMessageId;
                    }
                }

                // 8. Check if we should stop the loop
                if (shouldStopLoop)
                {
                    // Continue the loop to let the AI respond to the interjection or tool stop
                    return response; // We're done, return the final text response
                }
                
                // 9. Prepare for next iteration - generate new assistant message ID
                if (options.BranchedConversation != null)
                {
                    options.AssistantMessageId = Guid.NewGuid().ToString();
                }
            }

            // If we've exceeded max iterations, create error response and add to branched conversation
            var errorResponse = new AiResponse 
            { 
                Success = false, 
                ContentBlocks = new List<ContentBlock> 
                { 
                    new ContentBlock 
                    { 
                        Content = $"Exceeded maximum tool iterations ({maxIterations}). The AI may be stuck in a tool loop.",
                        ContentType = Core.Models.ContentType.Text 
                    } 
                } 
            };
            
            // Add error message to branched conversation
            if (options.OnAssistantMessageCreated != null && options.BranchedConversation != null)
            {
                Debug.WriteLine($"🤖 GEMINI: Creating ERROR ASSISTANT message (max iterations) - MessageId: {options.AssistantMessageId}, ParentId: {options.ParentMessageId}");
                
                var message = options.BranchedConversation.AddOrUpdateMessage(
                    v4BranchedConvMessageRole.Assistant,
                    options.AssistantMessageId,
                    errorResponse.ContentBlocks,
                    options.ParentMessageId,
                    errorResponse.Attachments);
                
                await options.OnAssistantMessageCreated(message);
            }
            
            return errorResponse;
        }

        protected override JObject CreateRequestPayload(
    string apiModel,
    LinearConv conv,
    ApiSettings apiSettings)
        {
            return new JObject
            {
                ["contents"] = new JArray()
            };
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            var partArray = new JArray();

            // Try to parse content as structured parts first (for tool calls/responses)
            try
            {
                var parsedContent = JArray.Parse(message.content);
                partArray = parsedContent;
            }
            catch
            {
                // If parsing fails, treat as plain text
                partArray.Add(new JObject { ["text"] = message.content });
            }

            // Add legacy single image if present
            if (!string.IsNullOrEmpty(message.base64image))
            {
                partArray.Add(new JObject
                {
                    ["inline_data"] = new JObject
                    {
                        ["mime_type"] = message.base64type,
                        ["data"] = message.base64image
                    }
                });
            }

            // Add multiple attachments if present
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                    {
                        partArray.Add(new JObject
                        {
                            ["inline_data"] = new JObject
                            {
                                ["mime_type"] = attachment.Type,
                                ["data"] = attachment.Content
                            }
                        });
                    }
                }
            }

            return new JObject
            {
                ["role"] = message.role == "assistant" ? "model" : message.role,
                ["parts"] = partArray
            };
        }



        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            
        }
        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            StringBuilder fullResponse = new StringBuilder(); 
            onStreamingUpdate?.Invoke(""); 
            try
            {
                string contentString = await content.ReadAsStringAsync();

                using (var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}{ApiModel}:streamGenerateContent?key={ApiKey}"))
                {
                    request.Content = content;
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {

                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            StringBuilder jsonBuffer = new StringBuilder();
                            bool isFirstLine = true;

                            while (!reader.EndOfStream)
                            {
                                string line = await reader.ReadLineAsync(cancellationToken);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    
                                    throw new OperationCanceledException(cancellationToken);
                                }
                                
                                
                                if (isFirstLine)
                                {
                                    
                                    line = line.TrimStart('[');
                                    isFirstLine = false;
                                }

                                jsonBuffer.Append(line);
                                if (line == "," || line == "]")
                                {
                                    
                                    string jsonObject = jsonBuffer.ToString().TrimEnd(',').TrimEnd(']');
                                    await ProcessJsonObject(jsonObject, fullResponse, onStreamingUpdate); 
                                    jsonBuffer.Clear();
                                }
                            }

                            
                        }
                    }
                }

                
                onStreamingComplete?.Invoke();
                
                // Enhanced error handling for any "Invalid JSON payload received" errors
                // Provides detailed information about the function and property causing the issue
                var responseText = fullResponse.ToString();
                if (responseText.Contains("Invalid JSON payload received"))
                {
                    // Extract function information from the error message - supports various error patterns
                    var match = Regex.Match(responseText, @"tools\[(\d+)\]\.function_declarations\[(\d+)\]\.parameters\.properties\[(\d+)\]\.([^'\s:]+)");
                    if (match.Success)
                    {
                        
                        var toolIndex = match.Groups[1].Value;
                        var functionIndex = match.Groups[2].Value;
                        var propertyIndex = match.Groups[3].Value;
                        var propertyPath = match.Groups[4].Value;
                        
                        var enhancedError = $"{responseText}\n\n" +
                            $"DEBUGGING INFO:\n" +
                            $"- Tool Index: {toolIndex}\n" +
                            $"- Function Declaration Index: {functionIndex}\n" +
                            $"- Property Index: {propertyIndex}\n" +
                            $"- Property Path: {propertyPath}\n" +
                            $"- Common causes: Invalid 'type' field (array instead of string), unsupported schema keywords, malformed property definitions.\n" +
                            $"- Check the function definition for property at index {propertyIndex} for schema validation issues.\n" +
                            $"- The problematic function is at position {functionIndex} in the tools array.";

                        // Parse contentString to get actual function details
                        try
                        {
                            var requestJson = JObject.Parse(contentString);
                            var tools = requestJson["tools"] as JArray;
                            
                            if (tools != null && int.TryParse(toolIndex, out int tIdx) && tIdx < tools.Count)
                            {
                                var tool = tools[tIdx];
                                var functionDeclarations = tool["function_declarations"] as JArray;
                                
                                if (functionDeclarations != null && int.TryParse(functionIndex, out int fIdx) && fIdx < functionDeclarations.Count)
                                {
                                    var functionDecl = functionDeclarations[fIdx];
                                    var functionName = functionDecl["name"]?.ToString();
                                    var functionDescription = functionDecl["description"]?.ToString();
                                    
                                    // Navigate to the problematic property
                                    var parameters = functionDecl["parameters"];
                                    var properties = parameters?["properties"] as JObject;
                                    
                                    if (properties != null && int.TryParse(propertyIndex, out int pIdx))
                                    {
                                        var propertyNames = properties.Properties().ToArray();
                                        if (pIdx < propertyNames.Length)
                                        {
                                            var problematicProperty = propertyNames[pIdx];
                                            var propertyName = problematicProperty.Name;
                                            var propertyValue = problematicProperty.Value;
                                            
                                            enhancedError += $"\n\n" +
                                                $"FUNCTION DETAILS:\n" +
                                                $"- Function Name: {functionName}\n" +
                                                $"- Function Description: {(functionDescription?.Length > 100 ? functionDescription.Substring(0, 100) + "..." : functionDescription)}\n" +
                                                $"- Problematic Property Name: {propertyName}\n" +
                                                $"- Problematic Property Definition: {propertyValue?.ToString()}\n" +
                                                $"- Property Path in Schema: parameters.properties.{propertyName}.{propertyPath}";
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            enhancedError += $"\n\nFailed to parse request JSON for additional details: {parseEx.Message}";
                        }

                        // contentstring example:
                        // {"tools":[{"function_declarations":[{"name":"DirectoryTree","description":"Get a recursive tree view of files and directories with customizable depth and filtering.\r\n\r\nReturns a structured view of the directory tree with files and subdirectories. Directories are marked with trailing slashes. The output is formatted as an indented list for readability. By default, common development directories like .git, node_modules, and venv are noted but not traversed unless explicitly requested. Only works within allowed directories.","parameters":{"properties":{"path":{"title":"Path","type":"string","description":"The path to the directory to view"},"depth":{"title":"Depth","type":"integer","description":"The maximum depth to traverse (0 for unlimited)"}},"required":["path"],"title":"DirectoryTreeArguments","type":"object"}}, ...

                        fullResponse.Clear();
                        fullResponse.Append(enhancedError);
                        Debug.WriteLine($"Enhanced Gemini error with debugging info: {enhancedError}");
                    }
                }

                Debug.WriteLine("Streaming Complete");

                if (ToolResponseSet.Tools.Count == 0)
                {
                    var json = ExtractTrailingJsonObject(fullResponse.ToString());
                    if (json != null)
                    {
                        var jsonResponse = JsonConvert.DeserializeObject<JObject>(json);

                        if (jsonResponse["args"] != null)
                        {
                            Debug.WriteLine("Args are not null");

                            var toolName = jsonResponse["name"]?.ToString();
                            var toolArgs = jsonResponse["args"].ToString();
                            ToolResponseSet.Tools.Add(new ToolResponseItem { ToolName = toolName, ResponseText = toolArgs });

                            
                            
                        }
                    }
                }

                try
                {
                    var jsonResponse = JsonConvert.DeserializeObject<JObject>(fullResponse.ToString());

                    if (jsonResponse != null && jsonResponse["args"] != null)
                    {
                        Debug.WriteLine("Args are not null");

                        var toolName = jsonResponse["name"]?.ToString();
                        var toolArgs = jsonResponse["args"].ToString();

                        currentResponseItem = null;

                        Debug.WriteLine($"Returning with {ToolResponseSet.Tools.Count} tools in the tool response set... (1)");

                        return new AiResponse
                        {
                            ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = toolArgs, ContentType = Core.Models.ContentType.Text } },
                            Success = true,
                            TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount),
                            ChosenTool = toolName,
                            ToolResponseSet = ToolResponseSet,
                            IsCancelled = false 
                        };
                    }
                }
                catch (Exception e)
                {
                    
                }

                
                var attachments = new List<DataModels.Attachment>();
                int imageIndex = 1;
                foreach (var image in _generatedImages)
                {
                    attachments.Add(new DataModels.Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"generated_image_{imageIndex++}.png",
                        Type = image.MimeType,
                        Content = image.Base64Data,
                        Size = image.Base64Data.Length * 3 / 4 
                    });
                }
                currentResponseItem = null;

                Debug.WriteLine($"Returning with {ToolResponseSet.Tools.Count} tools in the tool response set: {string.Join(",", ToolResponseSet.Tools.Select(x => x.ToolName))}... (2)");
                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = fullResponse.ToString(), ContentType = Core.Models.ContentType.Text } },
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount ?? "0"),
                    ChosenTool = null,
                    Attachments = attachments.Count > 0 ? attachments : null,
                    ToolResponseSet = ToolResponseSet,
                    IsCancelled = false 
                };
            }
            catch (OperationCanceledException)
            {
                
                
                
                Debug.WriteLine("Cancelled. ");
                var attachments = new List<DataModels.Attachment>();
                int imageIndex = 1;
                foreach (var image in _generatedImages)
                {
                    attachments.Add(new DataModels.Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"generated_image_{imageIndex++}.png",
                        Type = image.MimeType,
                        Content = image.Base64Data,
                        Size = image.Base64Data.Length * 3 / 4 
                    });
                }
                currentResponseItem = null;
                
                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = fullResponse.ToString(), ContentType = Core.Models.ContentType.Text } },
                    Success = true, 
                    TokenUsage = new TokenUsage(inputTokenCount ?? "0", outputTokenCount ?? "0", "0", cachedTokenCount ?? "0"),
                    ChosenTool = chosenTool, 
                    Attachments = attachments.Count > 0 ? attachments : null,
                    ToolResponseSet = ToolResponseSet, 
                    IsCancelled = true
                };
            }
            catch (Exception ex)
            {
                
                return HandleError(ex, "Error during streaming response");
            }
        }

        private AiResponse HandleError(Exception ex, string additionalInfo = "")
        {
            string errorMessage = $"Error: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMessage += $" Additional info: {additionalInfo}";
            }
            return new AiResponse { Success = false, ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = errorMessage, ContentType = Core.Models.ContentType.Text } } };
        }
        public static string ExtractTrailingJsonObject(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] == '}')
                {
                    
                    int depth = 1;
                    int j;

                    for (j = i - 1; j >= 0; j--)
                    {
                        if (input[j] == '}')
                            depth++;
                        else if (input[j] == '{')
                            depth--;

                        
                        if (depth == 0)
                            break;
                    }

                    
                    if (j >= 0)
                    {
                        string potentialJson = input.Substring(j);

                        
                        try
                        {
                            System.Text.Json.JsonDocument.Parse(potentialJson);
                            return potentialJson;
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            
                            i = j; 
                        }
                    }
                }
            }

            return null;
        }

        private string ExtractResponseText(JObject completion)
        {
            if (completion["candidates"]?[0]?["content"]?["parts"] is JArray parts)
            {
                StringBuilder textBuilder = new StringBuilder();

                foreach (var part in parts)
                {
                    
                    if (part["functionCall"] != null)
                    {
                        textBuilder.Append(JsonConvert.SerializeObject(part["functionCall"]));
                    }
                    
                    else if (part["text"] != null)
                    {
                        textBuilder.Append(part["text"]?.ToString() ?? "");
                    }
                    
                    else if (part["inlineData"] != null)
                    {
                        textBuilder.Append("[Generated Image]");
                    }
                }

                return textBuilder.ToString();
            }
            return completion.ToString();
        }

        private string ExtractChosenToolFromCompletion(JObject completion)
        {
            if (completion["candidates"]?[0]?["content"]?["parts"] != null)
            {
                var content = completion["candidates"][0]["content"]["parts"][0];
                if (content["functionCall"] != null)
                {
                    return content["functionCall"]["name"]?.ToString();
                }
            }
            return null;
        }



        private string inputTokenCount = "";
        private string outputTokenCount = "";
        private string cachedTokenCount = "";
        private string chosenTool = null;
        private ToolResponseItem currentResponseItem = null;
        private async Task<string> ProcessJsonObject(string jsonString, StringBuilder fullResponse, Action<string> onStreamingUpdate)
        {
            if (!string.IsNullOrWhiteSpace(jsonString))
            {
                try
                {
                    var streamData = JObject.Parse(jsonString);
                    if (streamData["candidates"] != null && streamData["candidates"][0]["content"]["parts"] != null)
                    {
                        var parts = streamData["candidates"][0]["content"]["parts"] as JArray;

                        foreach (var part in parts)
                        {
                            
                            if (part["functionCall"] != null)
                            {
                                var toolResponse = JsonConvert.SerializeObject(part["functionCall"]);
                                var toolName = part["functionCall"]["name"]?.ToString();
                                var toolArgs = part["functionCall"]["args"]?.ToString() ?? "{}";

                                chosenTool = toolName;

                                Debug.WriteLine($"Tool chosen: {chosenTool}");

                                
                                
                                
                                Debug.WriteLine($"new ToolResponseItem: {chosenTool} -> {toolArgs}");
                                currentResponseItem = new ToolResponseItem
                                {
                                    ToolName = toolName,
                                    ResponseText = toolArgs
                                };

                                
                                onStreamingUpdate?.Invoke($"\n\nTool selected: {toolName}\n\n"); 

                                ToolResponseSet.Tools.Add(currentResponseItem);
                                
                                
                                
                                
                                

                                
                                
                                onStreamingUpdate?.Invoke(toolResponse); 
                            }
                            
                            else if (part["text"] != null)
                            {
                                var textChunk = part["text"]?.ToString();
                                if (!string.IsNullOrEmpty(textChunk))
                                {
                                    if (fullResponse.Length != 0 || (textChunk != "ny"))
                                    {
                                        fullResponse.Append(textChunk);
                                        onStreamingUpdate?.Invoke(textChunk); 
                                        Debug.WriteLine($"text: {textChunk}");
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                            
                            else if (part["inlineData"] != null)
                            {
                                
                                string mimeType = part["inlineData"]["mimeType"]?.ToString();
                                string base64Data = part["inlineData"]["data"]?.ToString();

                                if (!string.IsNullOrEmpty(mimeType) && !string.IsNullOrEmpty(base64Data))
                                {
                                    
                                    _generatedImages.Add(new GenImage
                                    {
                                        MimeType = mimeType,
                                        Base64Data = base64Data
                                    });

                                    
                                    var imagePlaceholder = "[Generated Image]";
                                    fullResponse.Append(imagePlaceholder);
                                    onStreamingUpdate?.Invoke(imagePlaceholder); 
                                }
                            }
                            else
                            {

                            }
                        }
                    }

                    if (streamData["usageMetadata"] != null)
                    {
                        Debug.WriteLine(streamData["usageMetadata"].ToString());
                        inputTokenCount = ((int)(streamData["usageMetadata"]?["promptTokenCount"] ?? 0) + (int)(streamData["usageMetadata"]?["thoughtsTokenCount"] ?? 0)).ToString();
                        outputTokenCount = streamData["usageMetadata"]?["candidatesTokenCount"]?.ToString();

                        
                        
                        cachedTokenCount = streamData["usageMetadata"]?["cachedContentTokenCount"]?.ToString();
                    }

                    if (streamData["error"] != null)
                    {
                        fullResponse.Append(streamData["error"]["message"]);
                    }
                }
                catch (JsonReaderException ex)
                {
                    return jsonString;
                }
            }
            return "";
        }

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            var inputTokens = response["usageMetadata"]?["promptTokenCount"]?.ToString();
            var outputTokens = response["usageMetadata"]?["candidatesTokenCount"]?.ToString();
            var cachedTokens = response["usageMetadata"]?["cachedContentTokenCount"]?.ToString();
            return new TokenUsage(inputTokens, outputTokens, "0", cachedTokens);
        }

        protected override ToolFormat GetToolFormat()
        {
            return ToolFormat.Gemini;
        }

        private void RemoveAllOfAnyOfOneOf(JObject obj)
        {
            if (obj == null) return;

            var propertiesToRemove = new List<string>();
            foreach (var property in obj.Properties())
            {
                if (property.Name == "allOf" || property.Name == "anyOf" || property.Name == "oneOf")
                {
                    propertiesToRemove.Add(property.Name);
                }
                else if (property.Value is JObject)
                {
                    RemoveAllOfAnyOfOneOf((JObject)property.Value);
                }
                else if (property.Value is JArray)
                {
                    foreach (var item in (JArray)property.Value)
                    {
                        if (item is JObject)
                        {
                            RemoveAllOfAnyOfOneOf((JObject)item);
                        }
                    }
                }
            }

            foreach (var prop in propertiesToRemove)
            {
                obj.Remove(prop);
            }
        }


        private async Task<AiResponse> HandleTtsRequestAsync(AiRequestOptions options)
        {
            string textToSynthesize = options.Conv?.messages?.LastOrDefault(m => m.role == "user")?.content;
            if (string.IsNullOrEmpty(textToSynthesize))
            {
                return new AiResponse { Success = false, ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = "No text provided for speech synthesis in the last user message.", ContentType = Core.Models.ContentType.Text } } };
            }

            string voiceName = !string.IsNullOrEmpty(options.Model.TtsVoiceName) ? options.Model.TtsVoiceName : "Kore"; 

            
            string ttsUrl = $"{ApiUrl}{ApiModel}:generateContent?key={ApiKey}";

            var ttsRequestPayload = new JObject
            {
                ["contents"] = new JArray { new JObject { 
                    ["role"] = "user",
                    ["parts"] = new JArray { new JObject { ["text"] = textToSynthesize } } 
                } },
                ["generationConfig"] = new JObject {
                    ["responseModalities"] = new JArray { "audio" },
                    ["temperature"] = 1,
                    ["speech_config"] = new JObject {
                        ["voice_config"] = new JObject {
                            ["prebuilt_voice_config"] = new JObject { ["voice_name"] = voiceName }
                        }
                    }
                }
            };

            var jsonPayload = JsonConvert.SerializeObject(ttsRequestPayload);
            using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            {
                try
                {
                    HttpResponseMessage httpResponse = await client.PostAsync(ttsUrl, content, options.CancellationToken);
                    string responseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Gemini TTS API request failed: {httpResponse.StatusCode} - {responseBody}");
                    }

                    var responseObject = JObject.Parse(responseBody);
                    var inlineData = responseObject?["candidates"]?[0]?["content"]?["parts"]?[0]?["inlineData"];
                    string base64Audio = inlineData?["data"]?.ToString();
                    string mimeType = inlineData?["mimeType"]?.ToString() ?? "audio/wav";

                    if (string.IsNullOrEmpty(base64Audio))
                    {
                        throw new Exception("No audio data found in Gemini TTS response.");
                    }

                    var audioBytes = Convert.FromBase64String(base64Audio);

                    
                    string debugDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AiStudio4", "DebugLogs", "AudioFiles");
                    try
                    {
                        
                        if (!Directory.Exists(debugDir))
                        {
                            Directory.CreateDirectory(debugDir);
                        }
                        
                        
                        string debugFileName = $"speech_{DateTime.Now:yyyyMMddHHmmss}.wav";
                        string debugFilePath = Path.Combine(debugDir, debugFileName);
                        File.WriteAllBytes(debugFilePath, audioBytes);
                        Debug.WriteLine($"Audio debug file written to: {debugFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to write audio debug file: {ex.Message}");
                    }

                    
                    
                    int sampleRate = 24000; 
                    short bitsPerSample = 16; 
                    short numChannels = 1;   

                    byte[] wavFileData = PrependWavHeader(audioBytes, numChannels, sampleRate, bitsPerSample);
                    string finalBase64Audio = Convert.ToBase64String(wavFileData);

                    
                    try
                    {
                        string wavDebugFilePath = Path.Combine(debugDir, $"speech_wav_{DateTime.Now:yyyyMMddHHmmss}.wav");
                        File.WriteAllBytes(wavDebugFilePath, wavFileData);
                        Debug.WriteLine($"WAV audio debug file written to: {wavDebugFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to write WAV audio debug file: {ex.Message}");
                    }

                    var attachment = new Attachment
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = $"speech_{DateTime.Now:yyyyMMddHHmmss}.wav",
                        Type = "audio/wav", 
                        Content = finalBase64Audio, 
                        Size = wavFileData.Length
                    };

                    options.OnStreamingComplete?.Invoke();

                    if (responseObject["usageMetadata"] != null)
                    {
                        Debug.WriteLine(responseObject["usageMetadata"].ToString());
                        inputTokenCount = ((int)(responseObject["usageMetadata"]?["promptTokenCount"] ?? 0) + (int)(responseObject["usageMetadata"]?["thoughtsTokenCount"] ?? 0)).ToString();
                        outputTokenCount = responseObject["usageMetadata"]?["candidatesTokenCount"]?.ToString();
                    }

                    return new AiResponse
                    {
                        Success = true,
                        ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = $"Audio generated for: \"{textToSynthesize.Substring(0, Math.Min(textToSynthesize.Length, 50))}...\"", ContentType = Core.Models.ContentType.Text } },
                        Attachments = new List<Attachment> { attachment },
                        TokenUsage = new TokenUsage(inputTokenCount.ToString(), outputTokenCount.ToString()), 
                    };
                }
                catch (Exception ex)
                {
                    return new AiResponse
                    {
                        Success = false,
                        ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = $"TTS Error: {ex.Message}", ContentType = Core.Models.ContentType.Text } }
                    };
                }
            }
        }

        
        private static byte[] PrependWavHeader(byte[] pcmData, short numChannels, int sampleRate, short bitsPerSample)
        {
            int headerSize = 44;
            int totalFileSize = pcmData.Length + headerSize;
            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
            short blockAlign = (short)(numChannels * (bitsPerSample / 8));

            using (MemoryStream ms = new MemoryStream(totalFileSize))
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); 
                writer.Write(totalFileSize - 8);              
                writer.Write(Encoding.ASCII.GetBytes("WAVE")); 

                
                writer.Write(Encoding.ASCII.GetBytes("fmt ")); 
                writer.Write(16);                              
                writer.Write((short)1);                        
                writer.Write(numChannels);                     
                writer.Write(sampleRate);                      
                writer.Write(byteRate);                        
                writer.Write(blockAlign);                      
                writer.Write(bitsPerSample);                   

                
                writer.Write(Encoding.ASCII.GetBytes("data")); 
                writer.Write(pcmData.Length);                  
                writer.Write(pcmData);                         

                return ms.ToArray();
            }
        }
    }
}
