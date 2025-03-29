using Microsoft.Extensions.Logging;
using AiStudio4.InjectedDependencies;
using AiStudio4.Core.Exceptions;
using AiStudio4.Core.Models;
using AiStudio4.Core.Interfaces;
using SharedClasses.Providers;
using AiStudio4.AiServices;
using AiStudio4.DataModels;
using AiStudio4.Convs;
using Newtonsoft.Json;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.IO;

namespace AiStudio4.Services
{
    public class DefaultChatService : IChatService
    {
        private readonly ILogger<DefaultChatService> _logger;
        private readonly SettingsManager _settingsManager;
        private readonly IToolService _toolService;
        private readonly IMcpService _mcpService;
        private readonly ISystemPromptService _systemPromptService;
        private readonly IToolProcessorService _toolProcessorService;

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public DefaultChatService(ILogger<DefaultChatService> logger, SettingsManager settingsManager, IToolService toolService, ISystemPromptService systemPromptService, IMcpService mcpService, IToolProcessorService toolProcessorService)
        {
            _logger = logger;
            _settingsManager = settingsManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _mcpService = mcpService;
            _toolProcessorService = toolProcessorService;
        }

        public async Task<SimpleChatResponse> ProcessSimpleChatRequest(string chatMessage)
        {
            var startTime = DateTime.UtcNow;
            try
            {
                _logger.LogInformation("Processing simple chat request");
                
                // Get the secondary model
                var secondaryModelName = _settingsManager.DefaultSettings?.SecondaryModel;
                if (string.IsNullOrEmpty(secondaryModelName))
                {
                    return new SimpleChatResponse
                    {
                        Success = false,
                        Error = "No secondary model configured",
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }

                // Find the model and service provider
                var model = _settingsManager.CurrentSettings.ModelList.FirstOrDefault(x => x.ModelName == secondaryModelName);
                if (model == null)
                {
                    return new SimpleChatResponse
                    {
                        Success = false,
                        Error = $"Secondary model '{secondaryModelName}' not found",
                        ProcessingTime = DateTime.UtcNow - startTime
                    };
                }

                var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService, _mcpService);

                // Create a simple chat request
                var systemPrompt = await _systemPromptService.GetDefaultSystemPromptAsync();
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = systemPrompt?.Content ?? "You are a helpful assistant.",
                    messages = new List<LinearConvMessage>
                    {
                        new LinearConvMessage
                        {
                            role = "user",
                            content = chatMessage
                        }
                    }
                };

                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conv = conv,
                    CancellationToken = new CancellationToken(false),
                    ApiSettings = _settingsManager.CurrentSettings.ToApiSettings(),
                    MustNotUseEmbedding = true,
                    UseStreaming = false
                };
                
                var response = await aiService.FetchResponse(requestOptions);
                
                return new SimpleChatResponse
                {
                    Success = response.Success,
                    Response = response.ResponseText,
                    Error = response.Success ? null : "Failed to process chat request",
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing simple chat request");
                return new SimpleChatResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ProcessingTime = DateTime.UtcNow - startTime
                };
            }
        }

        public async Task<ChatResponse> ProcessChatRequest(ChatRequest request)
        {
            try
            {
                _logger.LogInformation("Processing chat request for conv {ConvId}", request.ConvId);

                var model = _settingsManager.CurrentSettings.ModelList.First(x => x.ModelName == request.Model);
                var service = ServiceProvider.GetProviderForGuid(_settingsManager.CurrentSettings.ServiceProviders, model.ProviderGuid);
                var aiService = AiServiceResolver.GetAiService(service.ServiceName, _toolService, _mcpService);


                // Wire up streaming events
                aiService.StreamingTextReceived += (sender, text) =>
                {
                    _logger.LogTrace("Received streaming text fragment");
                    StreamingTextReceived?.Invoke(this, text);
                };
                aiService.StreamingComplete += (sender, text) =>
                {
                    _logger.LogDebug("Streaming complete");
                    StreamingComplete?.Invoke(this, text);
                };

                // Get the appropriate system prompt
                string systemPromptContent = "You are a helpful chatbot.";

                if (!string.IsNullOrEmpty(request.SystemPromptContent))
                {
                    // Use custom system prompt content provided in the request
                    systemPromptContent = request.SystemPromptContent;
                }
                else if (!string.IsNullOrEmpty(request.SystemPromptId))
                {
                    // Use specified system prompt ID
                    var systemPrompt = await _systemPromptService.GetSystemPromptByIdAsync(request.SystemPromptId);
                    if (systemPrompt != null)
                    {
                        systemPromptContent = systemPrompt.Content;
                    }
                }
                else if (!string.IsNullOrEmpty(request.ConvId))
                {
                    // Use conv-specific system prompt
                    var systemPrompt = await _systemPromptService.GetConvSystemPromptAsync(request.ConvId);
                    if (systemPrompt != null)
                    {
                        systemPromptContent = systemPrompt.Content;
                    }
                }

                const int MAX_ITERATIONS = 50; // Maximum number of tool call iterations
                

                int currentIteration = 0;
                bool continueLoop = true;
                AiResponse response = null; // Store the latest response
                AiStudio4.Core.Models.TokenCost accumulatedCostInfo = null;
                List<Attachment> finalAttachments = null;

                // Prepare initial conversation state
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = systemPromptContent,
                    messages = new List<LinearConvMessage>()
                };

                // Add all messages from history first
                foreach (var historyItem in request.MessageHistory.Where(x => x.Role != "system"))
                {
                    var message = new LinearConvMessage
                    {
                        role = historyItem.Role,
                        content = historyItem.Content,
                        attachments = historyItem.Attachments?.ToList() ?? new List<Attachment>()
                        // Assuming LinearConvMessage doesn't explicitly store tool calls/results from history
                        // If it does, map historyItem.ToolCalls and historyItem.ToolResults here
                    };
                    conv.messages.Add(message);
                }

                // Add the latest user message (the trigger for this request)
                var lastUserMessage = request.MessageHistory.LastOrDefault(m => m.Role == "user");
                if (lastUserMessage != null && !conv.messages.Any(m => m.role == "user" && m.content == lastUserMessage.Content)) // Avoid duplicates if history includes the trigger
                {
                    conv.messages.Add(new LinearConvMessage
                    {
                        role = "user",
                        content = lastUserMessage.Content,
                        attachments = lastUserMessage.Attachments?.ToList() ?? new List<Attachment>()
                    });
                }

                StringBuilder collatedResponse = new StringBuilder();

                // --- Tool Use Loop ---
                while (continueLoop && currentIteration < MAX_ITERATIONS)
                {
                    currentIteration++;
                    _logger.LogInformation("Processing chat request - Iteration {Iteration}", currentIteration);

                    if(request.ToolIds.Any())
                    {
                        var stopTool = (await _toolService.GetToolByToolNameAsync("Stop")).Guid;
                        if (!request.ToolIds.Contains(stopTool))
                            request.ToolIds.Add(stopTool);
                    }

                    var requestOptions = new AiRequestOptions
                    {
                        ServiceProvider = service,
                        Model = model,
                        Conv = conv, // Use the current state of the conversation
                        CancellationToken = request.CancellationToken,
                        ApiSettings = _settingsManager.CurrentSettings.ToApiSettings(),
                        MustNotUseEmbedding = true,
                        ToolIds = request.ToolIds ?? new List<string>(), // Pass available tools
                        UseStreaming = true, // Optional: Only stream the first response
                                             // CustomSystemPrompt is already in conv.systemprompt
                    };

                    response = await aiService.FetchResponse(requestOptions);

                    // Accumulate cost
                    //if (response.CostInfo != null)
                    //{
                    //    if (accumulatedCostInfo == null)
                    //        accumulatedCostInfo = new AiStudio4.Core.Models.TokenCost(0, 0, response.CostInfo.Model);
                    //    accumulatedCostInfo.Add(response.CostInfo);
                    //}
                    finalAttachments = response.Attachments; // Keep the latest attachments

                    // Add assistant message to conversation history
                    var assistantMessage = new LinearConvMessage
                    {
                        role = "assistant",
                        content = response.ResponseText,
                        // TODO: Map response.ToolResponseSet.Tools to a ToolCall structure if LinearConvMessage supports it
                        // tool_calls = response.ToolResponseSet?.Tools.Select(t => new { id = t.ToolCallId, type = "function", function = new { name = t.ToolName, arguments = t.ParametersJson } }).ToList()
                    };
                    conv.messages.Add(assistantMessage);

                    accumulatedCostInfo = new TokenCost();
                    finalAttachments = new List<Attachment>();

                    var toolResult = await _toolProcessorService.ProcessToolsAsync(response, conv, collatedResponse, request.CancellationToken);
                    continueLoop = toolResult.ContinueProcessing;

                    if(request.ToolIds.Count == 2) // one of which must be "Stop", so the user has only selected 1 tool
                    {
                        continueLoop = false;
                    }

                    if (toolResult.Attachments?.Count > 0)
                    {
                        if (finalAttachments == null) finalAttachments = new List<Attachment>();
                        finalAttachments.AddRange(toolResult.Attachments);
                    }

                    // If the loop should continue, add a user message to prompt the next step
                    if (continueLoop && currentIteration < MAX_ITERATIONS)
                    {
                        _logger.LogDebug("Adding 'Continue' message for next iteration.");
                        conv.messages.Add(new LinearConvMessage { role = "user", content = "Continue" });
                    }
                    else if (currentIteration >= MAX_ITERATIONS)
                    {
                        _logger.LogWarning("Maximum tool iteration limit ({MaxIterations}) reached.", MAX_ITERATIONS);
                        continueLoop = false; // Ensure loop terminates
                    }

                } // --- End of Tool Use Loop ---



                _logger.LogInformation("Successfully processed chat request after {Iterations} iterations.", currentIteration);
                return new ChatResponse
                {
                    Success = true,
                    // Return the text from the *last* assistant response in the loop
                    ResponseText = $"{(response?.ResponseText ?? "")}\n{collatedResponse.ToString()}",
                    CostInfo = accumulatedCostInfo, // Return the accumulated cost
                    Attachments = finalAttachments // Return the latest attachments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request");
                throw new ChatProcessingException("Failed to process chat request", ex);
            }
        }

    }

    public class CustomJsonParser
    {
        public static Dictionary<string, object> ParseJson(string json)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                return ProcessJsonElement(doc.RootElement) as Dictionary<string, object>;
            }
        }

        private static object ProcessJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dictionary = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        dictionary[property.Name] = ProcessJsonElement(property.Value);
                    }
                    return dictionary;

                case JsonValueKind.Array:
                    // Check if all elements are numbers - if so, return as float[]
                    bool allNumbers = true;
                    foreach (var item in element.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Number)
                        {
                            allNumbers = false;
                            break;
                        }
                    }

                    if (allNumbers)
                    {
                        return element.EnumerateArray()
                            .Select(e => e.GetSingle())
                            .ToArray();
                    }
                    else
                    {
                        return element.EnumerateArray()
                            .Select(ProcessJsonElement)
                            .ToArray();
                    }

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    // You could add additional logic here if needed
                    return element.GetSingle();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return null;
            }
        }
    }
}
