import { LiveChatStreamToken } from '@/services/websocket/WebSocketManager';

export interface WebSocketState {
    isConnected: boolean;
    clientId: string | null;
    messages: string[];
    streamTokens: LiveChatStreamToken[];
}