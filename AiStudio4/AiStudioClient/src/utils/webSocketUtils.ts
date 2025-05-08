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


export function useStreamableWebSocketData<T = any>(
  eventType: WebSocketEventType,
  initialData: T[],
  options?: {
    resetOnEnd?: boolean;
    onReset?: () => void;
  },
) {
  const [data, setData] = useState<T[]>(initialData);
  const [isActive, setIsActive] = useState(false);
  
  const dataRef = useRef<T[]>(initialData);
  
  
  useEffect(() => {
    dataRef.current = data;
  }, [data]);

  const reset = useCallback(() => {
      // Directly clear the data state
      setData([]);
      
      // Also dispatch an event to clear streamTokens in ConvView
      const clearEvent = new CustomEvent('stream:clear', {
        detail: { cleared: true }
      });
      window.dispatchEvent(clearEvent);

    setIsActive(false);
    if (options?.onReset) options.onReset();
  }, [initialData, options]);

  
  useEffect(() => {
    if (data.length > 0 && !isActive) {
      setIsActive(true);
    } else if (data.length === 0 && isActive) {
      setIsActive(false);
    }
  }, [data.length, isActive]);

  
  useEffect(() => {
    
    const handleDataEvent = (detail: WebSocketEventDetail) => {
      setData((prev) => [...prev, detail.content]);
    };
    
    
    const handleEndEvent = () => {
      if (options?.resetOnEnd) {
        
        const content = dataRef.current.join('');
        const event = new CustomEvent('stream:finalized', {
          detail: { content }
        });
        window.dispatchEvent(event);
        
        
        requestAnimationFrame(() => {
          reset();
        });
      }
    };

    
    const unsubscribeData = listenToWebSocketEvent(eventType, handleDataEvent);
    let unsubscribeEnd: (() => void) | undefined;
    
    if (options?.resetOnEnd) {
      unsubscribeEnd = listenToWebSocketEvent('stream:end', handleEndEvent);
    }

    
    return () => {
      unsubscribeData();
      if (unsubscribeEnd) unsubscribeEnd();
    };
  }, [eventType, reset, options]);

  return { data, reset };
}