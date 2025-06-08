
export type WebSocketEventType =
  | 'connection:status'
  | 'message:received'
  | 'cfrag'
  | 'endstream'
  | 'conv:upd'
  | 'conv:load'
  | 'historical:update'
  | 'request:cancelled'
  | 'transcription:received'    ;

export interface WebSocketEventDetail {
  type: string;
  content?: any;
  timestamp: number;
  clientId?: string | null;
  messageType?: string;
}


export const dispatchWebSocketEvent = (eventType: WebSocketEventType, detail: Partial<WebSocketEventDetail>): void => {
  const fullDetail: WebSocketEventDetail = {
    type: detail.type || '',
    timestamp: detail.timestamp || Date.now(),
    clientId: detail.clientId,
    content: detail.content,
    messageType: detail.messageType,
    ...detail,
  };

  const event = new CustomEvent(eventType, {
    detail: fullDetail,
    bubbles: true,
    cancelable: true,
  });

  window.dispatchEvent(event);
  //console.debug(`WebSocket Event Dispatched: ${eventType}`, fullDetail);
};


export const listenToWebSocketEvent = (
  eventType: WebSocketEventType,
  handler: (detail: WebSocketEventDetail) => void,
): (() => void) => {
  const eventHandler = (event: Event) => {
    const customEvent = event as CustomEvent<WebSocketEventDetail>;
    handler(customEvent.detail);
  };

  window.addEventListener(eventType, eventHandler);
  return () => window.removeEventListener(eventType, eventHandler);
};

