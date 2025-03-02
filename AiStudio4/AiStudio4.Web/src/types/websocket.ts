export interface WebSocketState {
    isConnected: boolean;
    clientId: string | null;
    messages: string[];
}