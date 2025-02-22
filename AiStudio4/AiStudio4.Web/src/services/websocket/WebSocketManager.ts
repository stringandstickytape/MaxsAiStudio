import { store } from '../../store/store';
import { addMessage, createConversation } from '../../store/conversationSlice';
import { Message } from '../../types/conversation';
import { eventBus } from '../messaging/EventBus';

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
    private streamTokenString: string = '';
    private connected: boolean = false;

    constructor() {
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
    }

    private handleEndStream = () => {
        this.streamTokenString = '';
        console.log('Stream ended - cleared stream token string');
    }

    private handleCachedConversationMessage = (content: any) => {
        console.log('Received cached conversation message:', content);
        const cachedConversation = {
            convGuid: content.id,
            summary: content.content,
            fileName: `conv_${content.id}.json`,
            lastModified: content.lastModified || new Date().toISOString(),
            highlightColour: undefined
        };
        // Notify subscribers to update the CachedConversationList
    }

    private handleLoadConversation = (content: any) => {
        console.log('Loading conversation:', content);
        const { conversationId, messages } = content;

        if (!messages || messages.length === 0) return;

        // Find the first message with no parent (should be system/root message)
        const rootMessageIndex = messages.findIndex(m => !m.parentId);
        if (rootMessageIndex === -1) return;

        // Initialize conversation with the root message
        store.dispatch(createConversation({
            id: conversationId,
            rootMessage: {
                id: messages[rootMessageIndex].id,
                content: messages[rootMessageIndex].content,
                source: messages[rootMessageIndex].source,
                parentId: null,
                timestamp: messages[rootMessageIndex].timestamp,
                children: []
            }
        }));

        // Add remaining messages in chronological order
        // Skip the root message since we already added it
        messages.forEach((message: any, index: number) => {
            if (index !== rootMessageIndex) {
                store.dispatch(addMessage({
                    conversationId,
                    message: {
                        id: message.id,
                        content: message.content,
                        source: message.source,
                        parentId: message.parentId,
                        timestamp: message.timestamp,
                        children: []
                    }
                }));
            }
        });
    }

    private handleConversationMessage = (content: Message) => {
        console.log('Received conversation message:', content);

        // For new conversation root messages
        if (!content.parentId) {
            const state = store.getState();
            const existingConversationId = Object.keys(state.conversations.conversations).find(
                id => state.conversations.conversations[id].messages.some(m => m.id === content.id)
            );
            const conversationId = existingConversationId || `conv_${Date.now()}`;
            
            store.dispatch(createConversation({
                id: conversationId,
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

    public send(message: WebSocketMessage) {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify(message));
        }
    }

    private handleOpen = () => {
        console.log('WebSocket Connected');
        this.connected = true;
        // Emit connection status via EventBus so that the messaging service and hooks are notified
        eventBus.emit('connectionStatus', { isConnected: true, clientId: this.config.clientId });
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
            } else if (message.messageType === 'cfrag') {
                this.handleNewLiveChatStreamToken(message.content);
            } else if (message.messageType === 'conversation') {
                this.handleConversationMessage(message.content);
            } else if (message.messageType === 'cachedconversation') {
                this.handleCachedConversationMessage(message.content);
            } else if (message.messageType === 'loadConversation') {
                this.handleLoadConversation(message.content);
            } else if (message.messageType === 'endstream') {
                this.handleEndStream();
            }

            eventBus.emit(message.messageType, message.content);
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
        this.connected = false;
        // Emit connection status via EventBus so that subscribers (e.g., our messaging service) know the connection is down
        eventBus.emit('connectionStatus', { isConnected: false, clientId: null });
    }

    private handleNewLiveChatStreamToken = (token: string) => {
        // Concatenate the new token with existing tokens
        this.streamTokenString = this.streamTokenString + token;
        console.log('WebSocket Manager - Current stream token string:', this.streamTokenString);
    }

    public disconnect() {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.close();
        }
    }

    public getClientId(): string | undefined {
        return this.config.clientId;
    }

    public isConnected(): boolean {
        return this.connected;
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