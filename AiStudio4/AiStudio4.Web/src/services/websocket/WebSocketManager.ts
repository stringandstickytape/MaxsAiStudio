import { store } from '@/store/store';
import { addMessage, createConversation, setActiveConversation } from '@/store/conversationSlice';
import { Message } from '@/types/conversation';
import { eventBus } from '@/services/messaging/EventBus';

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
}

class WebSocketManager {
    private socket: WebSocket | null = null;
    private config: ClientConfig = {};
    private connected = false;

    constructor() {
        this.connect = this.connect.bind(this);
        this.handleMessage = this.handleMessage.bind(this);
    }

    private handleEndStream = () => eventBus.emit('endstream', null);

    private handleHistoricalConversationTreeMessage = (content: any) => {
        const { id, content: summary, lastModified } = content;
        const historicalConversation = {
            convGuid: id,
            summary,
            fileName: `conv_${id}.json`,
            lastModified: lastModified || new Date().toISOString(),
            highlightColour: undefined
        };
    }

    private handleLoadConversation = (content: any) => {
        const { conversationId, messages } = content;
        if (!messages?.length) return;

        const urlParams = new URLSearchParams(window.location.search);
        const selectedMessageId = urlParams.get('messageId');

        const rootMessage = messages.find(m => !m.parentId) || messages[0];

        store.dispatch(createConversation({
            id: conversationId,
            rootMessage: this.mapMessage(rootMessage),
            selectedMessageId
        }));

        messages.slice(1).forEach((message: any) => {
            store.dispatch(addMessage({
                conversationId,
                message: this.mapMessage(message),
                selectedMessageId
            }));
        });

        store.dispatch(setActiveConversation({
            conversationId,
            selectedMessageId: selectedMessageId || messages[messages.length - 1].id
        }));
    };

    private mapMessage = (message: any): Message => ({
        id: message.id,
        content: message.content,
        source: message.source,
        parentId: message.parentId || null,
        timestamp: message.timestamp || Date.now(),
        children: []
    });

    private handleConversationMessage = (content: Message) => {
        const state = store.getState();
        const { activeConversationId, selectedMessageId } = state.conversations;

        if (activeConversationId) {
            const conversation = state.conversations.conversations[activeConversationId];

            const parentId = content.parentId ||
                (content.source === 'user' ? selectedMessageId : null) ||
                conversation.messages[conversation.messages.length - 1]?.id ||
                null;

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
                selectedMessageId: content.source === 'ai' ? content.id : undefined
            }));
        } else {
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
                    timestamp: content.timestamp || Date.now(),
                    children: []
                }
            }));

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
        this.connected = true;
        eventBus.emit('connectionStatus', { isConnected: true, clientId: this.config.clientId });
    }

    private handleMessage = (event: MessageEvent) => {
        try {
            const message: WebSocketMessage = JSON.parse(event.data);

            switch (message.messageType) {
                case 'clientId':
                    this.config.clientId = message.content;
                    break;
                case 'cfrag':
                    this.handleNewLiveChatStreamToken(message.content);
                    break;
                case 'conversation':
                    this.handleConversationMessage(message.content);
                    break;
                case 'historicalConversationTree':
                    this.handleHistoricalConversationTreeMessage(message.content);
                    break;
                case 'loadConversation':
                    this.handleLoadConversation(message.content);
                    break;
                case 'endstream':
                    this.handleEndStream();
                    break;
                default:
                    eventBus.emit(message.messageType, message.content);
            }
        } catch (error) {
            console.error('Error processing message:', error);
        }
    }

    private handleError = (event: Event) => {
        console.error('WebSocket error:', event);
    }

    private handleClose = () => {
        this.socket = null;
        this.connected = false;
        eventBus.emit('connectionStatus', { isConnected: false, clientId: null });
    }

    private handleNewLiveChatStreamToken = (token: string) => { };

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

export const wsManager = new WebSocketManager();