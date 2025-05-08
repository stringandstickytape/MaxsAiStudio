# Feature Design: Interjection During Tool Loop - Completed Implementation

## Overview

This feature allows users to provide additional information or context while the AI is processing their request. The implementation has been enhanced to accept interjections as soon as the server receives the chat message from the client, rather than waiting for the AI to begin responding with tool use.

## User Experience

1. User sends a message to the AI
2. Input area is cleared (not disabled) after sending the initial message
3. User can type a new message immediately after sending their request
4. User presses Ctrl+Return to send an interjection
5. The interjection is sent to the server and stored
6. If the AI hasn't started processing yet, the interjection is appended to the original message
7. If the AI is in a tool loop, the interjection is prepended to the next user message in the tool loop
8. After the interjection is processed, the input area is cleared again

## Technical Implementation

### Client-Side Components

#### 1. WebSocket Store (Zustand)

Added state tracking for when a message has been sent to the server:

```typescript
interface WebSocketStore {
  // Existing state...
  messageSent: boolean; // Track if a message has been sent and waiting for response
  
  // Existing methods...
  setMessageSent: (messageSent: boolean) => void; // Set message sent state
}
```

#### 2. InputBar Component

Modified to use the `messageSent` state to determine when to send interjections:

```typescript
// If a message has been sent to the server (even before AI starts responding with tools),
// treat any new input as an interjection
if (useWebSocketStore.getState().messageSent && !isCancelling && inputText.trim()) {
    webSocketService.sendInterjection(inputText);
    setInputText('');
    return;
}
```

#### 3. Chat Management Hook

Updated to set `messageSent` state when a message is sent:

```typescript
return executeApiCall(async () => {
    // Set messageSent to true in WebSocketStore
    const { setMessageSent } = useWebSocketStore.getState();
    setMessageSent(true);
    
    // Rest of the function...
});
```

### Server-Side Components

#### 1. DefaultChatService

Added interjection check at the beginning of the request processing:

```csharp
// Check for interjections at the beginning of the request processing
var interjectionService = _serviceProvider.GetService<IInterjectionService>();
if (interjectionService != null && await interjectionService.HasInterjectionAsync(request.ClientId))
{
    string interjection = await interjectionService.GetAndClearInterjectionAsync(request.ClientId);
    if (!string.IsNullOrEmpty(interjection))
    {
        // Append the interjection to the user's message
        var lastMessage = request.BranchedConv.GetMessageHistory(request.MessageId).LastOrDefault(m => m.Role == v4BranchedConvMessageRole.User);
        if (lastMessage != null)
        {
            lastMessage.UserMessage += $"\n\nUser interjection: {interjection}";
        }
        
        // Notify the client that the interjection was processed
        await _statusMessageService.SendStatusMessageAsync(request.ClientId, "Your interjection has been added to the conversation.");
    }
}
```

## Improvements Over Original Design

1. **Earlier Interjection Support**: Users can now interject as soon as they send their message, rather than waiting for the AI to begin tool use
2. **Better User Experience**: Provides immediate feedback and allows users to add forgotten context right away
3. **Seamless Integration**: Interjections are incorporated naturally into the conversation flow

## Future Enhancements

- Allow multiple interjections to be collected and sent as a batch
- Add visual indicators in the conversation history to show where interjections were added
- Support for attachments in interjections