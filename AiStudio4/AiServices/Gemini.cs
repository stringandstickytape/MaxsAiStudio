using AiStudio4.Convs;
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
    internal class GeneratedImage
    {
        public string MimeType { get; set; }
        public string Base64Data { get; set; }
    }

    internal class Gemini : AiServiceBase
        {
        private readonly List<GeneratedImage> _generatedImages = new List<GeneratedImage>();
        
        public Gemini()
        {
            }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            // Clear any previously generated images
            _generatedImages.Clear();
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 300);
            var url = $"{ApiUrl}{ApiModel}:{(options.UseStreaming ? "streamGenerateContent" : "generateContent")}?key={ApiKey}";
            
            // Apply custom system prompt if provided
            if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
            {
                options.Conv.systemprompt = options.CustomSystemPrompt;
            }

            var requestPayload = CreateRequestPayload(ApiModel, options.Conv, options.UseStreaming, options.ApiSettings);

            // Add tools if specified
            if (options.ToolIds?.Any() == true)
            {
                AddToolsToRequest(requestPayload, options.ToolIds);
            }

            // Construct the messages array
            var contentsArray = new JArray();
            foreach (var message in options.Conv.messages)
            {
                var messageObj = CreateMessageObject(message);
                contentsArray.Add(messageObj);
            }


            requestPayload["contents"] = contentsArray;

            // Add response modalities for image generation model
            if (ApiModel == "gemini-2.0-flash-exp-image-generation")
            {
                requestPayload["generationConfig"] = new JObject
                {
                    ["responseModalities"] = new JArray { "Text", "Image" }
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
            }


            if (options.AddEmbeddings)
            {
                var lastMessage = options.Conv.messages.Last().content;
                var newInput = await AddEmbeddingsIfRequired(
                    options.Conv, 
                    options.ApiSettings, 
                    options.MustNotUseEmbedding, 
                    options.AddEmbeddings, 
                    lastMessage);
                    
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
                return await HandleResponse(content, options.UseStreaming, options.CancellationToken);
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
        protected override async Task<AiResponse> HandleStreamingResponse( HttpContent content, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiUrl}{ApiModel}:streamGenerateContent?key={ApiKey}"))
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

                        try
                        {
                            var jsonResponse = JsonConvert.DeserializeObject<JObject>(fullResponse.ToString()); 

                            if (jsonResponse["args"] != null)
                            {
                                //var toolCallArray = jsonResponse["choices"]?[0]?["message"]?["tool_calls"] as JArray;

                                return new AiResponse
                                {
                                    ResponseText = jsonResponse["args"].ToString(),
                                    Success = !cancellationToken.IsCancellationRequested,
                                    TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount),
                                    ChosenTool = jsonResponse["name"]?.ToString()
                                };
                            }
                        }
                        catch(Exception e)
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

                        return new AiResponse
                        {
                            ResponseText = fullResponse.ToString(),
                            Success = !cancellationToken.IsCancellationRequested,
                            TokenUsage = new TokenUsage(inputTokenCount, outputTokenCount),
                            ChosenTool = null,
                            Attachments = attachments.Count > 0 ? attachments : null
                        };
                }
                }
            }
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

        protected override async Task<AiResponse> HandleNonStreamingResponse( HttpContent content, CancellationToken cancellationToken)
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
                
                return new AiResponse
                {
                    ResponseText = ExtractResponseText(completion),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens, outputTokens),
                    ChosenTool = chosenTool,
                    Attachments = attachments.Count > 0 ? attachments : null
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
        private string chosenTool = null;
        private async Task<string> ProcessJsonObject(string jsonString, StringBuilder fullResponse)
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
                                chosenTool = part["functionCall"]["name"]?.ToString();
                                fullResponse.Append(toolResponse);
                                OnStreamingDataReceived(toolResponse);
                            }
                            // Handle text responses
                            else if (part["text"] != null)
                            {
                                var textChunk = part["text"]?.ToString();
                                if (!string.IsNullOrEmpty(textChunk))
                                {
                                    fullResponse.Append(textChunk);
                                    OnStreamingDataReceived(textChunk);
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
                                    _generatedImages.Add(new GeneratedImage
                                    {
                                        MimeType = mimeType,
                                        Base64Data = base64Data
                                    });
                                    
                                    // Add a placeholder in the response text
                                    var imagePlaceholder = "[Generated Image]";
                                    fullResponse.Append(imagePlaceholder);
                                    OnStreamingDataReceived(imagePlaceholder);
                                }
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