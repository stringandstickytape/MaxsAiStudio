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
        private readonly GeminiToolResponseProcessor _toolResponseProcessor;

        public Gemini()
        {
            _toolResponseProcessor = new GeminiToolResponseProcessor();
        }

        protected override ProviderFormat GetProviderFormat() => ProviderFormat.Gemini;

        protected override async Task AddTopPToRequest(JObject request, float topP)
        {
            // Gemini requires rebuilding the request with TopP
            // This is handled in CustomizeRequest instead
            await Task.CompletedTask;
        }

        protected override async Task CustomizeRequest(JObject request, AiRequestOptions options)
        {
            // Handle TopP for Gemini (requires rebuilding request)
            if (options.Model.AllowsTopP && options.ApiSettings.TopP > 0.0f && options.ApiSettings.TopP <= 1.0f)
            {
                ((JObject)request["generationConfig"])["topP"] = options.ApiSettings.TopP;
            }

            // Handle special Google Search directive
            bool hasGoogleSearchDirective = options.ToolIds?.Contains("GEMINI_INTERNAL_GOOGLE_SEARCH") == true;
            if (hasGoogleSearchDirective)
            {
                request["tools"] = new JArray
                {
                    new JObject
                    {
                        ["google_search"] = new JObject()
                    }
                };
            }

            await base.CustomizeRequest(request, options);
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            _generatedImages.Clear();
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);

            // Handle TTS models specially
            if (options.Model.IsTtsModel)
            {
                return await HandleTtsRequestAsync(options);
            }

            return await MakeStandardApiCall(options, async (content) =>
            {
                return await HandleResponse(options, content);
            }, forceNoTools);
        }

        public override async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings, 1800);

            // Handle TTS models
            if (options.Model.IsTtsModel)
            {
                return await HandleTtsRequestAsync(options);
            }

            return await ExecuteCommonToolLoop(
                options,
                toolExecutor,
                makeApiCall: async (opts) => await MakeGeminiApiCall(opts),
                createAssistantMessage: CreateGeminiAssistantMessage,
                createToolResultMessage: CreateGeminiToolResultMessage,
                options.MaxToolIterations ?? 10);
        }

        private async Task<AiResponse> MakeGeminiApiCall(AiRequestOptions options)
        {
            _generatedImages.Clear();
            
            return await MakeStandardApiCall(options, async (content) =>
            {
                return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
            });
        }

        private LinearConvMessage CreateGeminiAssistantMessage(AiResponse response)
        {
            return _toolResponseProcessor.CreateAssistantMessage(response);
        }

        private LinearConvMessage CreateGeminiToolResultMessage(List<ContentBlock> toolResultBlocks)
        {
            return _toolResponseProcessor.CreateToolResultMessage(toolResultBlocks);
        }

        protected override LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return _toolResponseProcessor.CreateUserInterjectionMessage(interjectionText);
        }

        protected override JObject CreateRequestPayload(
    string apiModel,
    LinearConv conv,
    ApiSettings apiSettings)
        {
            return RequestPayloadBuilder.Create(ProviderFormat.Gemini)
                .WithModel(apiModel)
                .WithConversation(conv)
                .WithApiSettings(apiSettings)
                .WithGenerationConfig()
                .WithMessages()
                .Build();
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            return MessageBuilder.CreateMessage(message, ProviderFormat.Gemini);
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
                            TokenUsage = CreateTokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount),
                            ChosenTool = toolName,
                            ToolResponseSet = ToolResponseSet,
                            IsCancelled = false 
                        };
                    }
                }
                catch (Exception e)
                {
                    
                }

                // Check if response contains malformed tool calls as JSON blocks in text
                var malformedToolCallResponse = await TryProcessMalformedToolCallsAsync(fullResponse.ToString());
                if (malformedToolCallResponse != null)
                {
                    return malformedToolCallResponse;
                }
                
                var attachments = BuildImageAttachments();
                currentResponseItem = null;

                Debug.WriteLine($"Returning with {ToolResponseSet.Tools.Count} tools in the tool response set: {string.Join(",", ToolResponseSet.Tools.Select(x => x.ToolName))}... (2)");
                return new AiResponse
                {
                    ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = fullResponse.ToString(), ContentType = Core.Models.ContentType.Text } },
                    Success = true,
                    TokenUsage = CreateTokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount),
                    ChosenTool = null,
                    Attachments = attachments.Count > 0 ? attachments : null,
                    ToolResponseSet = ToolResponseSet,
                    IsCancelled = false 
                };
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Cancelled. ");
                var attachments = BuildImageAttachments();
                currentResponseItem = null;
                
                var tokenUsage = CreateTokenUsage(inputTokenCount, outputTokenCount, "0", cachedTokenCount);
                
                return HandleCancellation(
                    fullResponse.ToString(),
                    tokenUsage,
                    ToolResponseSet,
                    chosenTool,
                    attachments.Count > 0 ? attachments : null
                );
            }
            catch (Exception ex)
            {
                
                return HandleError(ex, "Error during streaming response");
            }
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

        private List<DataModels.Attachment> BuildImageAttachments()
        {
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
            return attachments;
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
                        TokenUsage = CreateTokenUsage(inputTokenCount.ToString(), outputTokenCount.ToString()), 
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
