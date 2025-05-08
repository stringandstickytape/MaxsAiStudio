export interface WebSocketState {
  isConnected: boolean;
  clientId: string | null;
  messages: string[];
  isCancelling?: boolean;
  currentRequest?: {
    convId: string;
    messageId: string;
  };
}
