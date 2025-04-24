# Tool Status Update Feature

## Overview

This feature allows individual tool implementations to send status updates during their execution. These updates are displayed to the user via the WebSocket notification system, providing real-time feedback on tool progress.

## Architecture

### Components

1. **BaseToolImplementation**
   - Added `_statusUpdateCallback` property (Action<string>)
   - Added `SetStatusUpdateCallback` method to set the callback
   - Added `SendStatusUpdate` method for tools to send updates

2. **ToolProcessorService**
   - Creates a status update callback for each tool
   - Passes the callback to the BuiltinToolService

3. **BuiltinToolService**
   - Updated to accept and pass the status update callback to tool implementations

4. **Individual Tools (e.g., CodeDiffTool)**
   - Use the `SendStatusUpdate` method to provide progress information

5. **WebSocketNotificationService**
   - Already had the capability to send status messages to clients

## Flow

1. User initiates a tool call
2. ToolProcessorService creates a status update callback that uses WebSocketNotificationService
3. The callback is passed to the tool implementation via BuiltinToolService
4. The tool calls SendStatusUpdate at key points during execution
5. Updates are sent to the client via WebSocket
6. The client UI displays the status messages

## Benefits

- Improved user experience with real-time feedback
- Better visibility into long-running operations
- Easier debugging and monitoring of tool execution
- Consistent status update mechanism across all tools

## Implementation Notes

- Status updates are optional - tools can choose whether to send them
- The status update mechanism is fault-tolerant - errors in sending updates won't break tool execution
- The implementation maintains separation of concerns - tools don't need to know about WebSockets

## Future Enhancements

- Add progress percentage to status updates
- Support for cancellation of long-running tool operations
- More detailed status reporting with structured data
- Status history for completed operations