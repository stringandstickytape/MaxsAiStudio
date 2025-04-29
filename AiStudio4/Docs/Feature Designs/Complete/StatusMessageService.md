# Status Message Service Design

## Overview

The Status Message Service will provide a centralized mechanism for sending status messages throughout the AiStudio4 application. This service will abstract the existing functionality into a dedicated service without changing the current behavior or adding new features.

## Current Implementation

Currently, status messages are implemented through several components:

- **Backend**:
  - `BaseToolImplementation.cs` has a `SendStatusUpdate` method that tools use to send status updates
  - `WebSocketNotificationService.cs` has a `NotifyStatusMessage` method that sends status messages to clients
  - `ToolProcessorService.cs` sends status messages at the beginning and end of tool processing

- **Frontend**:
  - `useStatusMessageStore.ts` is a Zustand store that manages the current status message
  - `StatusMessage.tsx` is a component that displays the current status message
  - `useWebSocket.ts` subscribes to status messages from the WebSocket and updates the store

## Limitations of Current Implementation

1. **Lack of Centralization**: Status messages are sent directly from various components rather than through a centralized service
2. **Inconsistent Usage**: Some components send status messages directly, while others don't have access to the notification service

## Proposed Solution

Implement a simple `StatusMessageService` that will serve as the central point for sending status messages, without changing the current behavior or adding new features.

### Key Components

#### Backend

1. **IStatusMessageService Interface**
   ```csharp
   public interface IStatusMessageService
   {
       Task SendStatusMessageAsync(string clientId, string message);
       Task ClearStatusMessageAsync(string clientId);
   }
   ```

2. **StatusMessageService Implementation**
   ```csharp
   public class StatusMessageService : IStatusMessageService
   {
       private readonly IWebSocketNotificationService _webSocketNotificationService;
       private readonly ILogger<StatusMessageService> _logger;

       public StatusMessageService(
           IWebSocketNotificationService webSocketNotificationService,
           ILogger<StatusMessageService> logger)
       {
           _webSocketNotificationService = webSocketNotificationService;
           _logger = logger;
       }

       public async Task SendStatusMessageAsync(string clientId, string message)
       {
           try
           {
               if (string.IsNullOrEmpty(clientId))
               {
                   _logger.LogWarning("Cannot send status message: clientId is null or empty");
                   return;
               }

               await _webSocketNotificationService.NotifyStatusMessage(clientId, message);
               _logger.LogDebug("Sent status message to client {ClientId}: {Message}", clientId, message);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Failed to send status message to client {ClientId}", clientId);
           }
       }

       public async Task ClearStatusMessageAsync(string clientId)
       {
           await SendStatusMessageAsync(clientId, "");
       }
   }
   ```

## Implementation Plan

### Phase 1: Backend Implementation

1. Create the `IStatusMessageService` interface in Core/Interfaces
2. Implement the `StatusMessageService` class in Services
3. Register the service in the DI container

### Phase 2: Integration

1. Update `BaseToolImplementation.cs` to use the new StatusMessageService
2. Update `ToolProcessorService.cs` to use the new StatusMessageService
3. Update any other components that currently send status messages directly

## Benefits

1. **Centralized Management**: Single point of control for all status messages
2. **Consistent Usage**: Standard patterns for status message creation
3. **Improved Testability**: Easier to mock and test status message functionality
4. **Better Maintainability**: Changes to status message behavior only need to be made in one place

## Future Considerations

While this design focuses solely on abstracting the existing functionality without adding new features, future enhancements could include:

1. Message priority levels
2. Message timeouts and auto-dismissal
3. Message history
4. Message queuing
5. Interactive messages

However, these enhancements are out of scope for the current implementation.

## Conclusion

The StatusMessageService will provide a simple abstraction over the existing status message functionality, making the code more maintainable and consistent without changing the current behavior or adding new features.