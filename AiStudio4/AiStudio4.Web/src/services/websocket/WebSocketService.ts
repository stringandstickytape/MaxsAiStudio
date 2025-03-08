// src/services/websocket/WebSocketService.ts
import { dispatchWebSocketEvent } from './websocketEvents';
import { Message } from '@/types/conversation';
import { MessageGraph } from '@/utils/messageGraph';

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
            
            // Dispatch an event when a message is sent
            dispatchWebSocketEvent('message:received', {
                type: 'sent',
                messageType: message.messageType,
                content: message.content
            });
        } else {
            console.warn('Cannot send message: WebSocket is not connected');
        }
    }

    private dispatchCustomWebSocketEvent(eventName: string): void {
        const event = new CustomEvent(eventName, {
            detail: {
                clientId: this.clientId,
                timestamp: Date.now()
            },
            bubbles: true,
            cancelable: true
        });

        window.dispatchEvent(event);
        console.log(`Dispatched custom event: ${eventName}`);
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
        
        // Dispatch an event for the connection status change
        dispatchWebSocketEvent('connection:status', {
            type: 'connected',
            clientId: this.clientId,
            content: { isConnected: true, clientId: this.clientId }
        });
        
        this.notifyConnectionStatusChange();
        this.dispatchCustomWebSocketEvent('ws-connected');
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
                
                // Dispatch an event for the client ID update
                dispatchWebSocketEvent('connection:status', {
                    type: 'clientId',
                    clientId: message.content,
                    content: { isConnected: this.connected, clientId: message.content }
                });
                
                this.notifyConnectionStatusChange();
            }
            
            // Dispatch a general message event for all messages
            dispatchWebSocketEvent('message:received', {
                type: message.messageType,
                content: message.content,
                messageType: message.messageType
            });
            
            // Handle specific message types with their own events
            if (message.messageType === 'cfrag') {
                dispatchWebSocketEvent('stream:token', {
                    type: 'fragment',
                    content: message.content
                });
            } else if (message.messageType === 'endstream') {
                dispatchWebSocketEvent('stream:end', {
                    type: 'end'
                });
            } else if (message.messageType === 'conversation') {
                dispatchWebSocketEvent('conversation:new', {
                    type: 'message',
                    content: message.content
                });
            } else if (message.messageType === 'loadConversation') {
                dispatchWebSocketEvent('conversation:load', {
                    type: 'load',
                    content: message.content
                });
            } else if (message.messageType === 'historicalConversationTree') {
                dispatchWebSocketEvent('historical:update', {
                    type: 'tree',
                    content: message.content
                });
            }
            
            // Still notify direct subscribers for backward compatibility
            this.notifySubscribers(message.messageType, message.content);
        } catch (error) {
            console.error('Error processing message:', error);
        }
    }

    private handleError = (event: Event): void => {
        console.error('WebSocket error:', event);
        
        // Dispatch an event for the connection error
        dispatchWebSocketEvent('connection:status', {
            type: 'error',
            content: { error: event }
        });
    }

    private handleClose = (): void => {
        console.log('WebSocket disconnected');
        this.socket = null;
        this.connected = false;
        
        // Dispatch an event for the connection close
        dispatchWebSocketEvent('connection:status', {
            type: 'disconnected',
            clientId: this.clientId,
            content: { isConnected: false, clientId: this.clientId }
        });
        
        this.notifyConnectionStatusChange();
        this.dispatchCustomWebSocketEvent('ws-disconnected');

        // Attempt to reconnect if not deliberately disconnected
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
            console.log(`Attempting to reconnect in ${delay / 1000} seconds (attempt ${this.reconnectAttempts})`);

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
}

// Create a singleton instance
export const webSocketService = new WebSocketService();