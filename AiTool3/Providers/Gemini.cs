using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AiTool3.Providers
{

        internal class Gemini : AiServiceBase
        {
            public Gemini()
            {
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
            InitializeHttpClient(apiModel, currentSettings, 300);
            var url = $"{apiModel.Url}{apiModel.ModelName}:{(useStreaming ? "streamGenerateContent" : "generateContent")}?key={apiModel.Key}";

            var requestPayload = CreateRequestPayload(apiModel, conversation, useStreaming, currentSettings);

            // Add tools if specified
            if (toolIDs?.Any() == true)
            {
                AddToolsToRequest(requestPayload, toolIDs);
            }

            // Construct the messages array
            var contentsArray = new JArray();
            foreach (var message in conversation.messages)
            {
                var messageObj = CreateMessageObject(message);
                contentsArray.Add(messageObj);
            }


            requestPayload["contents"] = contentsArray;
            requestPayload["system_instruction"] = new JObject
            {
                ["parts"] = new JObject
                {
                    ["text"] = conversation.SystemPromptWithDateTime()
                }
            };


            if (addEmbeddings)
                {
                    var lastMessage = conversation.messages.Last().content;
                    var newInput = await AddEmbeddingsIfRequired(conversation, currentSettings, mustNotUseEmbedding, addEmbeddings, lastMessage);
                    // does the last content array thing have a text prop?
                    var lastContent = ((JArray)requestPayload["contents"]).Last;
                    if (lastContent["parts"].Last["text"] != null)
                    {
                        lastContent["parts"].Last["text"] = newInput;
                    }
                    else
                    {
                        // set the text prop on the last-but-one content instead
                        var lastButOneContent = ((JArray)requestPayload["contents"]).Reverse().Skip(1).First();
                        lastButOneContent["parts"].Last["text"] = newInput;
                    }
                }

                var jsonPayload = JsonConvert.SerializeObject(requestPayload);
                using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                {
                    return await HandleResponse(apiModel, content, useStreaming, cancellationToken);
                }
            }

        protected override JObject CreateRequestPayload(
    Model apiModel,
    Conversation conversation,
    bool useStreaming,
    SettingsSet currentSettings)
        {
            return new JObject
            {
                ["contents"] = new JArray()
            };
        }

        protected override JObject CreateMessageObject(ConversationMessage message)
            {
                var partArray = new JArray();
                partArray.Add(new JObject { ["text"] = message.content });

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

                return new JObject
                {
                    ["role"] = message.role == "assistant" ? "model" : message.role,
                    ["parts"] = partArray
                };
            }



        protected override void ConfigureHttpClientHeaders(Model apiModel, SettingsSet currentSettings)
        {
            // Gemini uses key as URL parameter, not as Authorization header
        }
        protected override async Task<AiResponse> HandleStreamingResponse(Model apiModel, HttpContent content, CancellationToken cancellationToken)
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, $"{apiModel.Url}{apiModel.ModelName}:streamGenerateContent?key={apiModel.Key}"))
                {
                    request.Content = content;
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var reader = new StreamReader(stream))
                        {
                            StringBuilder fullResponse = new StringBuilder();
                            StringBuilder jsonBuffer = new StringBuilder();
                            bool isFirstLine = true;

                            while (!reader.EndOfStream)
                            {
                                string line = await reader.ReadLineAsync(cancellationToken);
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                System.Diagnostics.Debug.WriteLine(line);
                                // :-/
                                if (isFirstLine)
                                {
                                    // Remove leading '[' from the first line
                                    line = line.TrimStart('[');
                                    isFirstLine = false;
                                }

                                jsonBuffer.Append(line);
                                if (line == "," || line == "]")
                                {
                                    // We have a complete JSON object
                                    string jsonObject = jsonBuffer.ToString().TrimEnd(',').TrimEnd(']');
                                    await ProcessJsonObject(jsonObject, fullResponse);
                                    jsonBuffer.Clear();
                                }
                            }

                            if (cancellationToken.IsCancellationRequested)
                            {
                                reader.Dispose();
                                await stream.DisposeAsync();
                            }
                            else
                            {
                                OnStreamingComplete();
                            }

                            return new AiResponse
                            {
                                ResponseText = fullResponse.ToString(),
                                Success = !cancellationToken.IsCancellationRequested,
                                TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount)
                            };
                        }
                    }
                }
            }

        private string ExtractResponseText(JObject completion)
        {
            if (completion["candidates"]?[0]?["content"]?["parts"] != null)
            {
                var content = completion["candidates"][0]["content"]["parts"][0];

                // Check if this is a tool response
                if (content["functionCall"] != null)
                {
                    return JsonConvert.SerializeObject(content["functionCall"]);
                }

                return content["text"]?.ToString() ?? "";
            }
            return "";
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(Model apiModel, HttpContent content, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await client.PostAsync($"{apiModel.Url}{apiModel.ModelName}:generateContent?key={apiModel.Key}", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var completion = JsonConvert.DeserializeObject<JObject>(responseContent);

                var inputTokens = completion["usageMetadata"]?["promptTokenCount"]?.ToString();
                var outputTokens = completion["usageMetadata"]?["candidatesTokenCount"]?.ToString();

                return new AiResponse
                {
                    ResponseText = ExtractResponseText(completion),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens, outputTokens)
                };
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                return new AiResponse { ResponseText = errorContent, Success = false };
            }
        }

        private string inputTokenCount = "";
            private string outputTokenCount = "";
        private async Task<string> ProcessJsonObject(string jsonString, StringBuilder fullResponse)
        {
            if (!string.IsNullOrWhiteSpace(jsonString))
            {
                try
                {
                    var streamData = JObject.Parse(jsonString);
                    if (streamData["candidates"] != null)
                    {
                        var content = streamData["candidates"][0]["content"]["parts"][0];

                        // Handle tool responses
                        if (content["functionCall"] != null)
                        {
                            var toolResponse = JsonConvert.SerializeObject(content["functionCall"]);
                            fullResponse.Append(toolResponse);
                            OnStreamingDataReceived(toolResponse);
                        }
                        else
                        {
                            var textChunk = content["text"]?.ToString();
                            if (!string.IsNullOrEmpty(textChunk))
                            {
                                fullResponse.Append(textChunk);
                                OnStreamingDataReceived(textChunk);
                            }
                        }
                    }

                    if (streamData["usageMetadata"] != null)
                    {
                        inputTokenCount = streamData["usageMetadata"]?["promptTokenCount"]?.ToString();
                        outputTokenCount = streamData["usageMetadata"]?["candidatesTokenCount"]?.ToString();
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
                return new TokenUsage(inputTokens, outputTokens);
            }

        protected override void AddToolsToRequest(JObject request, List<string> toolIDs)
        {
            if (!toolIDs?.Any() == true) return;

            var toolObj = ToolManager.Tools.First(x => x.Name == toolIDs[0]);
            var firstLine = toolObj.FullText.Split("\n")[0]
                .Replace("//", "")
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "");

            var toolManager = new ToolManager();
            var tool = toolManager.Tools.First(x => x.InternalName == firstLine);
            var toolText = Regex.Replace(tool.FullText, @"^//.*\n", "", RegexOptions.Multiline);
            var toolConfig = JObject.Parse(toolText);

            toolConfig["parameters"] = toolConfig["input_schema"];
            toolConfig.Remove("input_schema");
            //toolConfig["required"] = toolConfig["parameters"]["required"];
            //((JObject)toolConfig["parameters"]).Remove("required");

            request["tools"] = new JArray
            {
                new JObject
                {
                    ["function_declarations"] = new JArray
                    {
                        toolConfig
                    }
                }
            }; // toolConfig;
            request["tool_config"] = new JObject
            {
                ["function_calling_config"] = new JObject
                {
                    ["mode"] = "ANY"
                }
            };
        }
    }


}