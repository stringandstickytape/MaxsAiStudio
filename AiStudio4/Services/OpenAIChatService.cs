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
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace AiStudio4.Services
{
    public class OpenAIChatService : IChatService
    {
        private readonly ILogger<OpenAIChatService> _logger;
        private readonly SettingsManager _settingsManager;
        private readonly IToolService _toolService;
        private readonly IMcpService _mcpService;
        private readonly ISystemPromptService _systemPromptService;

        public event EventHandler<string> StreamingTextReceived;
        public event EventHandler<string> StreamingComplete;

        public OpenAIChatService(ILogger<OpenAIChatService> logger, SettingsManager settingsManager, IToolService toolService, ISystemPromptService systemPromptService, IMcpService mcpService)
        {
            _logger = logger;
            _settingsManager = settingsManager;
            _toolService = toolService;
            _systemPromptService = systemPromptService;
            _mcpService = mcpService;
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
                
                var conv = new LinearConv(DateTime.Now)
                {
                    systemprompt = systemPromptContent,
                    messages = new List<LinearConvMessage>()
                };

                // Get tools if specified
                List<string> toolNames = null;
                if (request.ToolIds != null && request.ToolIds.Any())
                {
                    toolNames = new List<string>();
                    foreach (var toolId in request.ToolIds)
                    {
                        var tool = await _toolService.GetToolByIdAsync(toolId);
                        if (tool != null)
                        {
                            toolNames.Add(tool.Name);
                        }
                    }
                }


                // Add all messages from history first
                foreach (var historyItem in request.MessageHistory.Where(x => x.Role != "system"))
                {
                    var message = new LinearConvMessage
                    {
                        role = historyItem.Role,
                        content = historyItem.Content,
                        attachments = historyItem.Attachments?.ToList() ?? new List<Attachment>()
                    };
                    
                   
                    conv.messages.Add(message);
                }

                var requestOptions = new AiRequestOptions
                {
                    ServiceProvider = service,
                    Model = model,
                    Conv = conv,
                    CancellationToken = request.CancellationToken,
                    ApiSettings = _settingsManager.CurrentSettings.ToApiSettings(),
                    MustNotUseEmbedding = true,
                    ToolIds = request.ToolIds ?? new List<string>(),
                    UseStreaming = true,
                    
                    // No need to set CustomSystemPrompt as we've already set it in the conv object
                };
                
                var response = await aiService.FetchResponse(requestOptions);

               //var toolResponse = JsonConvert.DeserializeObject<dynamic>(response.ToolResponses[0].ToolJson);
               //var command = new
               //{
               //    type = toolResponse.name.ToString(),
               //    params_ = new
               //    {
               //        type = toolResponse.args.type.ToString(),
               //        name = "sphere1",
               //        location = new float[] { 0, 0, 0 },
               //        scale = new float[] { 1, 1, 1 }
               //    }
               //};
               //
               //string jsonCommand = System.Text.Json.JsonSerializer.Serialize(command)
               //    .Replace("params_", "params");

                var serverDefinitions = await _mcpService.GetAllServerDefinitionsAsync();

                if (response.ToolResponses != null)
                {
                    foreach (var toolResponse in response.ToolResponses)
                    {
                        //Dictionary<string, object> dict = JObject.Parse(toolResponse.ToolJson).ToObject<Dictionary<string, object>>();
                        var result = CustomJsonParser.ParseJson(toolResponse.ToolJson);
                        var retVal = await _mcpService.CallToolAsync(serverDefinitions[0].Id, toolResponse.ToolName, result);

                        response.ResponseText += JsonConvert.SerializeObject(retVal);
                    }
                }

               // _mcpService.CallToolAsync(serverDefinitions[0].Id, toolResponse.name, toolResponse. );
                    //var tools = await mcpService.ListToolsAsync(serverDefinition.Id);

                    // Calculate cost from the model directly
                    if (response.TokenUsage != null)
                {
                    response.CostInfo = new AiStudio4.Core.Models.TokenCost(
                        response.TokenUsage,
                        model
                    );
                }

                _logger.LogInformation("Successfully processed chat request");

                var responseText = response.ResponseText;

                if(response.ChosenTool != null)
                {
                    var tool = await _toolService.GetToolByNameAsync(response.ChosenTool);

                    if(tool != null)
                    {
                        responseText = $"{new string('`', 3)}{tool.Filetype}\n{responseText}\n{new string('`', 3)}";
                    }
                }
                return new ChatResponse
                {
                    Success = true,
                    ResponseText = responseText,
                    CostInfo = response.CostInfo,
                    Attachments = response.Attachments
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