#define USE_STRUCTURED_OUTPUTS

using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Linq;
using SharedClasses.Helpers;

namespace AiTool3.Providers
{
    internal class OpenAI : AiServiceBase
    {
        public OpenAI()
        {
        }

        protected override void ConfigureHttpClientHeaders(Model apiModel, SettingsSet currentSettings)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);
        }


        public override async Task<AiResponse> FetchResponse(
            Model apiModel,
            Conversation conversation,
            string base64image,
            string base64ImageType,
            CancellationToken cancellationToken,
            SettingsSet currentSettings,
            bool mustNotUseEmbedding,
            List<string> toolIDs,
            bool useStreaming = false,
            bool addEmbeddings = false)
        {
            InitializeHttpClient(apiModel, currentSettings);
            var requestPayload = CreateRequestPayload(apiModel, conversation, useStreaming, currentSettings);

            var messagesArray = new JArray();

            messagesArray.Add(new JObject
            {
                ["role"] = "system",
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = conversation.SystemPromptWithDateTime()
                    }
                }
            });

            foreach (var m in conversation.messages)
            {
                messagesArray.Add(CreateMessageObject(m));
            }

            requestPayload["messages"] = messagesArray;

            AddToolsToRequest(requestPayload, toolIDs);


            if (addEmbeddings)
            {
                var newInput = await AddEmbeddingsIfRequired(conversation, currentSettings, mustNotUseEmbedding, addEmbeddings, conversation.messages.Last().content);
                ((JArray)requestPayload["messages"]).Last["content"].Last["text"] = newInput;
            }
            var json = JsonConvert.SerializeObject(requestPayload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await HandleResponse(apiModel, content, useStreaming, cancellationToken);
        }

        protected override JObject CreateRequestPayload(Model apiModel, Conversation conversation, bool useStreaming, SettingsSet currentSettings)
        {
            var supportsLogprobs = !apiModel.Url.Contains("generativelanguage.googleapis.com");

            var payload = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["stream"] = useStreaming,

                ["stream_options"] = useStreaming ? new JObject
                {
                    ["include_usage"] = true
                } : null
            };

            if (supportsLogprobs)
            {
                payload["logprobs"] = true;
                payload["top_logprobs"] = 5;
            }

            return payload;
        }

        protected override JObject CreateMessageObject(ConversationMessage message)
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
                ["content"] = messageContent
            };
        }



#if USE_STRUCTURED_OUTPUTS

        protected override void AddToolsToRequest(JObject request, List<string> toolIDs)
        {
            if (toolIDs == null || !toolIDs.Any()) return;
        
            var toolObj = ToolManager.Tools.First(x => x.Name == toolIDs[0]);
            // get first line of toolObj.FullText
            var firstLine = toolObj.FullText.Split("\n")[0];
            firstLine = firstLine.Replace("//", "").Replace(" ", "").Replace("\r", "").Replace("\n", "");
        
            var colorSchemeTool = AssemblyHelper.GetEmbeddedResource(System.Reflection.Assembly.GetExecutingAssembly(), $"AiTool3.Tools.{firstLine}");
        
            colorSchemeTool = System.Text.RegularExpressions.Regex.Replace(colorSchemeTool, @"^//.*\n", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        
            var toolx = JObject.Parse(colorSchemeTool);
        
            var wrappedtool = new JObject
            {
                ["type"] = "function",
                ["function"] = toolx
            };
        
            wrappedtool["function"]["parameters"] = wrappedtool["function"]["input_schema"];
        
            wrappedtool["function"].Children().Reverse().ToList().ForEach(c =>
            { if (((JProperty)c).Name == "input_schema") c.Remove(); }
            );
        
            request["tools"] = new JArray { wrappedtool };
            request["tool_choice"] = wrappedtool;
        }

#else
        protected override void AddToolsToRequest(JObject request, List<string> toolIDs)
        {
            if (toolIDs == null || !toolIDs.Any()) return;

            var toolObj = ToolManager.Tools.First(x => x.Name == toolIDs[0]);
            var firstLine = toolObj.FullText.Split("\n")[0];
            firstLine = firstLine.Replace("//", "").Replace(" ", "").Replace("\r", "").Replace("\n", "");

            var schemaJson = AssemblyHelper.GetEmbeddedResource(System.Reflection.Assembly.GetExecutingAssembly(), $"AiTool3.Tools.{firstLine}");

            // Remove comment lines from the schema
            schemaJson = System.Text.RegularExpressions.Regex.Replace(schemaJson, @"^//.*\n", "", System.Text.RegularExpressions.RegexOptions.Multiline);

            var schema = JObject.Parse(schemaJson);
            schema["schema"] = schema["input_schema"];
            schema.Remove("input_schema");

            // Create the response_format structure
            var responseFormat = new JObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = schema, // Assuming the schema is in input_schema
            };

            // Add to the request
            request["response_format"] = responseFormat;
            
            // Remove any existing tools or tool_choice if they exist
            if (request["tools"] != null) request.Remove("tools");
            if (request["tool_choice"] != null) request.Remove("tool_choice");
        }
#endif

        protected override async Task<AiResponse> HandleStreamingResponse(Model apiModel, HttpContent content, CancellationToken cancellationToken)
        {
            using var response = await SendRequest(apiModel, content, cancellationToken, true);

            ValidateResponse(response);

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
                string jsonData = line.Substring("data: ".Length).Trim();

                if (jsonData == "[DONE]")
                    return "";

                try
                {

                    try
                    {
                        // Attempt to parse the JSON string
                        using (JsonDocument doc = JsonDocument.Parse(jsonData))
                        {
                            //System.Diagnostics.Debug.WriteLine("JSON valid.");
                        }
                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        return line;
                    }


                    var chunk = JsonConvert.DeserializeObject<JObject>(jsonData);

                    if (chunk["choices"] != null && chunk["choices"].Count() > 0)
                    {
                        if (chunk["choices"]?[0]?["logprobs"] != null)
                        {
                            var x = chunk["choices"]?[0]?["logprobs"];
                            System.Diagnostics.Debug.WriteLine(x.ToString());
                            // Debugger.Break();
                        }
                        var content = chunk["choices"]?[0]?["delta"]?["content"]?.ToString();

                        if (string.IsNullOrEmpty(content))
                        {
                            if (chunk["choices"]?[0]?["delta"]?["tool_calls"] != null && chunk["choices"]?[0]?["delta"]?["tool_calls"].Count() > 0)
                            {
                                content = chunk["choices"]?[0]?["delta"]?["tool_calls"]?[0]["function"]?["arguments"]?.ToString();
                            }
                        }

                        if (!string.IsNullOrEmpty(content))
                        {
                            responseBuilder.Append(content);
                            OnStreamingDataReceived(content);
                        }
                        return "";
                    }
                    else
                    {
                        // Update token counts if available
                        var usage = chunk["usage"];
                        if (usage != null && usage.HasValues)
                        {
                            inputTokens = usage["prompt_tokens"]?.Value<int>() ?? inputTokens;
                            outputTokens = usage["completion_tokens"]?.Value<int>() ?? outputTokens;
                        }
                        else
                        {
                            return line; /* left-overs */
                        }
                        return "";
                    }

                }
                catch (Newtonsoft.Json.JsonException)
                {
                    return line; /* left-overs */
                    // Handle JSON parsing errors
                }
            }
            return line;
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(Model apiModel, HttpContent content, CancellationToken cancellationToken)
        {
            var response = await SendRequest(apiModel, content, cancellationToken);
            ValidateResponse(response);
            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            var responseText = "";
            // if message has an array of tool_calls
            if ((jsonResponse["choices"]?[0]?["message"]?["tool_calls"] as JArray) != null)
            {
                var toolCallArray = jsonResponse["choices"]?[0]?["message"]?["tool_calls"] as JArray;

                // first tool call only for now... :/

                if (toolCallArray.Any())
                {
                    responseText = toolCallArray?[0]["function"]["arguments"].ToString();
                }
                else responseText = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();
            }
            else responseText = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();


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