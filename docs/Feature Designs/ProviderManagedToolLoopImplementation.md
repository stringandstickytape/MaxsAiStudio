# Provider-Managed Tool Loop Implementation Design

## Overview

This design document outlines the refactoring of the AI tool execution system from client-side tool looping to provider-managed tool loops. The goal is to move the tool execution loop inside each AI provider implementation, allowing each provider to handle tool calling in their native format while maintaining centralized local tool execution.

## Current Architecture Problems

### Current Tool Loop Location
- **Main Loop**: `AiStudio4/Services/DefaultChatService.cs:173-428` in `ProcessChatRequest` method
- **Tool Processing**: `AiStudio4/Services/ToolProcessorService.cs:61-313` in `ProcessToolsAsync` method
- **Loop Control**: Manual iteration with `MAX_ITERATIONS = 50` and `ShouldContinueToolLoop` flag

### Issues with Current Design
1. **Generic Tool Handling**: All providers use the same tool loop logic, preventing provider-specific optimizations
2. **Tool Result Formatting**: Tool results are formatted generically instead of using provider-native formats
3. **Conversation State Management**: Complex manual tracking of conversation state during tool execution
4. **Provider Limitations**: Cannot leverage provider-specific tool calling features and optimizations

## Target Architecture

### Core Principle
Move the tool execution loop **inside** each AI provider implementation while maintaining centralized local tool execution through a standardized interface.

### Architecture Flow
```
DefaultChatService.ProcessChatRequest()
├── Prepare initial request
├── aiService.FetchResponseWithToolLoop(options, toolExecutor, clientId)
│   ├── Provider-specific tool loop implementation
│   │   ├── Call AI API with tool definitions
│   │   ├── Receive tool calls in provider-native format
│   │   ├── Execute tools via toolExecutor.ExecuteToolAsync()
│   │   ├── Format results in provider-specific way
│   │   └── Loop until completion or max iterations
│   └── Return final response
└── Process final response and update conversation
```

## Detailed Implementation Plan

### Step 1: Create IToolExecutor Interface

**File**: `AiStudio4/Core/Interfaces/IToolExecutor.cs`

```csharp
using AiStudio4.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for executing tools locally while allowing AI providers to manage the tool loop.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Executes a single tool call with the given parameters.
    /// </summary>
    /// <param name="toolName">The name of the tool to execute.</param>
    /// <param name="toolParameters">The JSON string of parameters for the tool.</param>
    /// <param name="context">Execution context including client ID, cancellation token, etc.</param>
    /// <returns>The result of the tool execution.</returns>
    Task<BuiltinToolResult> ExecuteToolAsync(string toolName, string toolParameters, ToolExecutionContext context);

    /// <summary>
    /// Gets all tools available for the AI to use.
    /// </summary>
    /// <param name="toolIds">A list of specific tool GUIDs to retrieve.</param>
    /// <returns>A collection of tool definitions.</returns>
    Task<IEnumerable<Tool>> GetAvailableToolsAsync(IEnumerable<string> toolIds);
}

/// <summary>
/// Context information for tool execution.
/// </summary>
public class ToolExecutionContext
{
    public string ClientId { get; set; }
    public CancellationToken CancellationToken { get; set; }
    public LinearConv Conversation { get; set; }
    public int CurrentIteration { get; set; }
}
```

### Step 2: Implement ToolExecutor Service

**File**: `AiStudio4/Services/ToolExecutor.cs`

```csharp
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Centralized tool execution service that handles both built-in and MCP tools.
/// This replaces the logic currently in ToolProcessorService for individual tool execution.
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly IBuiltinToolService _builtinToolService;
    private readonly IMcpService _mcpService;
    private readonly IToolService _toolService;
    private readonly IInterjectionService _interjectionService;
    private readonly IWebSocketNotificationService _notificationService;

    public ToolExecutor(
        IBuiltinToolService builtinToolService, 
        IMcpService mcpService, 
        IToolService toolService,
        IInterjectionService interjectionService,
        IWebSocketNotificationService notificationService)
    {
        _builtinToolService = builtinToolService;
        _mcpService = mcpService;
        _toolService = toolService;
        _interjectionService = interjectionService;
        _notificationService = notificationService;
    }

    public async Task<BuiltinToolResult> ExecuteToolAsync(string toolName, string toolParameters, ToolExecutionContext context)
    {
        // Check for user interjections before tool execution
        if (_interjectionService.HasInterjection(context.ClientId))
        {
            var interjection = _interjectionService.GetAndClearInterjection(context.ClientId);
            return new BuiltinToolResult 
            { 
                WasProcessed = true, 
                ContinueProcessing = false, // Stop the loop for user input
                ResultMessage = $"User interjection: {interjection.Content}",
                UserInterjection = interjection
            };
        }

        // Handle built-in tools
        var tool = await _toolService.GetToolByToolNameAsync(toolName);
        if (tool != null && tool.IsBuiltIn)
        {
            // Notify UI of tool execution start
            _notificationService.NotifyToolExecutionStart(context.ClientId, toolName);
            
            var result = await _builtinToolService.ProcessBuiltinToolAsync(
                tool.Name, 
                toolParameters, 
                tool.ExtraProperties, 
                context.ClientId);

            // Notify UI of tool execution completion
            _notificationService.NotifyToolExecutionComplete(context.ClientId, toolName, result.ResultMessage);
            
            return result;
        }

        // Handle MCP tools
        var serverDefinitions = await _mcpService.GetAllServerDefinitionsAsync();
        if (toolName.Contains("_") && serverDefinitions.Any(x => toolName.StartsWith(x.Id + "_")))
        {
            var serverId = toolName.Split('_')[0];
            var actualToolName = string.Join("_", toolName.Split('_').Skip(1));
            var args = JsonConvert.DeserializeObject<Dictionary<string, object>>(toolParameters);
            
            // Notify UI of MCP tool execution start
            _notificationService.NotifyToolExecutionStart(context.ClientId, toolName);
            
            var mcpResult = await _mcpService.CallToolAsync(serverId, actualToolName, args);
            
            var result = new BuiltinToolResult 
            { 
                WasProcessed = true, 
                ContinueProcessing = true, // Provider will decide when to stop
                ResultMessage = JsonConvert.SerializeObject(mcpResult.Content) 
            };

            // Notify UI of MCP tool execution completion
            _notificationService.NotifyToolExecutionComplete(context.ClientId, toolName, result.ResultMessage);
            
            return result;
        }

        return new BuiltinToolResult 
        { 
            WasProcessed = false, 
            ContinueProcessing = false,
            ResultMessage = $"Tool '{toolName}' not found." 
        };
    }

    public async Task<IEnumerable<Tool>> GetAvailableToolsAsync(IEnumerable<string> toolIds)
    {
        var allTools = await _toolService.GetAllToolsAsync();
        return allTools.Where(t => toolIds.Contains(t.Guid));
    }
}
```

### Step 3: Modify IAiService Interface

**File**: `AiStudio4/Services/Interfaces/IAiService.cs`

```csharp
using AiStudio4.DataModels;
using AiStudio4.Core.Interfaces;
using System.Threading.Tasks;

public interface IAiService
{
    /// <summary>
    /// Fetches a response from the AI provider with full tool loop management.
    /// The provider handles all tool calling, execution coordination, and loop control.
    /// </summary>
    /// <param name="options">Request options including conversation, model, settings, etc.</param>
    /// <param name="toolExecutor">Interface for executing tools locally</param>
    /// <param name="clientId">Client ID for status updates and interjections</param>
    /// <returns>Final AI response after all tool executions are complete</returns>
    Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, IToolExecutor toolExecutor, string clientId);

    /// <summary>
    /// Legacy method - use FetchResponseWithToolLoop for new implementations.
    /// </summary>
    [Obsolete("Use FetchResponseWithToolLoop to let the provider handle the tool loop.")]
    Task<AiResponse> FetchResponse(AiRequestOptions options, bool forceNoTools = false);
    
    // Existing properties remain unchanged
    string Name { get; }
    bool SupportsImages { get; }
    bool SupportsAudio { get; }
    bool SupportsCaching { get; }
    bool SupportsToolUse { get; }
    ToolFormat ToolFormat { get; }
}
```

### Step 4: Extend AiRequestOptions

**File**: `AiStudio4/DataModels/AiRequestOptions.cs`

Add new properties to support provider-managed tool loops:

```csharp
public class AiRequestOptions
{
    // Existing properties...
    
    /// <summary>
    /// Maximum number of tool execution iterations before stopping the loop.
    /// Default is 10 if not specified.
    /// </summary>
    public int? MaxToolIterations { get; set; }

    /// <summary>
    /// Whether to allow user interjections during tool execution.
    /// Default is true.
    /// </summary>
    public bool AllowInterjections { get; set; } = true;

    /// <summary>
    /// Callback for tool execution progress updates.
    /// </summary>
    public Action<string, string> OnToolExecutionUpdate { get; set; }
}
```

### Step 5: Implement Provider-Specific Tool Loops

#### Claude Implementation

**File**: `AiStudio4/AiServices/Claude.cs`

```csharp
public async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, IToolExecutor toolExecutor, string clientId)
{
    InitializeHttpClient(options.ServiceProvider, options.Model, options.ApiSettings);

    var linearConv = options.Conv;
    linearConv.systemprompt = options.CustomSystemPrompt ?? linearConv.systemprompt;
    
    var maxIterations = options.MaxToolIterations ?? 10;
    
    for (int iteration = 0; iteration < maxIterations; iteration++)
    {
        // 1. Prepare the request payload with all available tools
        var req = CreateRequestPayload(ApiModel, linearConv, options.ApiSettings);
        
        // Add all available tools to the request
        var availableTools = await toolExecutor.GetAvailableToolsAsync(options.ToolIds);
        var toolRequestBuilder = new ToolRequestBuilder(null, null);
        
        foreach (var tool in availableTools)
        {
            var toolConfig = JObject.Parse(Regex.Replace(tool.Schema, @"^//.*\n", "", RegexOptions.Multiline));
            await toolRequestBuilder.AddToolToRequestAsync(req, tool.Guid, GetToolFormat());
        }
        
        if (availableTools.Any())
        {
            req["tool_choice"] = new JObject { ["type"] = "auto" };
        }

        // 2. Call the Claude API
        var content = new StringContent(req.ToString(), Encoding.UTF8, "application/json");
        var response = await HandleStreamingResponse(content, options.CancellationToken, options.OnStreamingUpdate, options.OnStreamingComplete);
        
        // 3. Check for final answer (no tool calls)
        if (response.ToolCalls == null || !response.ToolCalls.Any())
        {
            return response; // We're done, return the final text response
        }

        // 4. Add the AI's response with tool calls to conversation history
        var assistantContent = new JArray();
        
        // Add any text content first
        if (!string.IsNullOrEmpty(response.Content))
        {
            assistantContent.Add(new JObject 
            { 
                ["type"] = "text", 
                ["text"] = response.Content 
            });
        }

        // Add tool use blocks
        foreach (var toolCall in response.ToolCalls)
        {
            assistantContent.Add(new JObject
            {
                ["type"] = "tool_use",
                ["id"] = toolCall.ToolCallId,
                ["name"] = toolCall.ToolName,
                ["input"] = JObject.Parse(toolCall.Arguments.ToString())
            });
        }

        linearConv.messages.Add(new LinearConversationMessage
        {
            role = "assistant",
            content = assistantContent.ToString()
        });

        // 5. Execute tools and collect results
        var toolResults = new JArray();
        var shouldStopLoop = false;

        foreach (var toolCall in response.ToolCalls)
        {
            var context = new ToolExecutionContext
            {
                ClientId = clientId,
                CancellationToken = options.CancellationToken,
                Conversation = linearConv,
                CurrentIteration = iteration
            };

            var executionResult = await toolExecutor.ExecuteToolAsync(toolCall.ToolName, toolCall.Arguments.ToString(), context);
            
            // Check if tool execution indicates we should stop the loop
            if (!executionResult.ContinueProcessing || executionResult.UserInterjection != null)
            {
                shouldStopLoop = true;
                
                // If there's a user interjection, add it to the conversation
                if (executionResult.UserInterjection != null)
                {
                    linearConv.messages.Add(new LinearConversationMessage
                    {
                        role = "user",
                        content = executionResult.UserInterjection.Content
                    });
                    break; // Don't execute remaining tools, process the interjection
                }
            }

            toolResults.Add(new JObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = toolCall.ToolCallId,
                ["content"] = executionResult.ResultMessage,
                ["is_error"] = !executionResult.WasProcessed
            });
        }
        
        // 6. Add tool results to conversation history
        if (toolResults.Any())
        {
            linearConv.messages.Add(new LinearConversationMessage
            {
                role = "user",
                content = toolResults.ToString()
            });
        }

        // 7. Check if we should stop the loop
        if (shouldStopLoop)
        {
            // Continue the loop to let the AI respond to the interjection or tool stop
            continue;
        }
    }

    // If we've exceeded max iterations, return an error response
    return new AiResponse 
    { 
        Success = false, 
        Content = $"Exceeded maximum tool iterations ({maxIterations}). The AI may be stuck in a tool loop.",
        ContentBlocks = new List<ContentBlock> 
        { 
            new ContentBlock 
            { 
                Content = $"Exceeded maximum tool iterations ({maxIterations}). The AI may be stuck in a tool loop.",
                Type = ContentType.Text 
            } 
        } 
    };
}
```

#### OpenAI Implementation

**File**: `AiStudio4/AiServices/NetOpenAi.cs`

```csharp
public async Task<AiResponse> FetchResponseWithToolLoop(AiRequestOptions options, IToolExecutor toolExecutor, string clientId)
{
    // Similar structure to Claude but using OpenAI's function calling format
    var maxIterations = options.MaxToolIterations ?? 10;
    
    for (int iteration = 0; iteration < maxIterations; iteration++)
    {
        // 1. Prepare request with function definitions
        var requestBody = CreateRequestBody(options);
        
        // Add function definitions
        var availableTools = await toolExecutor.GetAvailableToolsAsync(options.ToolIds);
        if (availableTools.Any())
        {
            requestBody["functions"] = JArray.FromObject(availableTools.Select(ConvertToOpenAIFunction));
            requestBody["function_call"] = "auto";
        }

        // 2. Call OpenAI API
        var response = await CallOpenAIAPI(requestBody, options);
        
        // 3. Check for function calls
        var functionCall = response["choices"]?[0]?["message"]?["function_call"];
        if (functionCall == null)
        {
            // No function call, return final response
            return ConvertToAiResponse(response);
        }

        // 4. Add assistant message with function call
        options.Conv.messages.Add(new LinearConversationMessage
        {
            role = "assistant",
            content = response["choices"][0]["message"]["content"]?.ToString(),
            function_call = functionCall.ToString()
        });

        // 5. Execute function and add result
        var functionName = functionCall["name"]?.ToString();
        var functionArgs = functionCall["arguments"]?.ToString();
        
        var context = new ToolExecutionContext
        {
            ClientId = clientId,
            CancellationToken = options.CancellationToken,
            Conversation = options.Conv,
            CurrentIteration = iteration
        };

        var executionResult = await toolExecutor.ExecuteToolAsync(functionName, functionArgs, context);
        
        // Add function result to conversation
        options.Conv.messages.Add(new LinearConversationMessage
        {
            role = "function",
            name = functionName,
            content = executionResult.ResultMessage
        });

        // Check for stop conditions
        if (!executionResult.ContinueProcessing || executionResult.UserInterjection != null)
        {
            if (executionResult.UserInterjection != null)
            {
                options.Conv.messages.Add(new LinearConversationMessage
                {
                    role = "user",
                    content = executionResult.UserInterjection.Content
                });
            }
            continue; // Let AI respond to the change
        }
    }

    // Max iterations exceeded
    return new AiResponse 
    { 
        Success = false, 
        Content = $"Exceeded maximum tool iterations ({maxIterations})",
        ContentBlocks = new List<ContentBlock> { new ContentBlock { Content = $"Exceeded maximum tool iterations ({maxIterations})", Type = ContentType.Text } }
    };
}
```

### Step 6: Simplify DefaultChatService

**File**: `AiStudio4/Services/DefaultChatService.cs`

Replace the entire tool loop (lines 173-428) with:

```csharp
public async Task<ChatResponse> ProcessChatRequest(ChatRequest request, string assistantMessageId)
{
    try
    {
        // ... existing setup code for model, service, system prompt ...

        var toolExecutor = _serviceProvider.GetRequiredService<IToolExecutor>();
        
        var linearConversation = new LinearConv(DateTime.Now);
        // ... populate linearConversation from request.BranchedConv ...

        var requestOptions = new AiRequestOptions
        {
            ServiceProvider = service,
            Model = model,
            Conv = linearConversation,
            CancellationToken = request.CancellationToken,
            ApiSettings = _generalSettingsService.CurrentSettings.ToApiSettings(),
            ToolIds = request.ToolIds,
            CustomSystemPrompt = systemPromptContent,
            MaxToolIterations = 10, // Could be configurable
            AllowInterjections = true,
            OnStreamingUpdate = (text) => _notificationService.NotifyStreamingUpdate(request.ClientId, new StreamingUpdateDto 
            { 
                ConversationId = request.ConversationId, 
                MessageId = assistantMessageId, 
                Content = text, 
                IsComplete = false 
            }),
            OnStreamingComplete = () => _notificationService.NotifyStreamingUpdate(request.ClientId, new StreamingUpdateDto 
            { 
                ConversationId = request.ConversationId, 
                MessageId = assistantMessageId, 
                IsComplete = true 
            })
        };

        // *** THE BIG CHANGE: Single call replaces entire tool loop ***
        AiResponse response = await aiService.FetchResponseWithToolLoop(requestOptions, toolExecutor, request.ClientId);
        
        // Process the final response
        var tokenCost = new TokenCost(response.TokenUsage, model, _tokenCostStrategyFactory.GetStrategy(service.Name));
        
        var finalMessage = request.BranchedConv.AddOrUpdateMessage(
            v4BranchedConversationMessageRole.Assistant,
            assistantMessageId,
            response.ContentBlocks,
            request.MessageId,
            response.Attachments,
            tokenCost
        );

        // Save conversation and notify client
        await _conversationStorage.SaveConversationAsync(request.BranchedConv);
        
        _notificationService.NotifyConversationUpdate(request.ClientId, new ConvUpdateDto
        {
            ConversationId = request.ConversationId,
            UpdatedMessage = finalMessage,
            TokenCost = tokenCost
        });

        return new ChatResponse { Success = true, AssistantMessageId = assistantMessageId };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing chat request");
        throw new ChatProcessingException("Failed to process chat request", ex);
    }
}
```

### Step 7: Update Dependency Injection

**File**: `AiStudio4/Core/DependencyInjection.cs`

```csharp
public static void ConfigureServices(IServiceCollection services)
{
    // ... existing registrations ...

    // Add the new ToolExecutor
    services.AddScoped<IToolExecutor, ToolExecutor>();

    // Keep ToolProcessorService for backwards compatibility during transition
    // services.AddScoped<IToolProcessorService, ToolProcessorService>(); // Can be removed later

    // ... other registrations ...
}
```

### Step 8: Handle Legacy Support

For backwards compatibility during the transition:

1. **Keep old methods marked as Obsolete**
2. **Implement fallback logic** in providers that don't support the new interface yet
3. **Gradual migration** - start with Claude, then OpenAI, then others

## Migration Strategy

### Phase 1: Foundation (Week 1)
1. Create `IToolExecutor` interface and `ToolExecutor` implementation
2. Add new method to `IAiService` interface
3. Update dependency injection
4. Test `ToolExecutor` with existing tools

### Phase 2: Claude Implementation (Week 2)
1. Implement `FetchResponseWithToolLoop` in `Claude.cs`
2. Test thoroughly with various tool combinations
3. Ensure conversation state management works correctly
4. Validate interjection handling

### Phase 3: OpenAI Implementation (Week 3)
1. Implement `FetchResponseWithToolLoop` in `NetOpenAi.cs`
2. Handle OpenAI-specific function calling format
3. Test and compare behavior with Claude implementation

### Phase 4: Remaining Providers (Week 4)
1. Implement in `Gemini.cs` and other providers
2. Handle provider-specific tool calling formats
3. Test all providers for consistency

### Phase 5: Cleanup (Week 5)
1. Update `DefaultChatService.cs` to use new method
2. Remove old tool loop logic
3. Remove `ToolProcessorService.cs` or refactor as needed
4. Update documentation and tests

## Benefits of New Architecture

### 1. **Provider-Specific Optimization**
- Each provider can use their native tool calling format
- Better handling of provider-specific features (e.g., Claude's thinking, OpenAI's function descriptions)
- Optimized conversation flow for each provider

### 2. **Cleaner Separation of Concerns**
- `DefaultChatService` focuses on request/response handling
- Providers manage their own tool loop logic
- `ToolExecutor` handles centralized tool execution

### 3. **Better Error Handling**
- Provider-specific error handling for tool calls
- Clearer error propagation and recovery
- Better debugging capabilities

### 4. **Improved Maintainability**
- Less complex conversation state management
- Provider-specific code isolated to provider classes
- Easier to add new providers

### 5. **Enhanced User Experience**
- Better streaming updates during tool execution
- More responsive interjection handling
- Provider-optimized tool calling patterns

## Testing Strategy

### Unit Tests
1. **ToolExecutor Tests**: Verify tool execution logic
2. **Provider Tests**: Test each provider's tool loop implementation
3. **Integration Tests**: Full chat flow with tools

### Integration Tests
1. **Cross-Provider Consistency**: Same tools produce similar results across providers
2. **Conversation State**: Verify conversation history is maintained correctly
3. **Interjection Handling**: Test user interruptions during tool execution

### Performance Tests
1. **Tool Loop Performance**: Compare old vs new implementation
2. **Memory Usage**: Ensure no memory leaks during long tool loops
3. **Concurrent Requests**: Multiple users with tool loops

## Risk Mitigation

### 1. **Backwards Compatibility**
- Keep old interface methods during transition
- Gradual rollout per provider
- Feature flags for new vs old implementation

### 2. **Error Handling**
- Comprehensive logging in tool execution
- Graceful degradation when tools fail
- Maximum iteration limits to prevent infinite loops

### 3. **Performance**
- Monitor tool execution times
- Implement timeouts for individual tools
- Resource usage monitoring

## Future Enhancements

### 1. **Tool Result Caching**
- Cache tool results for identical calls within a conversation
- Provider-specific caching strategies

### 2. **Parallel Tool Execution**
- Execute independent tools in parallel
- Provider support for parallel function calling

### 3. **Tool Chain Optimization**
- Analyze tool usage patterns
- Suggest optimal tool combinations
- Auto-generation of tool chains

## Conclusion

This refactoring provides a more maintainable, scalable, and provider-optimized architecture for AI tool execution. By moving the tool loop into each provider while maintaining centralized tool execution, we achieve the best of both worlds: provider-specific optimization and consistent tool behavior.

The gradual migration strategy ensures minimal disruption to existing functionality while allowing for thorough testing at each phase.