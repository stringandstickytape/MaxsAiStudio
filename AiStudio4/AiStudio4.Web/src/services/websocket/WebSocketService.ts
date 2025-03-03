import { store } from '@/store/store';
import { addMessage, createConversation, setActiveConversation } from '@/store/conversationSlice';
import { Message } from '@/types/conversation';

export interface WebSocketMessage {
    messageType: string;
    content: any;
}

export interface WebSocketConnectionStatus {
    isConnected: boolean;
    clientId: string | null;
}

type MessageHandler = (data: any) => void;

export class WebSocketService {
    private socket: WebSocket | null = null;
    private clientId: string | null = null;
    private connected: boolean = false;
    private reconnectAttempts: number = 0;
    private maxReconnectAttempts: number = 5;
    private reconnectTimeout: ReturnType<typeof setTimeout> | null = null;
    
    // Subscription management
    private subscribers: Map<string, Set<MessageHandler>> = new Map();
    private connectionStatusSubscribers: Set<(status: WebSocketConnectionStatus) => void> = new Set();
    
    constructor() {
        // Bind methods to ensure 'this' context is preserved
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
    }

    /**
     * Connect to the WebSocket server
     */
    public connect(): void {
        if (this.socket?.readyState === WebSocket.OPEN) {
            console.log('WebSocket already connected');
            return;
        }

        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.socket = new WebSocket(`${protocol}//${window.location.host}/ws`);

        this.socket.addEventListener('open', this.handleOpen);
        this.socket.addEventListener('message', this.handleMessage);
        this.socket.addEventListener('error', this.handleError);
        this.socket.addEventListener('close', this.handleClose);
        
        console.log('WebSocket connection attempt initiated');
    }

    /**
     * Disconnect from the WebSocket server
     */
    public disconnect(): void {
        if (this.reconnectTimeout) {
            clearTimeout(this.reconnectTimeout);
            this.reconnectTimeout = null;
        }
        
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.close();
        }
        
        this.connected = false;
        this.notifyConnectionStatusChange();
    }

    /**
     * Send a message through the WebSocket
     */
    public send(message: WebSocketMessage): void {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.send(JSON.stringify(message));
        } else {
            console.warn('Cannot send message: WebSocket is not connected');
        }
    }

    /**
     * Subscribe to a specific message type
     */
    public subscribe(messageType: string, handler: MessageHandler): void {
        if (!this.subscribers.has(messageType)) {
            this.subscribers.set(messageType, new Set());
        }
        this.subscribers.get(messageType)?.add(handler);
    }

    /**
     * Unsubscribe from a specific message type
     */
    public unsubscribe(messageType: string, handler: MessageHandler): void {
        const handlers = this.subscribers.get(messageType);
        if (handlers) {
            handlers.delete(handler);
            if (handlers.size === 0) {
                this.subscribers.delete(messageType);
            }
        }
    }

    /**
     * Subscribe to connection status changes
     */
    public onConnectionStatusChange(handler: (status: WebSocketConnectionStatus) => void): void {
        this.connectionStatusSubscribers.add(handler);
        // Immediately notify with current status
        handler({
            isConnected: this.connected,
            clientId: this.clientId
        });
    }

    /**
     * Unsubscribe from connection status changes
     */
    public offConnectionStatusChange(handler: (status: WebSocketConnectionStatus) => void): void {
        this.connectionStatusSubscribers.delete(handler);
    }

    /**
     * Check if WebSocket is connected
     */
    public isConnected(): boolean {
        return this.connected;
    }

    /**
     * Get the client ID
     */
    public getClientId(): string | null {
        return this.clientId;
    }

    // Private methods for handling WebSocket events
    private handleOpen = (): void => {
        console.log('WebSocket Connected');
        this.connected = true;
        this.reconnectAttempts = 0;
        this.notifyConnectionStatusChange();
    }

    private handleMessage = (event: MessageEvent): void => {
        try {
            const message: WebSocketMessage = JSON.parse(event.data);

            // Handle special message types
            if (message.messageType === 'clientId') {
                this.clientId = message.content;
                // Save clientId to localStorage when received
                localStorage.setItem('clientId', message.content);
                console.log('Client ID received and saved to localStorage:', message.content);
                this.notifyConnectionStatusChange();
            } else if (message.messageType === 'conversation') {
                this.handleConversationMessage(message.content);
            } else if (message.messageType === 'loadConversation') {
                this.handleLoadConversation(message.content);
            } else if (message.messageType === 'historicalConversationTree') {
                this.handleHistoricalConversationTreeMessage(message.content);
            }

            // Notify all subscribers for this message type
            this.notifySubscribers(message.messageType, message.content);
        } catch (error) {
            console.error('Error processing message:', error);
        }
    }

    private handleError = (event: Event): void => {
        console.error('WebSocket error:', event);
    }

    private handleClose = (): void => {
        console.log('WebSocket disconnected');
        this.socket = null;
        this.connected = false;
        this.notifyConnectionStatusChange();
        
        // Attempt to reconnect if not deliberately disconnected
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
            console.log(`Attempting to reconnect in ${delay/1000} seconds (attempt ${this.reconnectAttempts})`);
            
            this.reconnectTimeout = setTimeout(() => {
                this.connect();
            }, delay);
        }
    }

    // Notification methods
    private notifySubscribers(messageType: string, data: any): void {
        const handlers = this.subscribers.get(messageType);
        if (handlers) {
            handlers.forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in message handler for ${messageType}:`, error);
                }
            });
        }
    }

    private notifyConnectionStatusChange(): void {
        const status: WebSocketConnectionStatus = {
            isConnected: this.connected,
            clientId: this.clientId
        };
        
        this.connectionStatusSubscribers.forEach(handler => {
            try {
                handler(status);
            } catch (error) {
                console.error('Error in connection status handler:', error);
            }
        });
    }

    // Message type handlers
    private handleConversationMessage(content: Message): void {
        const state = store.getState();
        const activeConversationId = state.conversations.activeConversationId;
        const selectedMessageId = state.conversations.selectedMessageId;
        
        console.log('WebSocketService: Handling conversation message:', {
            activeConversationId,
            selectedMessageId,
            messageId: content.id,
            messageSource: content.source,
            parentIdFromContent: content.parentId
        });

        if (activeConversationId) {
            // Get the conversation to find the last message as parent
            const conversation = state.conversations.conversations[activeConversationId];
            
            // Use the explicitly specified parentId first, then the selectedMessageId from state,
            // and finally fall back to the last message in the conversation
            const parentId = content.parentId || 
                             (content.source === 'user' ? selectedMessageId : null) || 
                             conversation.messages[conversation.messages.length - 1]?.id || 
                             null;

            console.log('WebSocketService: Message parentage determined:', {
                finalParentId: parentId,
                messageId: content.id
            });

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
                // For AI responses, set the selectedMessageId to continue the same branch
                // Only update the selectedMessageId if this is an AI response to ensure branch continuity
                selectedMessageId: content.source === 'ai' ? content.id : undefined
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
    }

    private handleLoadConversation(content: any): void {
        const { conversationId, messages } = content;
        const urlParams = new URLSearchParams(window.location.search);
        const selectedMessageId = urlParams.get('messageId');
        
        console.log('Loading conversation:', { conversationId, messageCount: messages?.length, selectedMessageId });
        if (!messages || messages.length === 0) return;

        // Find root message (message with no parent)
        const rootMessage = messages.find((m: any) => !m.parentId) || messages[0];

        // Clear any existing conversations by resetting the state
        // This ensures we start fresh when loading a historical conversation
        const existingState = store.getState();
        
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
        // When loading a historical conversation, always set the selectedMessageId
        // This ensures we only see the relevant branch
        store.dispatch(setActiveConversation({ 
            conversationId,
            selectedMessageId: selectedMessageId || messages[messages.length - 1].id
        }));
    }

    private handleHistoricalConversationTreeMessage(content: any): void {
        const historicalConversation = {
            convGuid: content.id,
            summary: content.content,
            fileName: `conv_${content.id}.json`,
            lastModified: content.lastModified || new Date().toISOString(),
            highlightColour: undefined
        };
        // Notify subscribers will handle this since we're just passing through the data
    }
}

// Create a singleton instance
export const webSocketService = new WebSocketService();