// C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiServices/LlamaCpp.cs
using AiStudio4.Convs;
using AiStudio4.Core.Models;
using AiStudio4.Core.Tools;
using AiStudio4.Core.Tools.CodeDiff.FileOperationHandlers;
using AiStudio4.DataModels;
using AiStudio4.InjectedDependencies;
using AiStudio4.Services;
using AiStudio4.Services.Interfaces;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using SharedClasses.Providers;
using System.Net.Http;
using System.Text;
using System.Windows.Shapes;

namespace AiStudio4.AiServices
{
    public class LlamaCpp : AiServiceBase
    {
        private readonly ILlamaServerService _llamaServerService;
        private readonly LlamaCppSettings _settings;

        public LlamaCpp()
        {
            var app = Application.Current as App;
            _llamaServerService = app.Services.GetService(typeof(ILlamaServerService)) as ILlamaServerService;

            _settings = new LlamaCppSettings();
            
            // Configure HTTP client timeout
            client.Timeout = TimeSpan.FromMinutes(10);
        }

        protected override void ConfigureHttpClientHeaders(ApiSettings apiSettings)
        {
            // llama-server doesn't require authentication headers
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        protected override async Task<AiResponse> FetchResponseInternal(AiRequestOptions options, bool forceNoTools = false)
        {
            try
            {
                // Ensure server is running with the correct model
                await EnsureServerRunning(options);
                
                // Build request using common pattern
                return await MakeStandardApiCall(options, async (content) =>
                {
                    return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
                }, forceNoTools);
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error in LlamaCpp provider");
            }
        }

        public override async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, Core.Interfaces.IToolExecutor toolExecutor, v4BranchedConv branchedConv, string parentMessageId, string assistantMessageId, string clientId)
        {
            try
            {
                await EnsureServerRunning(options);
            }
            catch(Exception e)
            {
                return new AiResponse
                {
                    ContentBlocks = new List<Core.Models.ContentBlock> { new Core.Models.ContentBlock { Content = e.Message, ContentType = ContentType.Text } }
                };

            }

             return await ExecuteCommonToolLoop(
                options,
                toolExecutor,
                makeApiCall: async (opts) => await MakeLlamaCppApiCall(opts),
                createAssistantMessage: CreateLlamaCppAssistantMessage,
                createToolResultMessage: CreateLlamaCppToolResultMessage,
                options.MaxToolIterations ?? 10);
        }

        private async Task<AiResponse> MakeLlamaCppApiCall(AiRequestOptions options)
        {
            var request = await BuildCommonRequest(options);
            await CustomizeRequest(request, options);

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
        }

        private LinearConvMessage CreateLlamaCppAssistantMessage(AiResponse response)
        {
            // Use OpenAI-compatible format since llama-server provides OpenAI API
            var contentArray = new JArray();
            
            // Add text content if any
            var textContent = response.ContentBlocks?.FirstOrDefault(c => c.ContentType == ContentType.Text)?.Content;
            if (!string.IsNullOrEmpty(textContent))
            {
                contentArray.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = textContent
                });
            }

            // Add tool calls if present
            if (response.ToolResponseSet?.Tools?.Any() == true)
            {
                foreach (var tool in response.ToolResponseSet.Tools)
                {
                    var toolCallId = $"call_{Guid.NewGuid():N}".Substring(0, 24);
                    
                    contentArray.Add(new JObject
                    {
                        ["type"] = "tool_call",
                        ["id"] = toolCallId,
                        ["function"] = new JObject
                        {
                            ["name"] = tool.ToolName,
                            ["arguments"] = tool.ResponseText
                        }
                    });
                }
            }

            return new LinearConvMessage
            {
                role = "assistant",
                contentBlocks = new List<Core.Models.ContentBlock>
                {
                    new Core.Models.ContentBlock
                    {
                        ContentType = ContentType.Text,
                        Content = contentArray.ToString()
                    }
                }
            };
        }

        private LinearConvMessage CreateLlamaCppToolResultMessage(List<Core.Models.ContentBlock> toolResultBlocks)
        {
            // Use OpenAI-compatible format
            var contentArray = new JArray();
            
            foreach (var block in toolResultBlocks)
            {
                if (block.ContentType == ContentType.ToolResponse)
                {
                    var toolData = JsonConvert.DeserializeObject<dynamic>(block.Content);
                    var toolCallId = $"call_{Guid.NewGuid():N}".Substring(0, 24);
                    
                    contentArray.Add(new JObject
                    {
                        ["type"] = "tool_result",
                        ["tool_call_id"] = toolCallId,
                        ["content"] = toolData.result?.ToString() ?? ""
                    });
                }
            }
            
            return new LinearConvMessage
            {
                role = "user",
                contentBlocks = new List<Core.Models.ContentBlock>
                {
                    new Core.Models.ContentBlock
                    {
                        ContentType = ContentType.ToolResponse,
                        Content = contentArray.ToString()
                    }
                }
            };
        }

        protected override LinearConvMessage CreateUserInterjectionMessage(string interjectionText)
        {
            return new LinearConvMessage
            {
                role = "user",
                contentBlocks = new List<Core.Models.ContentBlock>
                {
                    new Core.Models.ContentBlock
                    {
                        ContentType = ContentType.Text,
                        Content = interjectionText
                    }
                }
            };
        }

        private async Task EnsureServerRunning(AiRequestOptions options)
        {
            var modelPath = GetModelPath(options);
            var serverSettings = GetServerSettings(options);
            
            // Use the service to ensure server is running
            var baseUrl = await _llamaServerService.EnsureServerRunningAsync(modelPath, serverSettings);
            
            // Update client base address if needed
            if (client.BaseAddress?.ToString() != baseUrl)
            {
                client.BaseAddress = new Uri(baseUrl);
            }
        }

        private string GetModelPath(AiRequestOptions options)
        {
            // Try to get model path from various sources
            if (!string.IsNullOrEmpty(options.Model?.AdditionalParams))
            {
                try
                {
                    var settings = JsonConvert.DeserializeObject<LlamaCppSettings>(options.Model.AdditionalParams);
                    if (!string.IsNullOrEmpty(settings?.ModelPath) && File.Exists(settings.ModelPath))
                    {
                        return settings.ModelPath;
                    }
                }
                catch { }
            }

            // Fallback: try to interpret model name as path
            if (!string.IsNullOrEmpty(options.Model?.ModelName) && File.Exists(options.Model.ModelName))
            {
                return options.Model.ModelName;
            }

            throw new Exception("Model path not specified or file not found. Please provide a valid GGUF model path in the model configuration.");
        }

        private LlamaServerSettings GetServerSettings(AiRequestOptions options)
        {
            var settings = new LlamaServerSettings();

            // Try to get settings from model additional params
            if (!string.IsNullOrEmpty(options.Model?.AdditionalParams))
            {
                try
                {
                    var llamaCppSettings = JsonConvert.DeserializeObject<LlamaCppSettings>(options.Model.AdditionalParams);
                    if (llamaCppSettings != null)
                    {
                        settings.ContextSize = llamaCppSettings.ContextSize;
                        settings.GpuLayerCount = llamaCppSettings.GpuLayerCount;
                        settings.Threads = llamaCppSettings.Threads;
                        settings.BatchSize = llamaCppSettings.BatchSize;
                        settings.FlashAttention = llamaCppSettings.FlashAttention;
                        settings.AdditionalArgs = llamaCppSettings.AdditionalArgs;
                    }
                }
                catch { }
            }

            return settings;
        }

        protected override JObject CreateRequestPayload(string modelName, LinearConv conv, ApiSettings apiSettings)
        {
            var messages = new JArray();

            // Add system message if present
            if (!string.IsNullOrEmpty(conv.systemprompt))
            {
                messages.Add(new JObject
                {
                    ["role"] = "system",
                    ["content"] = conv.SystemPromptWithDateTime()
                });
            }

            // Add conversation messages
            foreach (var message in conv.messages)
            {
                messages.Add(CreateMessageObject(message));
            }

            return new JObject
            {
                ["model"] = "local-model", // llama-server ignores this
                ["messages"] = messages,
                ["temperature"] = apiSettings.Temperature,
                ["max_tokens"] = 2048,
                ["stream"] = true
            };
        }

        protected override JObject CreateMessageObject(LinearConvMessage message)
        {
            var textContent = string.Join("\n\n", 
                message.contentBlocks?.Where(b => b.ContentType == ContentType.Text)?.Select(b => b.Content) ?? new string[0]);

            var messageObj = new JObject
            {
                ["role"] = message.role,
                ["content"] = textContent
            };

            // Handle attachments if any
            if (message.attachments?.Any() == true)
            {
                var contentArray = new JArray();
                
                // Add text content
                if (!string.IsNullOrEmpty(textContent))
                {
                    contentArray.Add(new JObject
                    {
                        ["type"] = "text",
                        ["text"] = textContent
                    });
                }

                messageObj["content"] = contentArray;
            }

            return messageObj;
        }

        protected override async Task<AiResponse> HandleStreamingResponse(
            HttpContent content,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            try
            {
                using var response = await SendRequest(content, cancellationToken);
                //response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await ProcessLlamaCppStream(stream, cancellationToken, onStreamingUpdate, onStreamingComplete);
            }
            catch (OperationCanceledException)
            {
                return HandleCancellation("", new TokenUsage("0", "0"), new ToolResponse { Tools = new List<ToolResponseItem>() });
            }
            catch (Exception ex)
            {
                return HandleError(ex, "Error during streaming response");
            }
        }

        private async Task<AiResponse> ProcessLlamaCppStream(
            Stream stream,
            CancellationToken cancellationToken,
            Action<string> onStreamingUpdate,
            Action onStreamingComplete)
        {
            var responseBuilder = new StringBuilder();
            var toolResponseSet = new ToolResponse { Tools = new List<ToolResponseItem>() };
            string chosenTool = null;

            using var reader = new StreamReader(stream);
            string line;

            try
            {
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( line.StartsWith("error: "))
                    {
                        var data = line.Substring(7);

                        try
                        {
                            var chunk = JsonConvert.DeserializeObject<JObject>(data);
                            var msg = $"{chunk["code"]} {chunk["type"]}: {chunk["message"]}";
                            responseBuilder.Append(msg);
                            onStreamingUpdate?.Invoke(msg);
                        }
                        catch (JsonException)
                        {
                            // Skip malformed JSON chunks
                        }

                    }
                    else if (line.StartsWith("data: "))
                    {
                        var data = line.Substring(6);
                        if (data == "[DONE]")
                            break;

                        try
                        {
                            var chunk = JsonConvert.DeserializeObject<JObject>(data);
                            var choices = chunk["choices"] as JArray;

                            if (choices?.Count > 0)
                            {
                                var choice = choices[0] as JObject;
                                var delta = choice["delta"] as JObject;

                                // Handle text content
                                var content = delta?["content"]?.ToString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    responseBuilder.Append(content);
                                    onStreamingUpdate?.Invoke(content);
                                }

                                // Handle tool calls
                                var toolCalls = delta?["tool_calls"] as JArray;
                                if (toolCalls?.Count > 0)
                                {
                                    foreach (var toolCall in toolCalls)
                                    {
                                        var function = toolCall["function"];
                                        var name = function?["name"]?.ToString();
                                        var arguments = function?["arguments"]?.ToString();

                                        if (!string.IsNullOrEmpty(name))
                                        {
                                            chosenTool = name;
                                            var existingTool = toolResponseSet.Tools.FirstOrDefault(t => t.ToolName == name);
                                            if (existingTool == null)
                                            {
                                                toolResponseSet.Tools.Add(new ToolResponseItem
                                                {
                                                    ToolName = name,
                                                    ResponseText = arguments ?? ""
                                                });
                                            }
                                            else
                                            {
                                                existingTool.ResponseText += arguments ?? "";
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip malformed JSON chunks
                        }
                    }
                }

                onStreamingComplete?.Invoke();

                return new AiResponse
                {
                    ContentBlocks = new List<Core.Models.ContentBlock> 
                    { 
                        new Core.Models.ContentBlock
                        { 
                            Content = responseBuilder.ToString(), 
                            ContentType = ContentType.Text 
                        } 
                    },
                    Success = true,
                    TokenUsage = new TokenUsage("0", "0"), // llama-server doesn't always provide token counts in stream
                    ChosenTool = chosenTool,
                    ToolResponseSet = toolResponseSet,
                    IsCancelled = false
                };
            }
            catch (OperationCanceledException)
            {
                return HandleCancellation(responseBuilder.ToString(), new TokenUsage("0", "0"), toolResponseSet, chosenTool);
            }
        }

        protected override async Task<HttpResponseMessage> SendRequest(HttpContent content, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = content
            };

            return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        protected override ToolFormat GetToolFormat() => ToolFormat.OpenAI;
        protected override ProviderFormat GetProviderFormat() => ProviderFormat.OpenAI;

        protected override TokenUsage ExtractTokenUsage(JObject response)
        {
            var usage = response["usage"];
            return new TokenUsage(
                usage?["prompt_tokens"]?.ToString() ?? "0",
                usage?["completion_tokens"]?.ToString() ?? "0"
            );
        }

    }

    public class LlamaCppSettings
    {
        public string ModelPath { get; set; }
        public int ContextSize { get; set; } = 4096;
        public int GpuLayerCount { get; set; } = -1; // Auto-detect
        public int Threads { get; set; } = -1; // Auto-detect
        public int BatchSize { get; set; } = 2048;
        public bool FlashAttention { get; set; } = true;
        public string AdditionalArgs { get; set; } = "";
    }
}