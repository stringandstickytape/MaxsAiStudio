using AiStudio4.Convs;
using AiStudio4.Core.Tools;
using AiStudio4.DataModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Audio;
using OpenAI.Images;
using OpenAI.Embeddings;
using OpenAI.Assistants;
using SharedClasses.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ClientModel;
using AiStudio4.Core.Models;
using AiStudio4.Services;
using static System.Windows.Forms.DataFormats;

namespace AiStudio4.AiServices
{
    public class NetOpenAi: AiServiceBase
    {
        private OpenAIClient _openAIClient;
        private ChatClient _chatClient;
        private AudioClient _audioClient;
        private ImageClient _imageClient;
        private EmbeddingClient _embeddingClient;
        private readonly List<GeneratedImage> _generatedImages = new List<GeneratedImage>();

        public ToolResponse ToolResponseSet { get; set; } = new ToolResponse { Tools = new List<ToolResponseItem>() };

        public NetOpenAi() { }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            // Not using the base HttpClient as we're using the OpenAI .NET client
        }

        private void InitializeOpenAIClients(string model)
        {
            var cred = new ApiKeyCredential(ApiKey ?? "ollama");

            _openAIClient = new OpenAIClient(cred, new OpenAIClientOptions { Endpoint =  new Uri(ApiUrl) });
            
            _chatClient = _openAIClient.GetChatClient(model);
            _audioClient = _openAIClient.GetAudioClient("whisper-1"); // Default audio model
            _imageClient = _openAIClient.GetImageClient("dall-e-3"); // Default image model
            _embeddingClient = _openAIClient.GetEmbeddingClient("text-embedding-3-small"); // Default embedding model
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options)
        {
            // Reset ToolResponseSet for each new request
            ToolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            {
                // Initialize OpenAI clients
                InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);
                InitializeOpenAIClients(ApiModel);

                // Apply custom system prompt if provided
                if (!string.IsNullOrEmpty(options.CustomSystemPrompt))
                {
                    options.Conv.systemprompt = options.CustomSystemPrompt;
                }

                // Create list of messages for the chat completion
                List<ChatMessage> messages = new List<ChatMessage>();

                // Add system message if present
                if (!string.IsNullOrEmpty(options.Conv.systemprompt))
                {
                    messages.Add(new SystemChatMessage(options.Conv.SystemPromptWithDateTime()));
                }

                // Add conversation messages
                foreach (var message in options.Conv.messages)
                {
                    ChatMessage chatMessage = CreateChatMessage(message);
                    messages.Add(chatMessage);
                }

                // Configure chat completion options
                ChatCompletionOptions chatOptions = new ChatCompletionOptions
                {
                   Temperature = 0.2f
                };

                // Add tools if specified or if using MCP service tools
                await AddToolsToChatOptions(chatOptions, options.ToolIds);

                // Process embeddings if needed
                if (options.AddEmbeddings)
                {
                    var lastMessage = options.Conv.messages.Last();
                    var newInput = await AddEmbeddingsIfRequired(
                        options.Conv,
                        options.ApiSettings,
                        options.MustNotUseEmbedding,
                        options.AddEmbeddings,
                        lastMessage.content);

                    // Update the last message content with embeddings
                    if (messages.Count > 0 && messages.Last() is UserChatMessage userMessage)
                    {
                        // Replace the last message with the new content
                        int lastIndex = messages.Count - 1;
                        messages[lastIndex] = new UserChatMessage(newInput);
                    }
                }
                if (chatOptions.Tools.Any())
                {
                    chatOptions.ToolChoice = ChatToolChoice.CreateRequiredChoice();
                }
                try
                {
                    // Handle streaming vs non-streaming requests
                    if (options.UseStreaming)
                    {
                        return await HandleStreamingChatCompletion(messages, chatOptions, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                    }
                    else
                    {
                        return await HandleNonStreamingChatCompletion(messages, chatOptions, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                    }
                }
                catch (Exception ex)
                {
                    return HandleError(ex);
                }
            }
        }

        private ChatMessage CreateChatMessage(LinearConvMessage message)
        {
            List<ChatMessageContentPart> contentParts = new List<ChatMessageContentPart>();

            // Handle text content
            if (!string.IsNullOrEmpty(message.content))
            {
                contentParts.Add(ChatMessageContentPart.CreateTextPart(message.content));
            }

            // Handle legacy single image
            //if (!string.IsNullOrEmpty(message.base64image))
            //{
            //    byte[] imageData = Convert.FromBase64String(message.base64image);
            //    contentParts.Add(ChatMessageContentPart.CreateInputImagePart(
            //        BinaryData.FromBytes(imageData)));
            //}
            //
            //// Handle multiple attachments
            //if (message.attachments != null && message.attachments.Any())
            //{
            //    foreach (var attachment in message.attachments)
            //    {
            //        if (attachment.Type.StartsWith("image/"))
            //        {
            //            byte[] imageData = Convert.FromBase64String(attachment.Content);
            //            contentParts.Add(ChatMessageContentPart.CreateInputImagePart(
            //                BinaryData.FromBytes(imageData)));
            //        }
            //        // Additional attachment types could be handled here
            //    }
            //}

            // Create appropriate message type based on role
            switch (message.role.ToLower())
            {
                case "system":
                    return new SystemChatMessage(contentParts);
                case "user":
                    return new UserChatMessage(contentParts);
                case "assistant":
                    return new AssistantChatMessage(contentParts);
                case "tool":
                    // Tool messages require additional parameters
                    return new ToolChatMessage("tool_id", message.content);
                default:
                    return new UserChatMessage(contentParts);
            }
        }

        private async Task<AiResponse> HandleNonStreamingChatCompletion(
            List<ChatMessage> messages, 
            ChatCompletionOptions options, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, // Parameter added but not used
            Action onStreamingComplete) // Parameter added but not used
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);

            // Check if the response requires tool calls
            if (completion.FinishReason == ChatFinishReason.ToolCalls && completion.ToolCalls.Count > 0)
            {
                // Iterate through all tool calls and add them to the ToolResponseSet
                foreach (var toolCall in completion.ToolCalls)
                {
                    string toolName = toolCall.FunctionName;
                    string toolArguments = toolCall.FunctionArguments.ToString();
                    ToolResponseSet.Tools.Add(new ToolResponseItem
                    {
                        ToolName = toolName,
                        ResponseText = toolArguments
                    });
                }
                // Return response indicating tool calls were made
                return new AiResponse
                {
                    // ResponseText should be empty when tool calls are made
                    ResponseText = "", // Empty string instead of null or summary message
                    Success = true,
                    TokenUsage = ExtractTokenUsage(completion),
                    // ChosenTool is less relevant with multiple tools. Set to null or first tool name.
                    ChosenTool = completion.ToolCalls.FirstOrDefault()?.FunctionName, // Set to first tool name or null
                    ToolResponseSet = ToolResponseSet // Assign the populated set
                };
            }
            else
            {
                // Regular text response
                return new AiResponse
                {
                    ResponseText = completion.Content[0].Text,
                    Success = true,
                    TokenUsage = ExtractTokenUsage(completion),
                    ChosenTool = null,
                    ToolResponseSet = ToolResponseSet
                };
            }
        }

        private async Task<AiResponse> HandleStreamingChatCompletion(
            List<ChatMessage> messages, 
            ChatCompletionOptions options, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete)
        {
            StringBuilder responseBuilder = new StringBuilder();
            string chosenTool = null;
            int inputTokens = 0;
            int outputTokens = 0;

            try
            {
                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                    _chatClient.CompleteChatStreamingAsync(messages, options, cancellationToken);

                await foreach (StreamingChatCompletionUpdate update in completionUpdates.WithCancellation(cancellationToken))
                {

                    // Handle content updates (text)
                    if (update.ContentUpdate != null && update.ContentUpdate.Count > 0 && !string.IsNullOrEmpty(update.ContentUpdate[0].Text))
                    {
                        string textChunk = update.ContentUpdate[0].Text;
                        responseBuilder.Append(textChunk);
                        onStreamingUpdate?.Invoke(textChunk); // Use callback
                    }

                    // Handle tool call updates
                    if (update.ToolCallUpdates != null && update.ToolCallUpdates.Count > 0)
                    {
                        foreach (var toolCall in update.ToolCallUpdates)
                        {
                            if (!string.IsNullOrEmpty(toolCall.FunctionName))
                            {
                                chosenTool = toolCall.FunctionName;

                                // Create a new tool response item when we first identify the tool
                                var toolResponseItem = new ToolResponseItem
                                {
                                    ToolName = toolCall.FunctionName,
                                    ResponseText = ""
                                };
                                ToolResponseSet.Tools.Add(toolResponseItem);
                                
                                // Clear the response builder when a tool is chosen
                                //responseBuilder.Clear();
                            }
                            if (toolCall.FunctionArgumentsUpdate != null && toolCall.FunctionArgumentsUpdate.ToArray().Length > 0 && !string.IsNullOrEmpty(toolCall.FunctionArgumentsUpdate.ToString()))
                            {
                                string argumentUpdate = toolCall.FunctionArgumentsUpdate.ToString();
                                // Don't append to responseBuilder for tool calls
                                // responseBuilder.Append(argumentUpdate);
                                onStreamingUpdate?.Invoke(argumentUpdate); // Use callback

                                // Update the tool response text
                                if (ToolResponseSet.Tools.Count > 0)
                                {
                                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                                    if (lastToolResponse != null)
                                    {
                                        lastToolResponse.ResponseText += argumentUpdate;
                                        if (lastToolResponse.ResponseText.Length> 20 && !lastToolResponse.ResponseText.Substring(lastToolResponse.ResponseText.Length - 20, 20).Any(x => x != '\r' && x != '\n'))
                                        {
                                            // request cancellation on cancellationtoken

                                            onStreamingComplete?.Invoke(); // Use callback
                                            lastToolResponse.ResponseText = lastToolResponse.ResponseText.Trim();
                                            return new AiResponse
                                            {
                                                ResponseText = responseBuilder.ToString().TrimEnd(),
                                                Success = true,
                                                TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString()),
                                                ChosenTool = chosenTool,
                                                ToolResponseSet = ToolResponseSet

                                            };

                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Update the tool response text
                                if (ToolResponseSet.Tools.Count > 0)
                                {
                                    var lastToolResponse = ToolResponseSet.Tools.LastOrDefault(t => t.ToolName == chosenTool);
                                    if (lastToolResponse != null)
                                    {
                                        lastToolResponse.ResponseText += "";
                                    }
                                }
                            }
                        }
                    }

                    // Update token usage if available
                    if (update.Usage != null)
                    {
                        inputTokens = update.Usage.InputTokenCount;
                        outputTokens = update.Usage.OutputTokenCount;
                    }
                }

                onStreamingComplete?.Invoke(); // Use callback

                return new AiResponse
                {
                    ResponseText = responseBuilder.ToString(),
                    Success = true,
                    TokenUsage = new TokenUsage(inputTokens.ToString(), outputTokens.ToString()),
                    ChosenTool = chosenTool,
                    ToolResponseSet = ToolResponseSet
                    
                };
            }
            catch (Exception ex)
            {
                return HandleError(ex);
            }
        }

        private TokenUsage ExtractTokenUsage(ChatCompletion completion)
        {
            // Extract token usage from the completion
            if (completion.Usage != null)
            {
                return new TokenUsage(completion.Usage.InputTokenCount.ToString(), completion.Usage.OutputTokenCount.ToString());
            }
            return new TokenUsage("0", "0");
        }

        private async Task AddToolsToChatOptions(ChatCompletionOptions options, List<string> toolIDs)
        {
            // Create a ToolRequestBuilder to handle tool construction
            var toolRequestBuilder = new ToolRequestBuilder(ToolService, McpService);
            
            // Create a JObject that will mimic the request structure expected by ToolRequestBuilder
            JObject requestObj = new JObject
            {
                ["tools"] = new JArray()
            };
            
            // Add user-selected tools
            if (toolIDs?.Any() == true)
            {
                foreach (var toolId in toolIDs)
                {
                    await toolRequestBuilder.AddToolToRequestAsync(requestObj, toolId, ToolFormat.OpenAI);
                }
            }
            
            // Add MCP service tools
            await toolRequestBuilder.AddMcpServiceToolsToRequestAsync(requestObj, ToolFormat.OpenAI);
            
            // Convert the JArray of tools to ChatTool objects
            if (requestObj["tools"] is JArray toolsArray && toolsArray.Count > 0)
            {
                foreach (JObject toolObj in toolsArray)
                {
                    if (toolObj["type"]?.ToString() == "function" && toolObj["function"] is JObject functionObj)
                    {
                        string name = functionObj["name"]?.ToString();
                        string description = functionObj["description"]?.ToString();
                        JObject parameters = functionObj["parameters"] as JObject;
                        
                        if (!string.IsNullOrEmpty(name) && parameters != null)
                        {
                            options.Tools.Add(ChatTool.CreateFunctionTool(
                                functionName: name,
                                functionDescription: description,
                                functionParameters: BinaryData.FromString(parameters.ToString()),
                                functionSchemaIsStrict: false
                            ));
                        }
                    }
                }
            }
        }

        // This method has been removed as we now use ToolRequestBuilder to handle tool conversion
        
        //private async Task<ChatTool> ConvertToolToOpenAIFormatAsync(string toolId)
        //{
        //    // Get tool definition from your service
        //    var toolDef = await ToolService.GetToolByIdAsync(toolId);
        //   if (toolDef == null)
        //       return null;
        //
        //    var obj = JsonConvert.DeserializeObject(toolDef.Schema);
        //
        //    var schema = ((JObject)obj)["input_schema"].ToString().Replace("\r", "").ToString();
        //
        //    // Convert to OpenAI format
        //    return ChatTool.CreateFunctionTool(
        //       functionName: toolDef.Name.Replace(" ",""),
        //       functionDescription: toolDef.Description.Replace(" ",""),
        //       functionParameters: BinaryData.FromString(schema),
        //       functionSchemaIsStrict: true
        //   );
        //}

        // This method has been removed as we now use ToolRequestBuilder to handle MCP service tools
        //private void AddMcpServiceTools(ChatCompletionOptions options)
        //{
        //    // Implement MCP service tools conversion if needed
        //    // This would be similar to ConvertToolToOpenAIFormat but for MCP tools
        //}

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, bool useStreaming, ApiSettings apiSettings)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new JObject();
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new JObject();
        }

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, 
            Action onStreamingComplete)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new AiResponse { Success = false, ResponseText = "Not implemented" };
        }

        protected override async Task<AiResponse> HandleNonStreamingResponse(
            HttpContent content, 
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate, // Parameter added but not used
            Action onStreamingComplete) // Parameter added but not used
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new AiResponse { Success = false, ResponseText = "Not implemented" };
        }

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            // Not used in this implementation as we're using the OpenAI .NET client directly
            return new TokenUsage("0", "0");
        }

        protected override ToolFormat GetToolFormat()
        {
            return ToolFormat.OpenAI;
        }

        // Helper method for handling errors
        private AiResponse HandleError(Exception ex, string additionalInfo = "")
        {
            string errorMessage = $"Error: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMessage += $" Additional info: {additionalInfo}";
            }
            return new AiResponse { Success = false, ResponseText = errorMessage };
        }

        // Image generation support
        //public async Task<AiResponse> GenerateImage(string prompt, ApiSettings apiSettings)
        //{
        //    try
        //    {
        //        InitializeHttpClient(ServiceProvider.OpenAI, "dall-e-3", apiSettings);
        //        InitializeOpenAIClients("dall-e-3");
        //
        //        ImageGenerationOptions options = new ImageGenerationOptions
        //        {
        //            Quality = GeneratedImageQuality.Standard,
        //            Size = GeneratedImageSize.W1024xH1024,
        //            Style = GeneratedImageStyle.Natural,
        //            ResponseFormat = GeneratedImageFormat.Bytes
        //        };
        //
        //        GeneratedImage image = await _imageClient.GenerateImageAsync(prompt, options);
        //
        //        if (image.ImageBytes != null)
        //        {
        //            // Convert the image to base64 for the response
        //            string base64Image = Convert.ToBase64String(image.ImageBytes.ToArray());
        //
        //            // Create attachment
        //            var attachment = new DataModels.Attachment
        //            {
        //                Id = Guid.NewGuid().ToString(),
        //                Name = $"generated_image_{DateTime.Now:yyyyMMddHHmmss}.png",
        //                Type = "image/png",
        //                Content = base64Image,
        //                Size = base64Image.Length * 3 / 4 // Approximate size calculation
        //            };
        //
        //            return new AiResponse
        //            {
        //                ResponseText = "Image generated successfully.",
        //                Success = true,
        //                Attachments = new List<DataModels.Attachment> { attachment }
        //            };
        //        }
        //        else
        //        {
        //            return new AiResponse
        //            {
        //                ResponseText = "Image generation failed: No image data returned.",
        //                Success = false
        //            };
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return HandleError(ex, "Image generation failed");
        //    }
        //}

        // Audio transcription support
       //public async Task<AiResponse> TranscribeAudio(string audioFilePath, ApiSettings apiSettings)
       //{
       //    try
       //    {
       //        InitializeHttpClient(ServiceProvider.OpenAI, "whisper-1", apiSettings);
       //        InitializeOpenAIClients("whisper-1");
       //
       //        AudioTranscriptionOptions options = new AudioTranscriptionOptions
       //        {
       //            ResponseFormat = AudioTranscriptionFormat.Text,
       //            TimestampGranularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment
       //        };
       //
       //        AudioTranscription transcription = await _audioClient.TranscribeAudioAsync(audioFilePath, options);
       //
       //        return new AiResponse
       //        {
       //            ResponseText = transcription.Text,
       //            Success = true
       //        };
       //    }
       //    catch (Exception ex)
       //    {
       //        return HandleError(ex, "Audio transcription failed");
       //    }
       //}
    }


}