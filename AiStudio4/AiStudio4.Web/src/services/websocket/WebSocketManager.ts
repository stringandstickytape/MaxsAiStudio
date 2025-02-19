import { store } from '../../store/store';
import { addMessage, createConversation } from '../../store/conversationSlice';
import { Message } from '../../types/conversation';

// src/services/websocket/types.ts
export interface WebSocketMessage {
    messageType: string;
    content: any;
}

export interface LiveChatStreamToken {
    token: string;
    timestamp: number;
}

export interface ClientConfig {
    clientId?: string;
    // Add other client-specific config
}

// src/services/websocket/WebSocketManager.ts
class WebSocketManager {
    private socket: WebSocket | null = null;
    private config: ClientConfig = {};
    private messageHandlers: Map<string, ((data: any) => void)[]> = new Map();
    private streamTokenString: string = '';

    constructor() {
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
    }

    private handleConversationMessage = (content: Message) => {
        console.log('Received conversation message:', content);

        // For new conversation root messages
        if (!content.parentId) {
            store.dispatch(createConversation({
                rootMessage: {
                    id: content.id,
                    content: content.content,
                    source: content.source,
                    parentId: null,
                    timestamp: Date.now(),
                    children: []
                }
            }));
        }
        // For replies/branches
        else {
            const state = store.getState();
            const activeConversationId = state.conversations.activeConversationId;
            if (activeConversationId) {
                store.dispatch(addMessage({
                    conversationId: activeConversationId,
                    message: {
                        id: content.id,
                        content: content.content,
                        source: content.source,
                        parentId: content.parentId,
                        timestamp: Date.now(),
                        children: []
                    }
                }));
            }
        }
    };

    public connect() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.socket = new WebSocket(`${protocol}//${window.location.host}/ws`);

        this.socket.addEventListener('open', this.handleOpen);
        this.socket.addEventListener('message', this.handleMessage);
        this.socket.addEventListener('error', this.handleError);
        this.socket.addEventListener('close', this.handleClose);
        // Handle stream tokens through the message handler instead
    }

    public subscribe(messageType: string, handler: (data: any) => void) {
        const handlers = this.messageHandlers.get(messageType) || [];
        handlers.push(handler);
        this.messageHandlers.set(messageType, handlers);
    }

    public unsubscribe(messageType: string, handler: (data: any) => void) {
        const handlers = this.messageHandlers.get(messageType) || [];
        this.messageHandlers.set(
            messageType,
            handlers.filter(h => h !== handler)
        );
    }

    public send(message: WebSocketMessage) {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify(message));
        }
    }

    private handleOpen = () => {
        console.log('WebSocket Connected');
    }

    private handleMessage = (event: MessageEvent) => {
        try {
            console.log('Raw message received:', event.data);
            const message: WebSocketMessage = JSON.parse(event.data);
            console.log('Parsed message:', message);

            // Handle client ID message specially
            if (message.messageType === 'clientId') {
                this.config.clientId = message.content;
                console.log('set client id to ' + this.config.clientId);
            } else if (message.messageType === 'c') {
                this.handleNewLiveChatStreamToken(message.content);
            } else if (message.messageType === 'conversation') {
                this.handleConversationMessage(message.content);
            }

            // Notify all handlers for this message type
            const handlers = this.messageHandlers.get(message.messageType) || [];
            handlers.forEach(handler => handler(message.content));
        } catch (error) {
            console.error('Error processing message:', error);
        }
    }

    private handleError = (event: Event) => {
        console.error('WebSocket error:', event);
    }

    private handleClose = () => {
        console.log('WebSocket disconnected');
        this.socket = null;
    }

    private handleNewLiveChatStreamToken = (token: string) => {
        // Concatenate the new token with existing tokens
        this.streamTokenString = this.streamTokenString + token;
        console.log('WebSocket Manager - Current stream token string:', this.streamTokenString);
        // Notify subscribers about the new token
        const handlers = this.messageHandlers.get('newStreamToken') || [];
        handlers.forEach(handler => handler(token));
    }

    public disconnect() {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.close();
        }
    }

    public getClientId(): string | undefined {
        return this.config.clientId;
    }

    public getStreamTokens(): string {
        return this.streamTokenString;
    }

    public clearStreamTokens() {
        this.streamTokenString = '';
    }

    public getLatestStreamToken(): string {
        return this.streamTokenString;
    }
}

// Create a singleton instance
export const wsManager = new WebSocketManager();