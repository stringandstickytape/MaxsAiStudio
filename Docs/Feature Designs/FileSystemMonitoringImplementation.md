# File System Monitoring Implementation

## Overview
This document describes the implementation of file system monitoring in the AiStudio application. The feature allows the server to monitor the project file system for changes (add, delete, rename) and notify all connected clients via WebSockets, which then update their state accordingly.

## Server-Side Implementation

### Components

1. **FileSystemChangedEventArgs**
   - Event arguments class that contains the current state of the file system (directories and files)
   - Located in `Core/Models/FileSystemChangedEventArgs.cs`

2. **IProjectFileWatcherService**
   - Interface that defines the contract for the file system watcher service
   - Added a new `FileSystemChanged` event that is raised when file system changes occur
   - Located in `InjectedDependencies/IProjectFileWatcherService.cs`

3. **ProjectFileWatcherService**
   - Implementation of `IProjectFileWatcherService`
   - Monitors the file system for changes using `FileSystemWatcher`
   - Raises the `FileSystemChanged` event when changes are detected
   - Located in `InjectedDependencies/ProjectFileWatcherService.cs`

4. **IWebSocketNotificationService**
   - Interface that defines methods for sending notifications via WebSockets
   - Added a new `NotifyFileSystemChanges` method
   - Located in `Core/Interfaces/IWebSocketNotificationService.cs`

5. **WebSocketNotificationService**
   - Implementation of `IWebSocketNotificationService`
   - Sends file system changes to all connected clients
   - Located in `Services/WebSocketNotificationService.cs`

6. **FileSystemChangeHandler**
   - Connects the file system watcher to the WebSocket notification service
   - Subscribes to the `FileSystemChanged` event and calls `NotifyFileSystemChanges`
   - Located in `InjectedDependencies/FileSystemChangeHandler.cs`

### Flow

1. `ProjectFileWatcherService` monitors the file system for changes
2. When a change is detected, it updates its internal state and raises the `FileSystemChanged` event
3. `FileSystemChangeHandler` receives the event and calls `NotifyFileSystemChanges` on the `WebSocketNotificationService`
4. `WebSocketNotificationService` sends a WebSocket message to all connected clients with the updated file system state

## Client-Side Implementation

### Components

1. **useFileSystemStore**
   - Zustand store that holds the file system state (directories and files)
   - Provides actions to update the state
   - Located in `AiStudioClient/src/stores/useFileSystemStore.ts`

2. **websocketUtils**
   - Utility functions for handling WebSocket messages
   - Includes a handler for file system update messages
   - Located in `AiStudioClient/src/utils/websocketUtils.ts`

3. **WebSocketService**
   - Service that manages WebSocket connections
   - Updated to use the `handleWebSocketMessage` utility
   - Located in `AiStudioClient/src/services/websocket/WebSocketService.ts`

### Flow

1. `WebSocketService` receives a WebSocket message
2. It passes the message to `handleWebSocketMessage`
3. If the message is a file system update, `handleWebSocketMessage` updates the `useFileSystemStore`
4. Components that use the `useFileSystemStore` are automatically updated with the new state

## Message Format

### Server to Client

```json
{
  "messageType": "fileSystem",
  "content": {
    "directories": ["path/to/dir1", "path/to/dir2", ...],
    "files": ["path/to/file1", "path/to/file2", ...]
  }
}
```

## Usage

Components can access the file system state using the `useFileSystemStore` hook:

```typescript
import { useFileSystemStore } from '@/stores/useFileSystemStore';

function MyComponent() {
  const { directories, files } = useFileSystemStore();
  
  // Use directories and files in the component
  return (
    <div>
      <h2>Directories</h2>
      <ul>
        {directories.map(dir => <li key={dir}>{dir}</li>)}
      </ul>
      <h2>Files</h2>
      <ul>
        {files.map(file => <li key={file}>{file}</li>)}
      </ul>
    </div>
  );
}
```

Components can also listen for file system updates using the `file-system-updated` event:

```typescript
import { useEffect } from 'react';

function MyComponent() {
  useEffect(() => {
    const handleFileSystemUpdated = () => {
      // Do something when the file system is updated
    };
    
    window.addEventListener('file-system-updated', handleFileSystemUpdated);
    
    return () => {
      window.removeEventListener('file-system-updated', handleFileSystemUpdated);
    };
  }, []);
  
  // Rest of the component
}
```