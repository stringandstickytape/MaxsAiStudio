import { store } from '@/store/store';
import { addMessage } from '@/store/conversationSlice';
import { wsManager } from './websocket/WebSocketManager';

export class ChatService {
    static async sendMessage(message: string, selectedModel: string) {
        const clientId = wsManager.getClientId();
        const state = store.getState();
        const activeConversationId = state.conversations.activeConversationId;

        if (!activeConversationId || !clientId || selectedModel === 'Select Model') {
            throw new Error('Missing required chat parameters');
        }

        const conversation = state.conversations.conversations[activeConversationId];
        if (!conversation) {
            throw new Error('Could not find active conversation');
        }

        const newMessageId = `msg_${Date.now()}`;
        // Use selectedMessageId as parent if available, otherwise use last message
        // Always use the last message as parent for new messages
        const parentMessageId = conversation.messages[conversation.messages.length - 1]?.id || null;

        // Dispatch user message to store
        store.dispatch(addMessage({
            conversationId: activeConversationId,
            message: {
                id: newMessageId,
                content: message,
                source: 'user',
                timestamp: Date.now(),
                parentId: parentMessageId
            }
        }));

        const response = await fetch('/api/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify({
                clientId,
                message,
                conversationId: activeConversationId,
                newMessageId,
                parentMessageId,
                model: selectedModel
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    }

    static async fetchModels() {
        const response = await fetch("/api/getConfig", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
        });
        
        const data = await response.json();
        if (!data.success || !Array.isArray(data.models)) {
            throw new Error('Failed to fetch models');
        }

        return data.models;
    }
}