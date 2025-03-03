// src/services/ChatService.ts
import { v4 as uuidv4 } from 'uuid';
import { store } from '@/store/store';
import { addMessage } from '@/store/conversationSlice';

export type ModelType = 'primary' | 'secondary';

export class ChatService {
    static async sendMessage(
        message: string, 
        modelName: string, 
        activeTools: string[] = [], 
        systemPromptId?: string | null,
        systemPromptContent?: string | null
    ) {
        try {
            const clientId = localStorage.getItem('clientId') || '';
            const state = store.getState();
            const conversationId = state.conversations.activeConversationId;
            const parentMessageId = state.conversations.selectedMessageId;

            if (!conversationId || !parentMessageId) {
                console.error('Missing conversation or parent message ID');
                return;
            }

            const messageId = uuidv4();

            store.dispatch(addMessage({
                conversationId: conversationId,
                message: {
                    id: messageId,
                    content: message,
                    source: 'user',
                    timestamp: Date.now(),
                    parentId: parentMessageId
                }
            }));

            const userMessage = {
                conversationId: conversationId,
                newMessageId: messageId,
                parentMessageId: parentMessageId,
                message: message,
                model: modelName,
                toolIds: activeTools,
                systemPromptId: systemPromptId || undefined,
                systemPromptContent: systemPromptContent || undefined
            };

            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': clientId,
                },
                body: JSON.stringify(userMessage)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('Failed to send message:', error);
            throw error;
        }
    }

    static async fetchModels() {
        try {
            const clientId = localStorage.getItem('clientId') || 'no-client-id';
            const response = await fetch("/api/getConfig", {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': clientId,
                },
                body: JSON.stringify({})
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();

            if (!data.success || !Array.isArray(data.models)) {
                throw new Error('Failed to fetch models');
            }

            return {
                models: data.models,
                defaultModel: data.defaultModel || "",
                secondaryModel: data.secondaryModel || ""
            };
        } catch (error) {
            console.error('Failed to fetch models:', error);
            throw error;
        }
    }

    static async saveModel(modelType: ModelType, modelName: string) {
        const clientId = localStorage.getItem('clientId');
        if (!clientId) {
            throw new Error('Client ID not found');
        }

        const endpoint = modelType === 'primary' ? '/api/setDefaultModel' : '/api/setSecondaryModel';
        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': clientId,
                },
                body: JSON.stringify({ clientId, modelName })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error(`Failed to save ${modelType} model:`, error);
            throw error;
        }
    }

    static saveDefaultModel(modelName: string) {
        return this.saveModel('primary', modelName);
    }

    static saveSecondaryModel(modelName: string) {
        return this.saveModel('secondary', modelName);
    }
}