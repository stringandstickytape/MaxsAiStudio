// C:\Users\maxhe\source\repos\CloneTest\MaxsAiTool\AiStudio4\AiStudio4.Web\src\services\ChatService.ts
import { store } from '@/store/store';
import { addMessage } from '@/store/conversationSlice';
import { wsManager } from './websocket/WebSocketManager';

export type ModelType = 'primary' | 'secondary';

export class ChatService {
    private static async apiRequest(endpoint: string, clientId: string, data: any) {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Client-Id': clientId,
            },
            body: JSON.stringify(data)
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return response.json();
    }

    static async sendMessage(message: string, selectedModel: string, toolIds: string[] = []) {
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
        const selectedMessageId = state.conversations.selectedMessageId;
        const parentMessageId = selectedMessageId || conversation.messages[conversation.messages.length - 1]?.id || null;

        console.log('ChatService: Sending message with parent:', {
            selectedMessageId,
            parentMessageId,
            messageCount: conversation.messages.length,
            lastMessageId: conversation.messages[conversation.messages.length - 1]?.id
        });

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

        return await ChatService.apiRequest('/api/chat', clientId, {
            clientId,
            message,
            conversationId: activeConversationId,
            newMessageId,
            parentMessageId,
            model: selectedModel,
            toolIds: toolIds
        });
    }

    static async fetchModels() {
        const data = await ChatService.apiRequest("/api/getConfig", wsManager.getClientId() || 'no-client-id', {});
        if (!data.success || !Array.isArray(data.models)) {
            throw new Error('Failed to fetch models');
        }

        return {
            models: data.models,
            defaultModel: data.defaultModel || "",
            secondaryModel: data.secondaryModel || ""
        };
    }

    static async saveModel(modelType: ModelType, modelName: string) {
        const clientId = wsManager.getClientId();
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const endpoint = modelType === 'primary' ? '/api/setDefaultModel' : '/api/setSecondaryModel';
        return await ChatService.apiRequest(endpoint, clientId, { clientId, modelName });
    }

    static saveDefaultModel(modelName: string) {
        return this.saveModel('primary', modelName);
    }

    static saveSecondaryModel(modelName: string) {
        return this.saveModel('secondary', modelName);
    }
}