using AiTool3.ApiManagement;
using AiTool3.Conversations;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace AiTool3.Providers
{
    internal class OpenAI : IAiService
    {
        HttpClient client = new HttpClient();
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, ToolManager toolManager = null)
        {
            if (client.DefaultRequestHeaders.Authorization == null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiModel.Key);

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["messages"] = new JArray
                {
                    new JObject
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
                    }
                },
                ["stream"] = useStreaming
            };

            foreach (var m in conversation.messages)
            {
                var messageContent = new JArray
                {

                };

                if (!string.IsNullOrWhiteSpace(m.base64image))
                {
                    messageContent.Add(new JObject
                    {
                        ["type"] = "image_url",
                        ["image_url"] = new JObject
                        {
                            ["url"] = $"data:{m.base64type};base64,{m.base64image}"
                        }
                    });
                }


                messageContent.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = m.content
                });

                req["messages"].Last.AddAfterSelf(new JObject
                {
                    ["role"] = m.role,
                    ["content"] = messageContent
                });
            }


            JObject tool = null;
            if (toolIDs != null && toolIDs.Any())
            {
                var toolObj = toolManager.Tools.First(x => x.Name == toolIDs[0]);
                // get first line of toolObj.FullText
                var firstLine = toolObj.FullText.Split("\n")[0];
                firstLine = firstLine.Replace("//", "").Replace(" ", "").Replace("\r", "").Replace("\n", "");

                var colorSchemeTool = AssemblyHelper.GetEmbeddedAssembly($"AiTool3.Tools.{firstLine}");

                colorSchemeTool = Regex.Replace(colorSchemeTool, @"^//.*\n", "", RegexOptions.Multiline);

                var toolx = JObject.Parse(colorSchemeTool);

                var wrappedtool = new JObject
                {
                    ["type"] = "function",
                    ["function"] = toolx
                };

                var compare = GetFindAndReplaceTool();

                wrappedtool["function"]["parameters"] = wrappedtool["function"]["input_schema"];
                // neow remove input_schema
                wrappedtool["function"].Children().Reverse().ToList().ForEach(c =>
                { if (((JProperty)c).Name == "input_schema") c.Remove(); }
                );

                var jsonString = @"{
      ""type"": ""function"",
      ""function"": {
        ""name"": ""get_current_temperature"",
        ""description"": ""Get the current temperature for a specific location"",
        ""parameters"": {
          ""type"": ""object"",
          ""properties"": {
            ""location"": {
              ""type"": ""string"",
              ""description"": ""The city and state, e.g., San Francisco, CA""
            },
            ""unit"": {
              ""type"": ""string"",
              ""enum"": [""Celsius"", ""Fahrenheit""],
              ""description"": ""The temperature unit to use. Infer this from the user's location.""
            }
          },
          ""required"": [""location"", ""unit""]
        }
      }
    }";

                // set req["tools"] from the jsonstring
                //req["tools"] = new JArray { JObject.Parse(jsonString) };
                //req["tool_choice"] = JObject.Parse(jsonString);
                req["tools"] = new JArray { wrappedtool };
                req["tool_choice"] = wrappedtool;

                //JObject findAndReplacesTool = GetFindAndReplaceTool();
                //
                //req["tools"] = new JArray { findAndReplacesTool };
                //req["tool_choice"] = findAndReplacesTool;
            }

            var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
            req["messages"].Last["content"].Last()["text"] = newInput;


            var json = JsonConvert.SerializeObject(req, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (useStreaming)
            {
                return await HandleStreamingResponse(apiModel, content, cancellationToken);
            }
            else
            {
                var response = await client.PostAsync(apiModel.Url, content, cancellationToken);
                return await HandleNonStreamingResponse(response, cancellationToken);
            }
        }


        private async Task<AiResponse> HandleStreamingResponse(Model apiModel, StringContent content, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, apiModel.Url) { Content = content };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var reader = new StreamReader(stream);
            
            var responseBuilder = new StringBuilder();
            var buffer = new char[1024];
            int charsRead;

            int inputTokens = 0;
            int outputTokens = 0;

            string leftovers = null; ;

            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException();


                var chunk = new string(buffer, 0, charsRead);
                var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    response.EnsureSuccessStatusCode();
                    leftovers = ProcessLine(line.TrimStart(), responseBuilder, ref inputTokens, ref outputTokens);
                }
            }

            StreamingComplete?.Invoke(this, null);

            return new AiResponse
            {
                ResponseText = responseBuilder.ToString(),
                Success = true,
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString())
            };
        }

        private static JObject GetFindAndReplaceTool()
        {
            return new JObject
            {
                ["type"] = "function",
                ["function"] = new JObject
                {
                    ["name"] = "find_and_replace",
                    ["description"] = "Perform find and replace operations on text",
                    ["parameters"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["replacements"] = new JObject
                            {
                                ["type"] = "array",
                                ["items"] = new JObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = new JObject
                                    {
                                        ["find"] = new JObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = "The string to find"
                                        },
                                        ["replace"] = new JObject
                                        {
                                            ["type"] = "string",
                                            ["description"] = "The string to replace with"
                                        }
                                    },
                                    ["required"] = new JArray { "find", "replace" }
                                },
                                ["description"] = "A list of find-and-replace pairs"
                            }
                        },
                        ["required"] = new JArray { "replacements" }
                    }
                }
            };
        }
        private string ProcessLine(string line, StringBuilder responseBuilder, ref int inputTokens, ref int outputTokens)
        {
            Debug.WriteLine(line);
            if (line.StartsWith("data: "))
            {
                string jsonData = line.Substring("data: ".Length).Trim();

                if (jsonData == "[DONE]")
                    return null;

                try
                {
                    var chunk = JsonConvert.DeserializeObject<JObject>(jsonData);
                    var content = chunk["choices"]?[0]?["delta"]?["content"]?.ToString();

                    if (string.IsNullOrEmpty(content))
                        content = chunk["choices"]?[0]?["delta"]?["tool_calls"]?[0]["function"]?["arguments"]?.ToString();

                    if (!string.IsNullOrEmpty(content))
                    {
                        Debug.Write(content);
                        responseBuilder.Append(content);
                        StreamingTextReceived?.Invoke(this, content);
                    }

                    // Update token counts if available
                    var usage = chunk["usage"];
                    if (usage != null)
                    {
                        inputTokens = usage["prompt_tokens"]?.Value<int>() ?? inputTokens;
                        outputTokens = usage["completion_tokens"]?.Value<int>() ?? outputTokens;
                    }
                }
                catch (JsonException)
                {
                    return jsonData; /* left-overs */
                    // Handle JSON parsing errors
                }
            }
            return null;
        }
        private async Task<AiResponse> HandleNonStreamingResponse(HttpResponseMessage response, CancellationToken cts)
        {
            var responseContent = await response.Content.ReadAsStringAsync(cts);
            var jsonResponse = JsonConvert.DeserializeObject<JObject>(responseContent);

            var responseText = "";
            // if message has an array of tool_calls
            if (jsonResponse["choices"]?[0]?["message"]?["tool_calls"] != null)
            {
                var toolCallArray = jsonResponse["choices"]?[0]?["message"]?["tool_calls"] as JArray;

                // first tool call only for now... :/

                responseText = toolCallArray?[0]["function"]["arguments"].ToString();

            }
            else responseText = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();

            var usage = jsonResponse["usage"];
            var inputTokens = usage?["prompt_tokens"]?.Value<int>() ?? 0;
            var outputTokens = usage?["completion_tokens"]?.Value<int>() ?? 0;

            return new AiResponse
            {
                ResponseText = responseText,
                Success = true,
                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString())
            };
        }
    }
}