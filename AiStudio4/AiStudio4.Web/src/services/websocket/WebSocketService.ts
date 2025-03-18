import { dispatchWebSocketEvent } from './websocketEvents';
import { prepareAttachmentsForTransmission } from '@/utils/bufferUtils';
export interface WebSocketMessage {
    messageType: string;
    content: any;
    clientId?: string; 
}

export interface WebSocketConnectionStatus {
    isConnected: boolean;
    clientId: string | null;
}

type MessageHandler = (data: any) => void;

export class WebSocketService {
    private socket: WebSocket | null = null;
    private clientId: string = this.generateGuid();
    private connected: boolean = false;
    private reconnectAttempts: number = 0;
    private maxReconnectAttempts: number = 5;
    private reconnectTimeout: ReturnType<typeof setTimeout> | null = null;

    private subscribers: Map<string, Set<MessageHandler>> = new Map();
    private connectionStatusSubscribers: Set<(status: WebSocketConnectionStatus) => void> = new Set();
    private lastMessageTime: number | null = null;
    constructor() {
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
        console.log('WebSocketService initialized with clientId:', this.clientId);
    }

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

        dispatchWebSocketEvent('connection:status', {
            type: 'connecting',
            clientId: this.clientId,
            content: { isConnecting: true, clientId: this.clientId },
        });

        console.log('WebSocket connection attempt initiated');
    }

    public disconnect(): void {
        if (this.reconnectTimeout) {
            clearTimeout(this.reconnectTimeout);
            this.reconnectTimeout = null;
        }

        if (this.socket?.readyState === WebSocket.OPEN) {
            this.socket.close();
        }

        this.connected = false;
        this.reconnectAttempts = 0;
        
        dispatchWebSocketEvent('connection:status', {
            type: 'disconnected',
            clientId: this.clientId,
            content: { isConnected: false, clientId: this.clientId },
        });

        this.notifyConnectionStatusChange();
    }

  public send(message: WebSocketMessage): void {
    if (this.socket?.readyState === WebSocket.OPEN) {
      
      let processedMessage = { ...message };
      
      
      if (message.content && message.content.attachments && Array.isArray(message.content.attachments)) {
        processedMessage.content = { 
          ...message.content,
          attachments: prepareAttachmentsForTransmission(message.content.attachments)
        };
      }
      
      const messageWithClientId = {
        ...processedMessage,
        clientId: this.clientId
      };

      this.socket.send(JSON.stringify(messageWithClientId));

      dispatchWebSocketEvent('message:received', {
        type: 'sent',
        messageType: message.messageType,
        content: message.content,
        timestamp: Date.now(),
      });
    } else {
      console.warn('Cannot send message: WebSocket is not connected');
    }
  }


    public subscribe(messageType: string, handler: MessageHandler): void {
        if (!this.subscribers.has(messageType)) {
            this.subscribers.set(messageType, new Set());
        }
        this.subscribers.get(messageType)?.add(handler);
    }

    public unsubscribe(messageType: string, handler: MessageHandler): void {
        const handlers = this.subscribers.get(messageType);
        if (handlers) {
            handlers.delete(handler);
            if (handlers.size === 0) {
                this.subscribers.delete(messageType);
            }
        }
    }

  public onConnectionStatusChange(handler: (status: WebSocketConnectionStatus) => void): (() => void) {
    this.connectionStatusSubscribers.add(handler);
    
    handler({
      isConnected: this.connected,
      clientId: this.clientId,
    });
    
    
    return () => this.offConnectionStatusChange(handler);
  }

  public offConnectionStatusChange(handler: (status: WebSocketConnectionStatus) => void): void {
    this.connectionStatusSubscribers.delete(handler);
  }

    public isConnected(): boolean {
        return this.connected;
    }

    public getClientId(): string | null {
        return this.clientId;
    }

    public getReconnectAttempts(): number {
        return this.reconnectAttempts;
    }

    private handleOpen = (): void => {
        console.log('WebSocket Connected');
        this.connected = true;
        this.reconnectAttempts = 0;

        
        this.send({
            messageType: 'identify',
            content: { clientId: this.clientId }
        });

        dispatchWebSocketEvent('connection:status', {
            type: 'connected',
            clientId: this.clientId,
            content: { isConnected: true, clientId: this.clientId },
        });

        this.notifyConnectionStatusChange();
    };

    private handleMessage = (event: MessageEvent): void => {
        this.lastMessageTime = Date.now();
        try {
            const message: WebSocketMessage = JSON.parse(event.data);

            dispatchWebSocketEvent('message:received', {
                type: message.messageType,
                content: message.content,
                messageType: message.messageType,
                timestamp: Date.now(),
            });

            if (message.messageType === 'cfrag') {
                dispatchWebSocketEvent('stream:token', {
                    type: 'fragment',
                    content: message.content,
                });
            } else if (message.messageType === 'endstream') {
                dispatchWebSocketEvent('stream:end', {
                    type: 'end',
                });
            } else if (message.messageType === 'conv') {
                dispatchWebSocketEvent('conv:new', {
                    type: 'message',
                    content: message.content,
                });
            } else if (message.messageType === 'loadConv') {
                dispatchWebSocketEvent('conv:load', {
                    type: 'load',
                    content: message.content,
                });
            } else if (message.messageType === 'historicalConvTree') {
                dispatchWebSocketEvent('historical:update', {
                    type: 'tree',
                    content: message.content,
                });
            } else if (message.messageType === 'cancelledRequest') {
                dispatchWebSocketEvent('request:cancelled', {
                    type: 'cancelled',
                    content: message.content,
                });
            }

            this.notifySubscribers(message.messageType, message.content);
        } catch (error) {
            console.error('Error processing message:', error);
        }
    };

    private handleError = (event: Event): void => {
        console.error('WebSocket error:', event);

        dispatchWebSocketEvent('connection:status', {
            type: 'error',
            content: { error: event },
        });
    };

    private handleClose = (): void => {
        console.log('WebSocket disconnected');
        this.socket = null;
        this.connected = false;

        dispatchWebSocketEvent('connection:status', {
            type: 'disconnected',
            clientId: this.clientId,
            content: { isConnected: false, clientId: this.clientId },
        });

        this.notifyConnectionStatusChange();

        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
            console.log(`Attempting to reconnect in ${delay / 1000} seconds (attempt ${this.reconnectAttempts})`);

            this.reconnectTimeout = setTimeout(() => {
                this.connect();
            }, delay);
        }
    };

    private notifySubscribers(messageType: string, data: any): void {
        const handlers = this.subscribers.get(messageType);
        if (handlers) {
            handlers.forEach((handler) => {
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
            clientId: this.clientId,
        };

        this.connectionStatusSubscribers.forEach((handler) => {
            try {
                handler(status);
            } catch (error) {
                console.error('Error in connection status handler:', error);
            }
        });
    }

  public getLastMessageTime(): number | null {
    return this.lastMessageTime;
  }

  private generateGuid(): string {
    
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  
}


export const webSocketService = new WebSocketService();