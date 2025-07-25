

using System.Net.Http.Headers;

using System.Text.Json;
using System.Text.RegularExpressions;
using SharedClasses.Providers;
using AiStudio4.DataModels;
using System.Net.Http;
using AiStudio4.Convs;



using System.Threading;

using AiStudio4.Core.Tools; // added for tool support


namespace AiStudio4.AiServices
{
    internal class OpenAI : AiServiceBase
    {
        private bool deepseekBodge; // Field to store the name of the tool chosen via tool_calls (if any) private string chosenTool;

        public OpenAI() { }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);
            deepseekBodge = ApiUrl.Contains("deepseek");

            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conv, options.UseStreaming, options.ApiSettings);

            // Create system message
            var systemMessage = new JObject
            {
                ["role"] = "system",
                ["content"] = deepseekBodge
                    ? options.Conv.SystemPromptWithDateTime()
                    : new JArray(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = options.Conv.SystemPromptWithDateTime()
                    })
            };

            var messagesArray = new JArray { systemMessage };

            // Add conversation messages
            foreach (var m in options.Conv.messages)
            {
                messagesArray.Add(CreateMessageObject(m));
            }
            requestPayload["messages"] = messagesArray;

            // Add tools into the request if any tool IDs were specified.
            if (!forceNoTools)
            {
                AddToolsToRequestAsync(requestPayload, options.ToolIds);
            }

            if (options.AddEmbeddings)
            {
                var lastMessage = options.Conv.messages.Last();
                var lastMessageContent = string.Join("\n\n", 
                    lastMessage.contentBlocks?.Where(b => b.ContentType == ContentType.Text)?.Select(b => b.Content) ?? new string[0]);
                var newInput = await AddEmbeddingsIfRequired(options.Conv, options.ApiSettings, options.MustNotUseEmbedding, options.AddEmbeddings, lastMessageContent);
                // Adjust the content structure based on the deepseek flag.
                if (deepseekBodge)
                {
                    ((JArray)requestPayload["messages"]).Last["content"] = newInput;
                }
                else
                {
                    var lastContentArray = ((JArray)requestPayload["messages"]).Last["content"] as JArray;
                    if (lastContentArray != null && lastContentArray.Count > 0)
                    {
                        lastContentArray.Last["text"] = newInput;
                    }
                }
            }

            var json = JsonConvert.SerializeObject(requestPayload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await HandleResponse(options, content); // Pass options
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            // The supportsLogprobs flag may be extended later if desired
            var supportsLogprobs = false;

            var additionalParamsList = AdditionalParams.Split(';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim());
            var additionalParamsDict = additionalParamsList.ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

            var payload = new JObject
            {
                ["model"] = modelName,
                ["stream"] = true,
                ["stream_options"] = useStreaming ? new JObject { ["include_usage"] = true } : null,
                //["reasoning_effort"] = "lolz"
            };

            foreach (var entry in additionalParamsDict)
            {
                payload[entry.Key] = entry.Value;
            }

            if (supportsLogprobs)
            {
                payload["logprobs"] = true;
                payload["top_logprobs"] = 5;
            }

            return payload;
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            var messageContent = new JArray();

            // Support for single image from base64image (legacy)
            if (!string.IsNullOrWhiteSpace(message.base64image))
            {
                messageContent.Add(new JObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = new JObject
                    {
                        ["url"] = $"data:{message.base64type};base64,{message.base64image}"
                    }
                });
            }
            
            // Support for multiple attachments
            if (message.attachments != null && message.attachments.Any())
            {
                foreach (var attachment in message.attachments)
                {
                    if (attachment.Type.StartsWith("image/") || attachment.Type == "application/pdf")
                    {
                        messageContent.Add(new JObject
                        {
                            ["type"] = "image_url",
                            ["image_url"] = new JObject
                            {
                                ["url"] = $"data:{attachment.Type};base64,{attachment.Content}"
                            }
                        });
                    }
                    // Additional attachment types could be handled here
                }
            }

            // Convert ContentBlocks to OpenAI format
            foreach (var block in message.contentBlocks ?? new List<ContentBlock>())
            {
                if (block.ContentType == ContentType.Text)
                {
                    messageContent.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = block.Content ?? ""
                    });
                }
                // Handle other content types as needed
            }

            // For deepseek, flatten to simple text
            if (deepseekBodge)
            {
                var textContent = string.Join("\n\n", 
                    message.contentBlocks?.Where(b => b.ContentType == ContentType.Text)?.Select(b => b.Content) ?? new string[0]);
                return new JObject
                {
                    ["role"] = message.role,
                    ["content"] = textContent
                };
            }

            return new JObject
            {
                ["role"] = message.role,
                ["content"] = messageContent
            };
        }

        // --------------------- TOOL SUPPORT ---------------------
        // In this override we mirror the pattern used by Claude.cs:
        // If any toolIDs are supplied, we initialize the "tools" array on the request and use the ToolRequestBuilder
        // to insert the tool details. The current GetToolFormat() method returns ToolFormat.OpenAI.
        protected override async Task  AddToolsToRequestAsync(JObject req, List<string> toolIDs)
        {
            if (req["tools"] == null)
                req["tools"] = new JArray();

            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);
            foreach (var toolId in toolIDs)
            {
                await toolRequestBuilder.AddToolToRequestAsync(req, toolId, GetToolFormat());
            }

            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(req, GetToolFormat());
        }

        protected override ToolFormat GetToolFormat() => ToolFormat.OpenAI;
        // --------------------- END TOOL SUPPORT ---------------------

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete)
        {
            using var response = await SendRequest(content, cancellationToken);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var responseBuilder = new StringBuilder();
            var buffer = new char[1024];
            int charsRead;

            int inputTokens = 0;
            int outputTokens = 0;
            string leftovers = null;
            // Reset chosenTool for this response
            ChosenTool = null;

            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

                var chunk = new string(buffer, 0, charsRead);
                var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    leftovers = ProcessLine($"{leftovers}{line}", responseBuilder, ref inputTokens, ref outputTokens, onStreamingUpdate); // Pass callback
                }
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                return HandleError(e, $"Response leftovers: {leftovers}");
            }

            onStreamingComplete?.Invoke(); // Use callback

            return new AiResponse
            {
                ResponseText = responseBuilder.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString()),
                ChosenTool = ChosenTool
            };
        }

        // In this method we update the response as streaming data is received.
        // In addition to appending text we check for the presence of a tool_call.
        private string ProcessLine(string line, StringBuilder responseBuilder, ref int inputTokens, ref int outputTokens, Action<string> onStreamingUpdate)
        {
            if (line.Length < 6)
                return line;

            if (line == ": OPENROUTER PROCESSING")
                return "";

            if (line.StartsWith("\r"))
                line = line.Substring(1);

            if (line.StartsWith("data: "))
            {
                var jsonData = line.Substring("data: ".Length).Trim();
                if (jsonData.Equals("[DONE]"))
                    return string.Empty;

                try
                {
                    // Validate JSON using System.Text.Json briefly
                    try
                    {
                        using (JsonDocument.Parse(jsonData)) { }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        return line;
                    }

                    var chunk = JsonConvert.DeserializeObject<JObject>(jsonData);

                    if (chunk["choices"] != null && chunk["choices"].Any())
                    {
                        var content = chunk["choices"]?[0]?["delta"]?["content"]?.ToString();
                        if (string.IsNullOrEmpty(content))
                        {
                            // If no text content is returned check if a tool call exists.
                            if (chunk["choices"]?[0]?["delta"]?["tool_calls"] is JArray toolCalls && toolCalls.Any())
                            {
                                // Save the name of the chosen tool if not already set.
                                if (string.IsNullOrEmpty(ChosenTool))
                                    ChosenTool = toolCalls[0]["function"]?["name"]?.ToString();
                                content = toolCalls[0]["function"]?["arguments"]?.ToString();
                            }
                        }

                        if (!string.IsNullOrEmpty(content))
                        {
                            responseBuilder.Append(content);
                            onStreamingUpdate?.Invoke(content); // Use callback
                        }
                        return string.Empty;
                    }
                    else
                    {
                        // Update token counts if available.
                        if (chunk["usage"] is JObject usage && usage.HasValues)
                        {
                            inputTokens = usage["prompt_tokens"]?.Value<int>() ?? inputTokens;
                            outputTokens = usage["completion_tokens"]?.Value<int>() ?? outputTokens;
                        }
                        else
                        {
                            return line; // return leftovers if tokens not updated.
                        }
                        return string.Empty;
                    }
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return line; // return leftover line on JSON parse error.
                }
            }
            return line;
        }



        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            var usage = response["usage"];
            var inputTokens = usage?["prompt_tokens"]?.Value<int>() ?? 0;
            var outputTokens = usage?["completion_tokens"]?.Value<int>() ?? 0;
            return new TokenUsage(inputTokens.ToString(), outputTokens.ToString());
        }
    }
}
