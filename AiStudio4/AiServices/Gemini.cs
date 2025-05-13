using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedClasses.Providers;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace AiStudio4.AiServices
{
    // Class to hold generated image data
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
// Clear any previously generated images
            _generatedImages.Clear();
            // Reset ToolResponseSet for each new request
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);
            var url = $"{ApiUrl}{ApiModel}:{(options.UseStreaming ? "streamGenerateContent" : "generateContent")}?key={ApiKey}";
            
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conv, options.UseStreaming, options.ApiSettings);

            // Add tools if specified
            if (!forceNoTools)
            {
                await AddToolsToRequestAsync(requestPayload, options.ToolIds);
            }

            // Construct the messages array
            var contentsArray = new JArray();
            foreach (var message in options.Conv.messages)
            {
                var messageObj = CreateMessageObject(message);
                contentsArray.Add(messageObj);
            }


            

            // Add response modalities for image generation model
            if (ApiModel == "gemini-2.0-flash-exp-image-generation")
            {
                requestPayload["generationConfig"] = new JObject
                {
                    ["responseModalities"] = new JArray { "Text", "Image" },
                    ["temperature"] = options.ApiSettings.Temperature
                };
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
            }



            if (requestPayload["contents"] != null)
            {
                requestPayload.Remove("contents");
            }

            // Add "contents" at the end
            requestPayload.Add("contents", contentsArray);

            // Test that removes all other tools and enables Google Grounding
            //requestPayload["tools"] = new JArray();
            //((JArray)requestPayload["tools"]).Add(new JObject { ["google_search"] = new JObject() });
            File.WriteAllText(DateTime.Now.Ticks + ".json", requestPayload.ToString());
            var jsonPayload = JsonConvert.SerializeObject(requestPayload);
            using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            {
                return await HandleResponse(options, content); // Pass options
            }
        }

        protected override JObject CreateRequestPayload(
    string apiModel,
    LinearConv conv,
    bool useStreaming,
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
                partArray.Add(new JObject { ["text"] = message.content });

                // Handle legacy single image
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
                
                // Handle multiple attachments
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
                        // Additional attachment types could be handled here
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
            // Gemini uses key as URL parameter, not as Authorization header
        }
        protected override async Task<AiResponse> HandleStreamingResponse( 
            HttpContent content, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete)
        {
            StringBuilder fullResponse = new StringBuilder(); // Ensure this is accessible in catch
            onStreamingUpdate?.Invoke(""); // Use callback
            try
            {
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
                                    // Throwing ensures we jump to the catch block
                                    throw new OperationCanceledException(cancellationToken);
                                }
                                //System.Diagnostics.Debug.WriteLine(line);
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
                                    await ProcessJsonObject(jsonObject, fullResponse, onStreamingUpdate); // Pass callback
                                    jsonBuffer.Clear();
                                }
                            }

                            //response.EnsureSuccessStatusCode();
                        }
                    }
                }

                // Normal completion
                onStreamingComplete?.Invoke(); // Use callback
                Debug.WriteLine("Streaming Complete");

                if(ToolResponseSet.Tools.Count == 0)
                {
                    var json = ExtractTrailingJsonObject(fullResponse.ToString());
                    if(json != null)
                    {
                        var jsonResponse = JsonConvert.DeserializeObject<JObject>(json);

                        if (jsonResponse["args"] != null)
                        {
                            Debug.WriteLine("Args are not null");

                            var toolName = jsonResponse["name"]?.ToString();
                            var toolArgs = jsonResponse["args"].ToString();
                            ToolResponseSet.Tools.Add(new ToolResponseItem { ToolName = toolName, ResponseText = toolArgs });
                            
                            // Clear the response text when a tool is chosen
                            //fullResponse.Clear();
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
                            ResponseText = toolArgs,
                            Success = true,
                            TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount),
                            ChosenTool = toolName,
                            ToolResponseSet = ToolResponseSet,
                            IsCancelled = false // Explicitly false on normal completion
                        };
                    }
                }
                catch (Exception e)
                {
                    // Fall through to default response
                }

                // Create attachments from any generated images
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
                        Size = image.Base64Data.Length * 3 / 4 // Approximate size calculation
                    });
                }
                currentResponseItem = null;
                
                Debug.WriteLine($"Returning with {ToolResponseSet.Tools.Count} tools in the tool response set: {string.Join(",", ToolResponseSet.Tools.Select(x => x.ToolName))}... (2)");
                return new AiResponse
                {
                    ResponseText = fullResponse.ToString(),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount ?? "0"),
                    ChosenTool = null,
                    Attachments = attachments.Count > 0 ? attachments : null,
                    ToolResponseSet = ToolResponseSet,
                    IsCancelled = false // Explicitly false on normal completion
                };
            }
            catch (OperationCanceledException)
            {
                // Cancellation happened
                //System.Diagnostics.Debug.WriteLine("Gemini streaming cancelled.");
                // Create attachments from any partially generated images
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
                        Size = image.Base64Data.Length * 3 / 4 // Approximate size calculation
                    });
                }
                currentResponseItem = null;
                // Return partial response
                return new AiResponse
                {
                    ResponseText = fullResponse.ToString(),
                    Success = true, // Indicate successful handling of cancellation
                    TokenUsage = new TokenUsage(inputTokenCount ?? "0", outputTokenCount ?? "0", "0", cachedTokenCount ?? "0"),
                    ChosenTool = chosenTool, // Use the tool identified so far
                    Attachments = attachments.Count > 0 ? attachments : null,
                    ToolResponseSet = ToolResponseSet, // Use partially populated set
                    IsCancelled = true
                };
            }
            catch (Exception ex)
            {
                // Handle other errors
                return HandleError(ex, "Error during streaming response");
            }
        }

        public static string ExtractTrailingJsonObject(string input)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            // Start from the end and work backwards
            for (int i = input.Length - 1; i >= 0; i--)
            {
                if (input[i] == '}')
                {
                    // Found a closing brace, now find the matching opening brace
                    int depth = 1;
                    int j;

                    for (j = i - 1; j >= 0; j--)
                    {
                        if (input[j] == '}')
                            depth++;
                        else if (input[j] == '{')
                            depth--;

                        // When depth reaches 0, we've found the outermost matching opening brace
                        if (depth == 0)
                            break;
                    }

                    // If we found a matching opening brace
                    if (j >= 0)
                    {
                        string potentialJson = input.Substring(j);

                        // Validate it's proper JSON
                        try
                        {
                            System.Text.Json.JsonDocument.Parse(potentialJson);
                            return potentialJson;
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Not valid JSON, continue searching
                            i = j; // Skip to before this opening brace
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
                    // Check if this is a tool response
                    if (part["functionCall"] != null)
                    {
                        textBuilder.Append(JsonConvert.SerializeObject(part["functionCall"]));
                    }
                    // Handle text parts
                    else if (part["text"] != null)
                    {
                        textBuilder.Append(part["text"]?.ToString() ?? "");
                    }
                    // Add a placeholder for images
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

        protected override async Task<AiResponse> HandleNonStreamingResponse( 
            HttpContent content, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, // Parameter added but not used in non-streaming
            Action onStreamingComplete) // Parameter added but not used in non-streaming
        {
            HttpResponseMessage response = await client.PostAsync($"{ApiUrl}{ApiModel}:generateContent?key={ApiKey}", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                var completion = JsonConvert.DeserializeObject<JObject>(responseContent);

                var inputTokens = completion["usageMetadata"]?["promptTokenCount"]?.ToString();
                var outputTokens = completion["usageMetadata"]?["candidatesTokenCount"]?.ToString();
                var chosenTool = ExtractChosenToolFromCompletion(completion);

                // Extract any images from the response
                var attachments = new List<DataModels.Attachment>();
                int imageIndex = 1;
                
                if (completion["candidates"]?[0]?["content"]?["parts"] is JArray parts)
                {
                    foreach (var part in parts)
                    {
                        if (part["inlineData"] != null)
                        {
                            string mimeType = part["inlineData"]["mimeType"]?.ToString();
                            string base64Data = part["inlineData"]["data"]?.ToString();
                            
                            if (!string.IsNullOrEmpty(mimeType) && !string.IsNullOrEmpty(base64Data))
                            {
                                attachments.Add(new DataModels.Attachment
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = $"generated_image_{imageIndex++}.png",
                                    Type = mimeType,
                                    Content = base64Data,
                                    Size = base64Data.Length * 3 / 4 // Approximate size calculation
                                });
                            }
                        }
                    }
                }
                
                // Process tool calls if present
                if (chosenTool != null)
                {
                    if (completion["candidates"]?[0]?["content"]?["parts"] is JArray partsX)
                    {
                        foreach (var part in partsX)
                        {
                            if (part["functionCall"] != null)
                            {
                                string toolName = part["functionCall"]["name"]?.ToString();
                                string toolArguments = part["functionCall"]["args"]?.ToString() ?? "{}";
                                
                                // Add to ToolResponseSet
                                ToolResponseSet.Tools.Add(new ToolResponseItem
                                {
                                    ToolName = toolName,
                                    ResponseText = toolArguments
                                });
                                
                                // When a tool is chosen, don't include the tool response in ResponseText
                                return new AiResponse
                                {
                                    ResponseText = "", // Empty response text for tool calls
                                    Success = true,
                                    TokenUsage = new TokenUsage(inputTokens, outputTokens),
                                    ChosenTool = chosenTool,
                                    Attachments = attachments.Count > 0 ? attachments : null,
                                    ToolResponseSet = ToolResponseSet
                                };
                            }
                        }
                    }
                }

                currentResponseItem = null;
                return new AiResponse
                {
                    ResponseText = ExtractResponseText(completion),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens, outputTokens, "0", completion["usageMetadata"]?["cachedContentTokenCount"]?.ToString()),
                    ChosenTool = chosenTool,
                    Attachments = attachments.Count > 0 ? attachments : null,
                    ToolResponseSet = ToolResponseSet
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
                            // Handle function call responses
                            if (part["functionCall"] != null)
                            {
                                var toolResponse = JsonConvert.SerializeObject(part["functionCall"]);
                                var toolName = part["functionCall"]["name"]?.ToString();
                                var toolArgs = part["functionCall"]["args"]?.ToString() ?? "{}";
                                
                                chosenTool = toolName;

                                Debug.WriteLine($"Tool chosen: {chosenTool}");

                                // If this is a new tool call, create a new response item
                                //if (currentResponseItem == null || currentResponseItem.ToolName != toolName)
                                //{
                                    Debug.WriteLine($"new ToolResponseItem: {chosenTool} -> {toolArgs}");
                                    currentResponseItem = new ToolResponseItem
                                    {
                                        ToolName = toolName,
                                        ResponseText = toolArgs
                                    };

                                    // send the live stream message about tool-chosen
                                    onStreamingUpdate?.Invoke($"\n\nTool selected: {toolName}\n\n"); // Use callback

                                    ToolResponseSet.Tools.Add(currentResponseItem);
                                //}
                                //else
                                //{
                                //    currentResponseItem.ResponseText += toolArgs;
                                //}
                                
                                // Don't append tool response to fullResponse
                                // fullResponse.Append(toolResponse);
                                onStreamingUpdate?.Invoke(toolResponse); // Use callback
                            }
                            // Handle text responses
                            else if (part["text"] != null)
                            {
                                var textChunk = part["text"]?.ToString();
                                if (!string.IsNullOrEmpty(textChunk))
                                {
                                    if (fullResponse.Length != 0 || (textChunk != "ny"))
                                    {
                                        fullResponse.Append(textChunk);
                                        onStreamingUpdate?.Invoke(textChunk); // Use callback
                                        Debug.WriteLine($"text: {textChunk}");
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                            // Handle image responses
                            else if (part["inlineData"] != null)
                            {
                                // Capture image for later processing
                                string mimeType = part["inlineData"]["mimeType"]?.ToString();
                                string base64Data = part["inlineData"]["data"]?.ToString();
                                
                                if (!string.IsNullOrEmpty(mimeType) && !string.IsNullOrEmpty(base64Data))
                                {
                                    // Add image to the collection
                                    _generatedImages.Add(new GenImage
                                    {
                                        MimeType = mimeType,
                                        Base64Data = base64Data
                                    });
                                    
                                    // Add a placeholder in the response text
                                    var imagePlaceholder = "[Generated Image]";
                                    fullResponse.Append(imagePlaceholder);
                                    onStreamingUpdate?.Invoke(imagePlaceholder); // Use callback
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

                            //var x = streamData["usageMetadata"]?["cacheTokensDetails"];
                            //if (x != null) Debugger.Break();
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
    }


}