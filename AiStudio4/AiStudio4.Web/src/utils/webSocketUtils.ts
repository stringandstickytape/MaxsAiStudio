
import { useState, useEffect, useCallback, useRef } from 'react';
import { WebSocketEventType, WebSocketEventDetail, listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';


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
  const [isConnected, setIsConnected] = useState(false);
  const [clientId, setClientId] = useState<string | null>(null);

  useEffect(() => {
    const handleConnectionEvent = (detail: WebSocketEventDetail) => {
      if (detail.type === 'connected') {
        setIsConnected(true);
      } else if (detail.type === 'disconnected') {
        setIsConnected(false);
      } else if (detail.type === 'clientId' && detail.clientId) {
        setClientId(detail.clientId);
      }

      if (onStatusChange && detail.content) {
        onStatusChange(detail.content as WebSocketConnectionStatus);
      }
    };

    const unsubscribe = listenToWebSocketEvent('connection:status', handleConnectionEvent);
    return unsubscribe;
  }, [onStatusChange]);

  return { isConnected, clientId };
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
    setData(initialData);
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

