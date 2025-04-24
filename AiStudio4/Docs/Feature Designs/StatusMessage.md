# Status Message Component Design

## Overview
This document outlines the design for a new StatusMessage component that will display the current status of tool processing operations. The component will be positioned below the LoadingTimer in the ConvView component and will be backed by a Zustand store to manage its state.

## Requirements

1. Create a StatusMessage component that displays the current tool processing status
2. Component should be hidden when the status message is empty
3. Status messages will be sent via WebSocket (using the same stream as livestream tokens)
4. When tool processing begins, the server will send "Tools being processed"
5. When tool processing completes, the server will send an empty message ("")

## Technical Design

### 1. Frontend Components

#### StatusMessage Component
```tsx
// StatusMessage.tsx
import { useStatusMessageStore } from '@/stores/useStatusMessageStore';

export function StatusMessage() {
  const { message } = useStatusMessageStore();
  
  // Hide component when message is empty
  if (!message) return null;
  
  return (
    <div className="status-message-container p-2 mb-4 rounded-md text-sm font-medium">
      <span className="flex items-center gap-2">
        <span className="animate-pulse">⚙️</span>
        {message}
      </span>
    </div>
  );
}
```

#### Zustand Store
```tsx
// useStatusMessageStore.ts
import { create } from 'zustand';

interface StatusMessageState {
  message: string;
  setMessage: (message: string) => void;
}

export const useStatusMessageStore = create<StatusMessageState>((set) => ({
  message: '',
  setMessage: (message) => set({ message }),
}));
```

#### WebSocket Integration
Update the WebSocket hook to handle status message events:

```tsx
// useWebSocket.ts (modified)
import { useStatusMessageStore } from '@/stores/useStatusMessageStore';

export function useWebSocket(options: UseWebSocketOptions = {}): UseWebSocketResult {
  // Existing code...
  
  useEffect(() => {
    const { setMessage } = useStatusMessageStore.getState();
    
    // Add status message handler
    const handleStatusMessage = (data: any) => {
      setMessage(data.message || '');
    };
    
    webSocketService.subscribe('status', handleStatusMessage);
    
    return () => {
      webSocketService.unsubscribe('status', handleStatusMessage);
    };
  }, []);
  
  // Rest of existing code...
}
```

#### ConvView Integration
Update ConvView.tsx to include the StatusMessage component below the LoadingTimer:

```tsx
// ConvView.tsx (modified)
import { StatusMessage } from './StatusMessage';

// Inside the render function, below LoadingTimer:
<div className="overflow-hidden">  
  <LoadingTimer />
  <StatusMessage />
</div>
```

### 2. Backend Changes

#### WebSocketNotificationService Extension
Add a method to send status messages:

```csharp
// WebSocketNotificationService.cs
public async Task NotifyStatusMessage(string clientId, string message)
{
    try
    {
        if (string.IsNullOrEmpty(clientId)) throw new ArgumentNullException(nameof(clientId));

        var messageObj = new
        {
            messageType = "status",
            content = new
            {
                message = message
            }
        };

        await _webSocketServer.SendToClientAsync(clientId, JsonConvert.SerializeObject(messageObj));
        _logger.LogDebug("Sent status message to client {ClientId}: {Message}", clientId, message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send status message to client {ClientId}", clientId);
        throw new WebSocketNotificationException("Failed to send status message", ex);
    }
}
```

#### IWebSocketNotificationService Interface Update
Add the new method to the interface:

```csharp
// IWebSocketNotificationService.cs
public interface IWebSocketNotificationService
{
    // Existing methods...
    Task NotifyStatusMessage(string clientId, string message);
}
```

#### ToolProcessorService Integration
Update the ToolProcessorService to send status messages at the beginning and end of tool processing:

```csharp
// ToolProcessorService.cs
public async Task<ToolExecutionResult> ProcessToolsAsync(AiResponse response, LinearConv conv, StringBuilder collatedResponse, CancellationToken cancellationToken = default)
{
    // Get client ID from the conversation or request context
    string clientId = conv.ClientId; // Assuming this property exists
    
    try
    {
        // Send "Tools being processed" message at the start
        await _webSocketNotificationService.NotifyStatusMessage(clientId, "Tools being processed");
        
        // Existing tool processing code...
        
        // Rest of the method...
    }
    finally
    {
        // Always send empty message when done, regardless of success or failure
        await _webSocketNotificationService.NotifyStatusMessage(clientId, "");
    }
    
    // Return result as before
}
```

## Styling

The StatusMessage component will use a simple, unobtrusive design that fits with the existing UI:

- Background color slightly different from the message container
- Animated gear icon to indicate processing
- Consistent font and sizing with other UI elements
- Positioned directly below the LoadingTimer for logical grouping

## Implementation Timeline

1. Create the Zustand store for status messages
2. Implement the StatusMessage component
3. Update the WebSocket service to handle status message events
4. Modify the backend to send status messages during tool processing
5. Refine the implementation

## Conclusion

This design provides a simple, effective way to communicate tool processing status to users. By leveraging the existing WebSocket infrastructure and following the established UI patterns, the StatusMessage component will integrate seamlessly with the current application while providing valuable feedback to users during tool operations.