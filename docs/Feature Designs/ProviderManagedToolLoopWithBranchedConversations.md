# Provider-Managed Tool Loop with Branched Conversations

## Problem Statement

The new provider-managed tool loop system has moved tool execution from `DefaultChatService` into each AI provider implementation. While this provides better provider-specific optimizations, it has created gaps in:

1. **Branched conversation updates** - Tool responses aren't added to the branched conversation during execution
2. **Client notifications** - Intermediate AI responses (tool calls) aren't sent to the client
3. **Tool context** - Tools don't have access to the branched conversation
4. **Real-time updates** - Users can't see tool execution progress in the UI

## Solution Design

### 1. Enhanced Callback System

Extend `AiRequestOptions` with new callbacks for conversation management:

```csharp
public class AiRequestOptions
{
    // Existing properties...
    
    // New callbacks for conversation updates
    public Func<v4BranchedConvMessage, Task> OnAssistantMessageCreated { get; set; }
    public Func<string, List<ContentBlock>, List<ToolResponseItem>, Task> OnToolCallsGenerated { get; set; }
    public Func<string, BuiltinToolResult, Task> OnToolExecuted { get; set; }
    public Func<string, string, Task> OnUserInterjection { get; set; }
    
    // Pass branched conversation reference
    public v4BranchedConv BranchedConversation { get; set; }
    public string ParentMessageId { get; set; }
    public string AssistantMessageId { get; set; }
    public string ClientId { get; set; }
}
```

### 2. Conversation Update Interface

Create an interface for managing conversation updates during tool loops:

```csharp
public interface IConversationUpdateHandler
{
    /// <summary>
    /// Called when AI generates an initial response (may include tool calls)
    /// </summary>
    Task OnAssistantResponseAsync(
        v4BranchedConv conversation,
        string messageId,
        string parentId,
        List<ContentBlock> contentBlocks,
        List<ToolResponseItem> toolCalls,
        TokenUsage tokenUsage);
    
    /// <summary>
    /// Called after each tool execution
    /// </summary>
    Task OnToolExecutionCompleteAsync(
        v4BranchedConv conversation,
        string messageId,
        string toolName,
        BuiltinToolResult result);
    
    /// <summary>
    /// Called when user interjects during tool execution
    /// </summary>
    Task OnUserInterjectionAsync(
        v4BranchedConv conversation,
        string parentMessageId,
        string newMessageId,
        string interjectionContent);
    
    /// <summary>
    /// Called when tool loop completes with final response
    /// </summary>
    Task OnFinalResponseAsync(
        v4BranchedConv conversation,
        string messageId,
        List<ContentBlock> contentBlocks,
        TokenUsage tokenUsage,
        TokenCost costInfo);
}
```

### 3. Implementation in DefaultChatService

Create conversation update handler in `DefaultChatService`:

```csharp
private class ConversationUpdateHandler : IConversationUpdateHandler
{
    private readonly IWebSocketNotificationService _notificationService;
    private readonly string _clientId;
    private readonly string _convId;
    
    public ConversationUpdateHandler(
        IWebSocketNotificationService notificationService,
        string clientId,
        string convId)
    {
        _notificationService = notificationService;
        _clientId = clientId;
        _convId = convId;
    }
    
    public async Task OnAssistantResponseAsync(
        v4BranchedConv conversation,
        string messageId,
        string parentId,
        List<ContentBlock> contentBlocks,
        List<ToolResponseItem> toolCalls,
        TokenUsage tokenUsage)
    {
        // Add message to branched conversation
        var message = conversation.AddOrUpdateMessage(
            v4BranchedConvMessageRole.Assistant,
            messageId,
            contentBlocks,
            parentId);
        
        // Notify client of new message
        await _notificationService.NotifyConvUpdate(_clientId, new ConvUpdateDto
        {
            ConvId = _convId,
            MessageId = messageId,
            ContentBlocks = contentBlocks,
            ParentId = parentId,
            Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
            Source = "assistant",
            TokenUsage = tokenUsage,
            ToolCalls = toolCalls?.Select(t => new ToolCallDto 
            { 
                ToolName = t.ToolName, 
                Status = "pending" 
            }).ToList()
        });
    }
    
    public async Task OnToolExecutionCompleteAsync(
        v4BranchedConv conversation,
        string messageId,
        string toolName,
        BuiltinToolResult result)
    {
        // Update tool status in UI
        await _notificationService.NotifyToolUpdate(_clientId, new ToolUpdateDto
        {
            ConvId = _convId,
            MessageId = messageId,
            ToolName = toolName,
            Status = result.WasProcessed ? "completed" : "failed",
            Result = result.ResultMessage
        });
    }
    
    // ... other methods
}
```

### 4. Modified Provider Implementation

Update `Claude.cs` to use the new callbacks:

```csharp
public override async Task<AiResponse> FetchResponseWithToolLoop(
    AiRequestOptions options,
    IToolExecutor toolExecutor,
    v4BranchedConv branchedConv,
    string parentMessageId,
    string assistantMessageId,
    string clientId)
{
    // Extract linear conversation from branched
    var linearConv = ConvertToLinearConversation(branchedConv, parentMessageId);
    
    for (int iteration = 0; iteration < maxIterations; iteration++)
    {
        // Make API call
        var response = await FetchResponse(options);
        
        // Notify about assistant response (including tool calls)
        if (options.OnAssistantMessageCreated != null)
        {
            var message = branchedConv.AddOrUpdateMessage(
                v4BranchedConvMessageRole.Assistant,
                assistantMessageId,
                response.ContentBlocks,
                parentMessageId,
                response.Attachments,
                null); // Cost info added later
            
            await options.OnAssistantMessageCreated(message);
        }
        
        // Notify about tool calls if any
        if (response.ToolResponseSet?.Tools?.Any() == true && options.OnToolCallsGenerated != null)
        {
            await options.OnToolCallsGenerated(
                assistantMessageId,
                response.ContentBlocks,
                response.ToolResponseSet.Tools);
        }
        
        // Check for final answer
        if (response.ToolResponseSet == null || !response.ToolResponseSet.Tools.Any())
        {
            return response; // Done
        }
        
        // Execute tools
        foreach (var toolCall in response.ToolResponseSet.Tools)
        {
            var context = new ToolExecutionContext
            {
                ClientId = clientId,
                CancellationToken = options.CancellationToken,
                BranchedConversation = branchedConv, // Now available!
                LinearConversation = linearConv,
                CurrentIteration = iteration
            };
            
            var result = await toolExecutor.ExecuteToolAsync(
                toolCall.ToolName,
                toolCall.ResponseText,
                context);
            
            // Notify about tool execution
            if (options.OnToolExecuted != null)
            {
                await options.OnToolExecuted(assistantMessageId, result);
            }
            
            // Handle interjections
            if (result.UserInterjection != null && options.OnUserInterjection != null)
            {
                var interjectionId = Guid.NewGuid().ToString();
                await options.OnUserInterjection(interjectionId, result.UserInterjection);
                
                // Update parent for next iteration
                parentMessageId = interjectionId;
                assistantMessageId = Guid.NewGuid().ToString();
            }
        }
    }
    
    // Max iterations exceeded
    return CreateErrorResponse("Exceeded maximum tool iterations");
}
```

### 5. Enhanced WebSocket Notifications

Add new notification types for tool execution:

```csharp
public class ToolUpdateDto
{
    public string ConvId { get; set; }
    public string MessageId { get; set; }
    public string ToolName { get; set; }
    public string Status { get; set; } // "pending", "executing", "completed", "failed"
    public string Result { get; set; }
}

public class ConvUpdateDto
{
    // Existing properties...
    
    // Add tool call information
    public List<ToolCallDto> ToolCalls { get; set; }
}

public class ToolCallDto
{
    public string ToolName { get; set; }
    public string Status { get; set; }
    public string Parameters { get; set; }
}
```

### 6. Frontend Updates

The frontend can now:
1. Display tool calls as they're generated
2. Show real-time tool execution status
3. Display tool results inline
4. Handle interjections properly
5. Maintain full conversation tree visualization

## Implementation Steps

1. **Update Core Interfaces**
   - Extend `AiRequestOptions` with new callbacks
   - Add branched conversation to `ToolExecutionContext`
   - Create `IConversationUpdateHandler` interface

2. **Update DefaultChatService**
   - Implement `ConversationUpdateHandler`
   - Wire up callbacks when creating `AiRequestOptions`
   - Pass branched conversation reference

3. **Update Provider Implementations**
   - Modify `Claude.cs` to use callbacks
   - Update other providers as needed
   - Ensure backward compatibility

4. **Update WebSocket Service**
   - Add new notification types
   - Implement tool update notifications
   - Test real-time updates

5. **Update Frontend**
   - Handle new WebSocket message types
   - Display tool execution status
   - Update conversation tree visualization

## Benefits

1. **Real-time Updates**: Users see tool execution progress
2. **Complete History**: All tool calls recorded in branched conversation
3. **Better Context**: Tools have access to full conversation
4. **Provider Flexibility**: Each provider can optimize its tool loop
5. **Maintainability**: Clean separation of concerns

## Backward Compatibility

- Callbacks are optional - providers work without them
- Existing tool implementations continue to work
- Frontend gracefully handles missing updates
- Migration can be done incrementally

## Testing Strategy

1. Unit tests for callback system
2. Integration tests for each provider
3. E2E tests for real-time updates
4. Performance tests for callback overhead
5. Regression tests for existing functionality