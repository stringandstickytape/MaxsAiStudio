import { store } from '@/store/store';
import { addMessage, createConversation, setActiveConversation } from '@/store/conversationSlice';
import { Message } from '@/types/conversation';
import { eventBus } from '@/services/messaging/EventBus';

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
    private connected: boolean = false;

    constructor() {
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
    }

    private handleEndStream = () => {
        eventBus.emit('endstream', null);
    }

    private handleCachedConversationMessage = (content: any) => {
        
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
        const { conversationId, messages } = content;
        const urlParams = new URLSearchParams(window.location.search);
        const selectedMessageId = urlParams.get('messageId');

        if (!messages || messages.length === 0) return;

        // Find root message (message with no parent)
        const rootMessage = messages.find(m => !m.parentId) || messages[0];

        // Create new conversation with root message
        store.dispatch(createConversation({
            id: conversationId,
            rootMessage: {
                id: rootMessage.id,
                content: rootMessage.content,
                source: rootMessage.source as 'user' | 'ai' | 'system',
                parentId: null,
                timestamp: rootMessage.timestamp || Date.now(),
                children: []
            },
            selectedMessageId // Include selected message ID in action
        }));

        // Add remaining messages in order, preserving parent relationships
        messages.slice(1).forEach((message: any) => {
            store.dispatch(addMessage({
                conversationId,
                message: {
                    id: message.id,
                    content: message.content,
                    source: message.source as 'user' | 'ai' | 'system',
                    parentId: message.parentId,
                    timestamp: message.timestamp || Date.now(),
                    children: []
                },
                selectedMessageId // Include selected message ID in action
            }));
        });
        
        // Set this as the active conversation and track selected message
        store.dispatch(setActiveConversation({ 
            conversationId,
            selectedMessageId
        }));
    };

    private handleConversationMessage = (content: Message) => {
        

        const state = store.getState();
        const activeConversationId = state.conversations.activeConversationId;
        const selectedMessageId = state.conversations.selectedMessageId;

        if (activeConversationId) {
            // For replies/branches use selectedMessageId as parent if available
            const parentId = content.parentId || selectedMessageId || null;

            store.dispatch(addMessage({
                conversationId: activeConversationId,
                message: {
                    id: content.id,
                    content: content.content,
                    source: content.source,
                    parentId: parentId,
                    timestamp: Date.now(),
                    children: []
                },
                selectedMessageId: selectedMessageId // Preserve selected message context
            }));
        } else {
            const existingConversationId = Object.keys(state.conversations.conversations).find(
                id => state.conversations.conversations[id].messages.some(m => m.id === content.id)
            );
            const conversationId = existingConversationId || `conv_${Date.now()}`;

            // If no active conversation, treat as a new root message.
            store.dispatch(createConversation({
                id: conversationId,
                rootMessage: {
                    id: content.id,
                    content: content.content,
                    source: content.source,
                    parentId: null, // It's a root message
                    timestamp: content.timestamp || Date.now(),
                    children: []
                }
            }));

             //Set this new convo as active
            store.dispatch(setActiveConversation({ conversationId, selectedMessageId: content.id }));
        }
    };



    public connect() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.socket = new WebSocket(`${protocol}//${window.location.host}/ws`);

        this.socket.addEventListener('open', this.handleOpen);
        this.socket.addEventListener('message', this.handleMessage);
        this.socket.addEventListener('error', this.handleError);
        this.socket.addEventListener('close', this.handleClose);
    }

    public send(message: WebSocketMessage) {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify(message));
        }
    }

    private handleOpen = () => {
        console.log('WebSocket Connected');
        this.connected = true;
        eventBus.emit('connectionStatus', { isConnected: true, clientId: this.config.clientId });

        //Move fetch here
        fetch('/api/getAllConversations', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': this.config.clientId || '' //Handle null or undefined
            }
        });
    }

    private handleMessage = (event: MessageEvent) => {
        try {
            
            const message: WebSocketMessage = JSON.parse(event.data);
            
            if (message.messageType === 'clientId') {
                this.config.clientId = message.content;
                
            }
            else if (message.messageType === 'cfrag') {
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
        // Empty handler - will be used by event bus subscribers
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
}

// Create a singleton instance
export const wsManager = new WebSocketManager();