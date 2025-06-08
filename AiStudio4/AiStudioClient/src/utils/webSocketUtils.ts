import { useState, useEffect, useCallback, useRef } from 'react';
import { WebSocketEventType, WebSocketEventDetail, listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';
import { useFileSystemStore } from '@/stores/useFileSystemStore';

/**
 * Handles WebSocket messages received from the server
 * @param message The message received from the server
 */
export function handleWebSocketMessage(message: any) {
  // Parse the message if it's a string
  const data = typeof message === 'string' ? JSON.parse(message) : message;
  
  // Handle different message types
  switch (data.messageType) {
    case 'fileSystem':
      handleFileSystemUpdate(data.content);
      break;
      
    // Other message types are handled elsewhere
    default:
      break;
  }
}

/**
 * Handles file system update messages
 * @param content The file system update content
 */
function handleFileSystemUpdate(content: { directories: string[], files: string[] }) {
  const { updateFileSystem } = useFileSystemStore.getState();
  updateFileSystem(content.directories, content.files);
  
  // Dispatch an event for components to react to
  window.dispatchEvent(new CustomEvent('file-system-updated'));
}

export function useWebSocketEvent<T = any>(
  eventType: WebSocketEventType,
  onEvent?: (detail: WebSocketEventDetail) => void,
  deps: any[] = [],
) {
  const [eventData, setEventData] = useState<T | null>(null);

  useEffect(() => {
    const handler = (detail: WebSocketEventDetail) => {
      setEventData(detail.content as T);
      if (onEvent) onEvent(detail);
    };

    const unsubscribe = listenToWebSocketEvent(eventType, handler);
    return unsubscribe;
    
  }, [eventType, ...deps]);

  return eventData;
}


export function useWebSocketStatus(onStatusChange?: (status: WebSocketConnectionStatus) => void) {
  const [status, setStatus] = useState<WebSocketConnectionStatus>({
    isConnected: webSocketService.isConnected(),
    clientId: webSocketService.getClientId()
  });

  useEffect(() => {
    
    const unsubscribe = webSocketService.onConnectionStatusChange((newStatus) => {
      setStatus(newStatus);
      if (onStatusChange) {
        onStatusChange(newStatus);
      }
    });
    
    return () => {
      if (unsubscribe) webSocketService.offConnectionStatusChange(unsubscribe);
    };
  }, [onStatusChange]);

  return { 
    isConnected: status.isConnected, 
    clientId: status.clientId 
  };
}


