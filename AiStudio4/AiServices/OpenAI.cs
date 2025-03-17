using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SharedClasses.Providers;
using AiStudio4.DataModels;
using System.Net.Http;
using AiStudio4.Convs;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 300);
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
            AddToolsToRequest(requestPayload, options.ToolIds);

            if (options.AddEmbeddings)
            {
                var lastMessageContent = options.Conv.messages.Last().content;
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
            return await HandleResponse(content, options.UseStreaming, options.CancellationToken);
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, bool useStreaming, ApiSettings apiSettings)
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
                    if (attachment.Type.StartsWith("image/"))
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

        // --------------------- TOOL SUPPORT ---------------------
        // In this override we mirror the pattern used by Claude.cs:
        // If any toolIDs are supplied, we initialize the "tools" array on the request and use the ToolRequestBuilder
        // to insert the tool details. The current GetToolFormat() method returns ToolFormat.OpenAI.
        protected override void AddToolsToRequest(JObject req, List<string> toolIDs)
        {
            if (toolIDs == null || !toolIDs.Any())
                return;

            if (req["tools"] == null)
                req["tools"] = new JArray();

            var toolRequestBuilder = new ToolRequestBuilder(ToolService);
            foreach (var toolId in toolIDs)
            {
                toolRequestBuilder.AddToolToRequest(req, toolId, GetToolFormat());
            }
        }

        protected override ToolFormat GetToolFormat() => ToolFormat.OpenAI;
        // --------------------- END TOOL SUPPORT ---------------------

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
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString()),
                ChosenTool = ChosenTool
            };
        }

        // In this method we update the response as streaming data is received.
        // In addition to appending text we check for the presence of a tool_call.
        private string ProcessLine(string line, StringBuilder responseBuilder, ref int inputTokens, ref int outputTokens)
        {
            if (line.Length < 6)
                return line;

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
                            OnStreamingDataReceived(content);
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

        protected override async Task<AiResponse> HandleNonStreamingResponse(HttpContent content, CancellationToken cancellationToken)
        {
            var response = await SendRequest(content, cancellationToken);
            ValidateResponse(response);

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            string responseText = string.Empty;
            string chosenToolLocal = null;
            var message = jsonResponse["choices"]?[0]?["message"];
            if (message?["tool_calls"] is JArray toolCalls && toolCalls.Any())
            {
                // Extract tool call details
                chosenToolLocal = toolCalls[0]?["function"]?["name"]?.ToString();
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
                TokenUsage = ExtractTokenUsage(jsonResponse),
                ChosenTool = chosenToolLocal
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