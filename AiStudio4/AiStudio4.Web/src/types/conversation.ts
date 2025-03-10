// src/types/conversation.ts
export interface Message {
    id: string;
    content: string;
    source: 'user' | 'ai' | 'system';
    timestamp: number;
    parentId?: string | null;
    tokenUsage?: {
        inputTokens: number;
        outputTokens: number;
        cacheCreationInputTokens: number;
        cacheReadInputTokens: number;
    } | null;
}

export interface Conversation {
    id: string;
    messages: Message[];
}

export interface ConversationState {
    conversations: { [conversationId: string]: Conversation };
    activeConversationId: string | null;
    selectedMessageId: string | null;
}