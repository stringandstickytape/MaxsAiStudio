// src/services/websocket/types.ts
export interface WebSocketMessage {
    messageType: string;
    content: any;
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

    constructor() {
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
    }

    public connect() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.socket = new WebSocket(`${protocol}//${window.location.host}/ws`);

        this.socket.addEventListener('open', this.handleOpen);
        this.socket.addEventListener('message', this.handleMessage);
        this.socket.addEventListener('error', this.handleError);
        this.socket.addEventListener('close', this.handleClose);
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
            console.log('Raw message received:', event.data); // Add this
            const message: WebSocketMessage = JSON.parse(event.data);
            console.log('Parsed message:', message); // Add this

            // Handle client ID message specially
            if (message.messageType === 'clientId') {
                this.config.clientId = message.content;
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

    public disconnect() {
        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.close();
        }
    }
}

// Create a singleton instance
export const wsManager = new WebSocketManager();