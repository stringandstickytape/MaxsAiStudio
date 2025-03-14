// src/services/websocket/websocketEvents.ts
export type WebSocketEventType = 
  | 'connection:status' 
  | 'message:received' 
  | 'stream:token' 
  | 'stream:end'
  | 'conversation:new'
  | 'conversation:load'
  | 'historical:update';

export interface WebSocketEventDetail {
  type: string;
  content?: any;
  timestamp: number;
  clientId?: string | null;
  messageType?: string;
}

/**
 * Dispatch a WebSocket-related event through the DOM event system
 */
export const dispatchWebSocketEvent = (
  eventType: WebSocketEventType, 
  detail: Partial<WebSocketEventDetail>
): void => {
  const fullDetail: WebSocketEventDetail = {
    type: detail.type || '',
    timestamp: detail.timestamp || Date.now(),
    clientId: detail.clientId,
    content: detail.content,
    messageType: detail.messageType,
    ...detail
  };
  
  const event = new CustomEvent(eventType, { 
    detail: fullDetail,
    bubbles: true,
    cancelable: true
  });
  
  window.dispatchEvent(event);
  console.debug(`WebSocket Event Dispatched: ${eventType}`, fullDetail);
};

/**
 * Listen for WebSocket-related events
 * Returns an unsubscribe function
 */
export const listenToWebSocketEvent = (
  eventType: WebSocketEventType,
  handler: (detail: WebSocketEventDetail) => void
): () => void => {
  const eventHandler = (event: Event) => {
    const customEvent = event as CustomEvent<WebSocketEventDetail>;
    handler(customEvent.detail);
  };
  
  window.addEventListener(eventType, eventHandler);
  return () => window.removeEventListener(eventType, eventHandler);
};