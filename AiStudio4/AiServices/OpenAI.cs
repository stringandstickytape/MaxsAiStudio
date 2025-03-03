using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharedClasses.Providers;
using AiStudio4.DataModels;
using System.Net.Http;
using AiStudio4.Conversations;
using System.IO;

namespace AiStudio4.AiServices
{
    internal class OpenAI : AiServiceBase
    {
        private bool deepseekBodge;

        public OpenAI() { }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 300);
            deepseekBodge = ApiUrl.Contains("deepseek");
            
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conversation.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conversation, options.UseStreaming, options.ApiSettings);

            // Create system message
            var systemMessage = new JObject
            {
                ["role"] = "system",
                ["content"] = deepseekBodge
                    ? options.Conversation.SystemPromptWithDateTime()
                    : new JArray(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = options.Conversation.SystemPromptWithDateTime()
                    })
            };

            var messagesArray = new JArray { systemMessage };

            // Add conversation messages
            foreach (var m in options.Conversation.messages)
            {
                messagesArray.Add(CreateMessageObject(m));
            }
            requestPayload["messages"] = messagesArray;

            AddToolsToRequest(requestPayload, options.ToolIds);

            if (options.AddEmbeddings)
            {
                var lastMessageContent = options.Conversation.messages.Last().content;
                var newInput = await AddEmbeddingsIfRequired(options.Conversation, options.ApiSettings, options.MustNotUseEmbedding, options.AddEmbeddings, lastMessageContent);
                ((JArray)requestPayload["messages"]).Last["content"].Last["text"] = newInput;
            }

            var json = JsonConvert.SerializeObject(requestPayload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await HandleResponse(content, options.UseStreaming, options.CancellationToken);
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConversation conversation, bool useStreaming, ApiSettings apiSettings)
        {
            // The supportsLogprobs flag may be extended later if desired
            var supportsLogprobs = false;

            var additionalParamsList = AdditionalParams.Split(';').Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim());
            var additionalParamsDict = additionalParamsList.ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

            var payload = new JObject
            {
                ["model"] = modelName,
                ["stream"] = useStreaming,
                ["stream_options"] = useStreaming ? new JObject { ["include_usage"] = true } : null,
                //["reasoning_effort"] = "lolz"
            };

            foreach(var entry in additionalParamsDict)
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

        protected override JObject CreateMessageObject(LinearConversationMessage message)
        {
            var messageContent = new JArray();

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

            messageContent.Add(new JObject
            {
                ["type"] = "text",
                ["text"] = message.content
            });

            return new JObject
            {
                ["role"] = message.role,
                ["content"] = deepseekBodge ? message.content : messageContent
            };
        }

        protected override void AddToolsToRequest(JObject request, List<string> toolIDs)
        {
            //if (toolIDs == null || !toolIDs.Any())
            //    return;
            //
            //var toolObj = ToolManager.Tools.First(x => x.Name == toolIDs[0]);
            //var firstLine = toolObj.FullText.Split('\n')[0]
            //    .Replace("//", "")
            //    .Replace(" ", "")
            //    .Replace("\r", "")
            //    .Replace("\n", "");
            //
            //var toolManager = new ToolManager();
            //var colorSchemeTool = toolManager.Tools.First(x => x.InternalName == firstLine);
            //// Remove comment header
            //var colorSchemeToolText = Regex.Replace(colorSchemeTool.FullText, @"^//.*\n", string.Empty, RegexOptions.Multiline);
            //var schema = JObject.Parse(colorSchemeToolText);
            //
            //if (deepseekBodge)
            //{
            //    // Wrap tool details for deepseek requests
            //    var wrappedTool = new JObject
            //    {
            //        ["type"] = "function",
            //        ["function"] = schema
            //    };
            //
            //    wrappedTool["function"]["parameters"] = wrappedTool["function"]["input_schema"];
            //    // Remove the original "input_schema" property
            //    foreach (var c in wrappedTool["function"].Children().OfType<JProperty>().ToList())
            //    {
            //        if (c.Name == "input_schema")
            //            c.Remove();
            //    }
            //
            //    request["tools"] = new JArray { wrappedTool };
            //    request["tool_choice"] = wrappedTool;
            //}
            //else
            //{
            //    // Change key name from "input_schema" to "schema"
            //    schema["schema"] = schema["input_schema"];
            //    schema.Remove("input_schema");
            //
            //    request["response_format"] = new JObject
            //    {
            //        ["type"] = "json_schema",
            //        ["json_schema"] = schema
            //    };
            //
            //    request.Remove("tools");
            //    request.Remove("tool_choice");
            //}
        }

        protected override async Task<AiResponse> HandleStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            using var response = await SendRequest(content, cancellationToken, true);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            var responseBuilder = new StringBuilder();
            var buffer = new char[1024];
            int charsRead;

            int inputTokens = 0;
            int outputTokens = 0;
            string leftovers = null;

            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();

                var chunk = new string(buffer, 0, charsRead);
                var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    leftovers = ProcessLine($"{leftovers}{line}", responseBuilder, ref inputTokens, ref outputTokens);
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

            OnStreamingComplete();

            return new AiResponse
            {
                ResponseText = responseBuilder.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString())
            };
        }

        private string ProcessLine(string line, StringBuilder responseBuilder, ref int inputTokens, ref int outputTokens)
        {
            if (line.Length < 6)
                return line;

            if (line.StartsWith("\r"))
                line = line.Substring(1);

            if (line.StartsWith("data: "))
            {
                var jsonData = line["data: ".Length..].Trim();
                if (jsonData.Equals("[DONE]"))
                    return string.Empty;

                try
                {
                    // Validate JSON format briefly using System.Text.Json
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
                            // Check for tool_calls if present
                            if (chunk["choices"]?[0]?["delta"]?["tool_calls"] is JArray toolCalls && toolCalls.Any())
                            {
                                content = toolCalls[0]["function"]?["arguments"]?.ToString();
                            }
                        }

                        if (!string.IsNullOrEmpty(content))
                        {
                            responseBuilder.Append(content);
                            OnStreamingDataReceived(content);
                        }
                        return string.Empty;
                    }
                    else
                    {
                        // Update token counts if available
                        if (chunk["usage"] is JObject usage && usage.HasValues)
                        {
                            inputTokens = usage["prompt_tokens"]?.Value<int>() ?? inputTokens;
                            outputTokens = usage["completion_tokens"]?.Value<int>() ?? outputTokens;
                        }
                        else
                        {
                            return line; // left-overs
                        }
                        return string.Empty;
                    }
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return line; // left-overs on JSON parse error
                }
            }
            return line;
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            var response = await SendRequest(content, cancellationToken);
            ValidateResponse(response);

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            string responseText = string.Empty;
            var message = jsonResponse["choices"]?[0]?["message"];
            if (message?["tool_calls"] is JArray toolCalls && toolCalls.Any())
            {
                responseText = toolCalls[0]?["function"]?["arguments"]?.ToString();
            }
            else
            {
                responseText = message?["content"]?.ToString();
            }

            return new AiResponse
            {
                ResponseText = responseText,
                Success = true,
                TokenUsage = ExtractTokenUsage(jsonResponse)
            };
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