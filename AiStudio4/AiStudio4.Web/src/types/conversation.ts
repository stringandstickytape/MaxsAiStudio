export interface Message {
    id: string;
    content: string;
    source: 'user' | 'ai' | 'system';
    timestamp: number;
}

export interface Conversation {
    id: string;
    messages: Message[];
}

export interface ConversationState {
    conversations: { [conversationId: string]: Conversation };
    activeConversationId: string | null;
}