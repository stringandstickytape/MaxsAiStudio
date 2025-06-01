# Feature Design: Interjection During Tool Loop

## Overview

Currently, when a user sends a message and the AI begins a tool use loop, the input area becomes read-only until the entire tool loop completes. This prevents users from providing additional information or context while the AI is processing tools, which can be frustrating when the loop takes time or when the user realizes they need to add information.

This feature will allow users to interject with additional messages during an ongoing tool loop. These interjections will be incorporated into the next phase of the tool loop.

## User Experience

1. User sends a message to the AI
2. AI begins responding with tool use (enters a tool loop)
3. Input area is cleared (not disabled) after sending the initial message
4. User can type a new message while waiting for the tool loop to complete
5. User presses Ctrl+Return to send an interjection
6. The interjection is sent to the server and stored
7. When the current tool loop phase completes, the interjection is prepended to the next user message in the tool loop
8. After the interjection is processed, the input area is cleared again

## Technical Design

### Server-Side Components

#### 1. Interjection Service

Create a new service to store and retrieve interjections:

```csharp
public interface IInterjectionService
{
    Task StoreInterjectionAsync(string clientId, string interjection);
    Task<string> GetAndClearInterjectionAsync(string clientId);
    Task<bool> HasInterjectionAsync(string clientId);
}

public class InterjectionService : IInterjectionService
{
    private readonly ConcurrentDictionary<string, string> _interjections = new();

    public Task StoreInterjectionAsync(string clientId, string interjection)
    {
        _interjections[clientId] = interjection;
        return Task.CompletedTask;
    }

    public Task<string> GetAndClearInterjectionAsync(string clientId)
    {
        if (_interjections.TryRemove(clientId, out var interjection))
            return Task.FromResult(interjection);
        return Task.FromResult(string.Empty);
    }

    public Task<bool> HasInterjectionAsync(string clientId)
    {
        return Task.FromResult(_interjections.ContainsKey(clientId));
    }
}
```

#### 2. WebSocket Message Handler

Add a new message type for interjections in the WebSocket message handler:

```csharp
// In WebSocketMessageHandler.cs
private async Task HandleClientMessageAsync(string clientId, string message)
{
    var messageObj = JsonConvert.DeserializeObject<dynamic>(message);
    string messageType = messageObj.messageType;

    switch (messageType)
    {
        // Existing cases...
        
        case "interject":
            await HandleInterjectionAsync(clientId, messageObj);
            break;
    }
}

private async Task HandleInterjectionAsync(string clientId, dynamic messageObj)
{
    string interjection = messageObj.content.message;
    await _interjectionService.StoreInterjectionAsync(clientId, interjection);
    
    // Send acknowledgment back to client
    await _webSocketConnectionManager.SendMessageAsync(clientId, JsonConvert.SerializeObject(new
    {
        messageType = "interjectionAck",
        content = new { success = true }
    }));
}
```

#### 3. DefaultChatService Modification

Modify the tool loop in DefaultChatService to check for and incorporate interjections:

```csharp
// In ProcessChatRequest method of DefaultChatService.cs
while (continueLoop && currentIteration < MAX_ITERATIONS)
{
    // Existing code...
    
    // Check for interjections before continuing the loop
    if (await _interjectionService.HasInterjectionAsync(request.ClientId))
    {
        string interjection = await _interjectionService.GetAndClearInterjectionAsync(request.ClientId);
        if (!string.IsNullOrEmpty(interjection))
        {
            // Prepend the interjection to the collated response
            collatedResponse.Insert(0, $"User interjection: {interjection}\n\n");
            
            // Notify the client that the interjection was processed
            await _notificationService.NotifyStatusMessageAsync(request.ClientId, "Your interjection has been added to the conversation.");
        }
    }
    
    // Continue with the tool loop...
}
```

### Client-Side Components

#### 1. InputBar Component Modification

Modify the InputBar component to support interjections:

```typescript
// In InputBar.tsx
const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
  // Check for Ctrl+Return to send interjection
  if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && inputText.trim() && isLoading) {
    e.preventDefault();
    sendInterjection(inputText);
    setInputText('');
  }
};

const sendInterjection = async (message: string) => {
  try {
    await webSocketService.sendMessage({
      messageType: 'interject',
      content: {
        message
      }
    });
  } catch (error) {
    console.error('Error sending interjection:', error);
  }
};

// In MessageInputArea component
<textarea
  // Existing props...
  disabled={disabled} // Not disabled during tool loops
  onKeyDown={handleKeyDown}
/>
```

#### 2. WebSocket Service Extension

Extend the WebSocket service to handle interjection acknowledgments:

```typescript
// In WebSocketService.ts
private handleMessage(event: MessageEvent) {
  const data = JSON.parse(event.data);
  
  switch (data.messageType) {
    // Existing cases...
    
    case 'interjectionAck':
      // Optionally show a toast or notification
      toast.success('Interjection sent successfully');
      break;
  }
}
```

#### 3. User Interface Indicators

Add UI elements to indicate the interjection capability:

```typescript
// In InputBar.tsx
<div className="flex items-center text-xs text-gray-400 mt-1">
  {isLoading && (
    <span className="italic">
      Press Ctrl+Return to send an interjection during the current process
    </span>
  )}
</div>
```

## Implementation Steps

1. Create the InterjectionService and register it in the DI container
2. Modify the WebSocketMessageHandler to handle interjection messages
3. Update the DefaultChatService to check for and incorporate interjections
4. Modify the InputBar component to support sending interjections
5. Add UI indicators for the interjection capability
6. Test the feature with various scenarios

## Considerations

- **Timing**: Interjections will only be incorporated at the next phase of the tool loop, not immediately
- **Multiple Interjections**: If a user sends multiple interjections before the next loop phase, they will be combined
- **Cancellation**: Consider how interjections interact with request cancellation
- **UI Feedback**: Provide clear feedback when an interjection is sent and when it's incorporated

## Future Enhancements

- Allow interjections to be sent as separate messages in the conversation history
- Support for attachments in interjections
- Option to interrupt the current tool loop immediately with an interjection