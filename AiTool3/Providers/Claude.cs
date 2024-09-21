using AiTool3.Conversations;
using AiTool3.DataModels;
using AiTool3.Embeddings;
using AiTool3.Interfaces;
using AiTool3.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Helpers;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace AiTool3.Providers
{
    internal class Claude : IAiService
    {
        public ToolManager ToolManager { get; set; }
        public bool UseTool { get; set; } = true;

        HttpClient client = new HttpClient();
        bool clientInitialised = false;


        // streaming text received callback event
        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        private string oneOffPreFill { get; set; }

        public void SetOneOffPreFill(string prefill)
        {
            oneOffPreFill = prefill;
        }

        public async Task<AiResponse> FetchResponse(Model apiModel, Conversation conversation, string base64image, string base64ImageType, CancellationToken cancellationToken, SettingsSet currentSettings, bool mustNotUseEmbedding, List<string> toolIDs, bool useStreaming = false, bool addEmbeddings = false)
        {
            if (!clientInitialised)
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiModel.Key);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // Prompt Caching
                //anthropic-beta: prompt-caching-2024-07-31
                if (currentSettings.UsePromptCaching)
                {
                    client.DefaultRequestHeaders.Add("anthropic-beta", "prompt-caching-2024-07-31");
                }

                clientInitialised = true;
            }

            var req = new JObject
            {
                ["model"] = apiModel.ModelName,
                ["system"] = conversation.systemprompt ?? "",
                ["max_tokens"] = 4096,
                ["stream"] = useStreaming,
                ["temperature"] = currentSettings.Temperature,
            };
            if (toolIDs != null && toolIDs.Any())
            {
                var toolObj = ToolManager.Tools.First(x => x.Name == toolIDs[0]);
                
                var firstLine = toolObj.FullText.Split("\n")[0];
                firstLine = firstLine.Replace("//", "").Replace(" ", "").Replace("\r", "").Replace("\n", "");

                var colorSchemeTool = AssemblyHelper.GetEmbeddedResource(Assembly.GetExecutingAssembly(), $"AiTool3.Tools.{firstLine}");

                colorSchemeTool = Regex.Replace(colorSchemeTool, @"^//.*\n", "", RegexOptions.Multiline);

                var toolx = JObject.Parse(colorSchemeTool);

                req["tools"] = new JArray { toolx };
                req["tool_choice"] = new JObject
                {
                    ["type"] = "tool",
                    ["name"] = toolx["name"].ToString()
                };
            }



            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
                conversation.messages.Last().content = newInput;
            }

            var messagesArray = new JArray();
            int userMessageCount = 0;

            for (int i = 0; i < conversation.messages.Count; i++)
            {
                var message = conversation.messages[i];

                var contentArray = new JArray();

                if (message.base64image != null)
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "image",
                        ["source"] = new JObject
                        {
                            ["type"] = "base64",
                            ["media_type"] = message.base64type,
                            ["data"] = message.base64image
                        }
                    });
                }

                contentArray.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = message.content.Replace("\r","")
                });

                var messageObject = new JObject
                {
                    ["role"] = message.role,
                    ["content"] = contentArray
                };

                // Mark the content up to each of the first four USER messages as ephemeral.  It's a strategy...
                if (currentSettings.UsePromptCaching && message.role.ToLower() == "user" && userMessageCount < 4)
                {
                    messageObject["content"][0]["cache_control"] = new JObject
                    {
                        ["type"] = "ephemeral"
                    };
                    userMessageCount++;
                }

                messagesArray.Add(messageObject);

                // prefill response test

            }

            // successful pre-fill test

            if(oneOffPreFill != null)
            {
                messagesArray.Add(new JObject
                {
                    ["role"] = "assistant",
                    ["content"] = new JArray
                      {
                          new JObject
                          {
                              ["type"] = "text",
                              ["text"] = oneOffPreFill // must not end with whitespace
                          }
                      }
                });
                
                
            }
            
            req["messages"] = messagesArray;

            if (addEmbeddings)
            {
                var newInput = await OllamaEmbeddingsHelper.AddEmbeddingsToInput(conversation, currentSettings, conversation.messages.Last().content, mustNotUseEmbedding);
                req["messages"].Last["content"].Last["text"] = newInput;
            }

            var json = JsonConvert.SerializeObject(req);

            // serialise to file w datetimestamp in filename and write to working dir
            var filename = $"request_{DateTime.Now:yyyyMMddHHmmss}.json";
            File.WriteAllText(filename, json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            if (useStreaming)
            {
                return await HandleStreamingResponse(apiModel, json, cancellationToken, currentSettings);
            }
            else
            {
                while (true)
                {
                    try
                    {
                        return await HandleNonStreamingResponse(apiModel, json, cancellationToken, currentSettings);
                    }
                    catch (NotEnoughTokensForCachingException)
                    {
                        if (currentSettings.UsePromptCaching)
                        {
                            // Remove caching and retry
                            json = RemoveCachingFromJson(json);
                            currentSettings.UsePromptCaching = false;
                        }
                        else
                        {
                            // If we're not using caching and still get this exception, something else is wrong
                            throw;
                        }
                    }
                }
            }
        }

        private string RemoveCachingFromJson(string json)
    {
        var jObject = JObject.Parse(json);
        var messages = jObject["messages"] as JArray;
        if (messages != null)
        {
            foreach (var message in messages)
            {
                var content = message["content"] as JArray;
                if (content != null)
                {
                    foreach (var item in content)
                    {
                        item["cache_control"]?.Parent.Remove();
                    }
                }
            }
        }
        return jObject.ToString();
    }

    private async Task<AiResponse> HandleStreamingResponse(Model apiModel, string json, CancellationToken cancellationToken, SettingsSet currentSettings)
    {
        while (true)
        {
            try
            {
                return await ProcessStreamingResponse(apiModel, json, cancellationToken, currentSettings);
            }
            catch (NotEnoughTokensForCachingException)
            {
                // Remove caching and retry
                json = RemoveCachingFromJson(json);
            }
        }
    }

    private async Task<AiResponse> ProcessStreamingResponse(Model apiModel, string json, CancellationToken cancellationToken, SettingsSet currentSettings)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, apiModel.Url) { Content = content };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            //if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            //{
            //    var content2 = await response.Content.ReadAsStringAsync();
            //}

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch ( Exception e )
            {
                // get the error response stream and text and so on
                var errorResponse = await response.Content.ReadAsStringAsync();
                

                throw;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var streamProcessor = new StreamProcessor(currentSettings.UsePromptCaching);
            streamProcessor.StreamingTextReceived += (s, e) => StreamingTextReceived?.Invoke(this, e);

            var result = await streamProcessor.ProcessStream(stream, cancellationToken);

            // call streaming complete
            StreamingComplete?.Invoke(this, null);

            var responseText = oneOffPreFill == null ? result.ResponseText : $"{oneOffPreFill}{result.ResponseText}";
            oneOffPreFill = null;
            return new AiResponse
            {
                ResponseText = responseText,
                Success = true,
                TokenUsage = new TokenUsage(result.InputTokens?.ToString(), result.OutputTokens?.ToString(), result.CacheCreationInputTokens?.ToString(), result.CacheReadInputTokens?.ToString())
            };
        }


        private async Task<AiResponse> HandleNonStreamingResponse(Model apiModel, string json, CancellationToken cancellationToken, SettingsSet currentSettings)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiModel.Url, content, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var completion = JsonConvert.DeserializeObject<JObject>(responseString);

            if (completion["type"]?.ToString() == "error")
            {
                if (completion["error"]["message"].ToString().Contains("at least 1024 tokens"))
                {
                    throw new NotEnoughTokensForCachingException(completion["error"]["message"].ToString());
                }
                else if (completion["error"]["message"].ToString().StartsWith("Overloaded"))
                {
                    // ask the user if they want to retry, using a messagebox
                    var result = MessageBox.Show("Claude reports that it's overloaded.  Would you like to retry?", "Server Overloaded", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        return await HandleNonStreamingResponse(apiModel, json, cancellationToken, currentSettings);
                    }
                }

                return new AiResponse { ResponseText = "error - " + completion["error"]["message"].ToString(), Success = false };
            }
            var inputTokens = completion["usage"]?["input_tokens"]?.ToString();
            var outputTokens = completion["usage"]?["output_tokens"]?.ToString();
            var cacheCreationInputTokens = completion["usage"]?["cache_creation_input_tokens"]?.ToString();
            var cacheReadInputTokens = completion["usage"]?["cache_read_input_tokens"]?.ToString();
            var responseText = "";
            if (completion["content"] != null)
            {
                // is the content type tooL?
                if (completion["content"][0]["type"].ToString() == "tool_use")
                {
                    responseText = completion["content"][0]["input"].First().ToString();
                }
                else responseText = completion["content"][0]["text"].ToString();
            }
            else if (completion["tool_calls"] != null && completion["tool_calls"][0]["function"]["name"].ToString() == "Find-and-replaces")
            {
                responseText = completion["tool_calls"][0]["function"]["arguments"].ToString();
            }
            var responseTextPrefilled = oneOffPreFill == null ? responseText : $"{oneOffPreFill}{responseText}";
            oneOffPreFill = null;

            return new AiResponse { ResponseText = responseTextPrefilled, Success = true, TokenUsage = new TokenUsage(inputTokens, outputTokens, cacheCreationInputTokens, cacheReadInputTokens) };
        }
    }

    internal class NotEnoughTokensForCachingException : Exception
    {
        public NotEnoughTokensForCachingException(string message) : base(message) { }
    }

    internal class StreamProcessor
    {
        private bool usePromptCaching;
        public event EventHandler<string> StreamingTextReceived;

        public StreamProcessor(bool usePromptCaching)
        {
            this.usePromptCaching = usePromptCaching;
        }

        public async Task<StreamProcessingResult> ProcessStream(Stream stream, CancellationToken cancellationToken)
        {
            var responseBuilder = new StringBuilder();
            var lineBuilder = new StringBuilder();
            var buffer = new byte[48];
            var decoder = Encoding.UTF8.GetDecoder();

            int? inputTokens = null;
            int? outputTokens = null;
            int? cacheCreationInputTokens = null;
            int? cacheReadInputTokens = null;

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytesRead)];
                decoder.GetChars(buffer, 0, bytesRead, chars, 0);

                foreach (char c in chars)
                {
                    if (c == '\n')
                    {
                        ProcessLine(lineBuilder.ToString(), responseBuilder, ref inputTokens, ref outputTokens, ref cacheCreationInputTokens, ref cacheReadInputTokens);
                        lineBuilder.Clear();
                    }
                    else
                    {
                        lineBuilder.Append(c);
                    }
                }
            }

            if (lineBuilder.Length > 0)
            {
                ProcessLine(lineBuilder.ToString(), responseBuilder, ref inputTokens, ref outputTokens, ref cacheCreationInputTokens, ref cacheReadInputTokens);
            }

            return new StreamProcessingResult
            {
                ResponseText = responseBuilder.ToString(),
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                CacheCreationInputTokens = cacheCreationInputTokens,
                CacheReadInputTokens = cacheReadInputTokens
            };
        }

        private void ProcessLine(string line, StringBuilder responseBuilder, ref int? inputTokens, ref int? outputTokens, ref int? cacheCreationInputTokens, ref int? cacheReadInputTokens)
        {

            // These variables are now passed as ref parameters
            // could contain data: {"type":"error","error":{"details":null,"type":"overloaded_error","message":"Overloaded"}              }

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") return;

                try
                {
                    var eventData = JsonConvert.DeserializeObject<JObject>(data);
                    if (eventData["type"].ToString() == "content_block_delta")
                    {
                        var text = eventData["delta"]["text"]?.ToString();
                        if (text == null)
                        {
                            text = eventData["delta"]["partial_json"]?.ToString();
                        }

                        Debug.WriteLine(text);
                        
                        StreamingTextReceived?.Invoke(this, text);
                        responseBuilder.Append(text);
                    }
                    else if (eventData["type"].ToString() == "message_start")
                    {
                        inputTokens = eventData["message"]["usage"]["input_tokens"].Value<int>();

                        if (eventData["message"]["usage"]["output_tokens"] != null)
                        {
                            outputTokens = eventData["message"]["usage"]["output_tokens"].Value<int>();
                        }

                        if (eventData["message"]["usage"]["cache_creation_input_tokens"] != null)
                        {
                            cacheCreationInputTokens = eventData["message"]["usage"]["cache_creation_input_tokens"].Value<int>();
                        }

                        if (eventData["message"]["usage"]["cache_read_input_tokens"] != null)
                        {
                            cacheReadInputTokens = eventData["message"]["usage"]["cache_read_input_tokens"].Value<int>();
                        }
                    }
                    else if (eventData["type"].ToString() == "message_delta")
                    {
                        outputTokens = eventData["usage"]["output_tokens"].Value<int>();
                        if (eventData["usage"]["cache_creation_input_tokens"] != null)
                        {
                            cacheCreationInputTokens = eventData["usage"]["cache_creation_input_tokens"].Value<int>();
                        }

                        if (eventData["usage"]["cache_read_input_tokens"] != null)
                        {
                            cacheReadInputTokens = eventData["usage"]["cache_read_input_tokens"].Value<int>();
                        }
                    }
                    else if (eventData["type"].ToString() == "error")
                    {
                        var errorMessage = eventData["error"]["message"].ToString();
                        if (usePromptCaching && errorMessage.Contains("at least 1024 tokens"))
                        {
                            // Input isn't long enough to cache. We need to restart the stream without caching.
                            throw new NotEnoughTokensForCachingException(errorMessage);
                        }
                        StreamingTextReceived?.Invoke(this, errorMessage);
                        responseBuilder.Append(errorMessage);
                    }
                    else
                    {

                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing error
                    Console.WriteLine($"Error parsing JSON: {ex.Message}");
                }
            }
        }
    }


    internal class StreamProcessingResult
    {
        public string ResponseText { get; set; }
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
        public int? CacheCreationInputTokens { get; set; }
        public int? CacheReadInputTokens { get; set; }
    }
}