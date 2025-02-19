export interface Message {
    id: string;
    content: string;
    source: 'user' | 'ai';
    parentId: string | null;
    timestamp: number;
    children: string[];
}

export interface Conversation {
    id: string;
    messages: { [messageId: string]: Message };
    rootMessageId: string;
}

export interface ConversationState {
    conversations: { [conversationId: string]: Conversation };
    activeConversationId: string | null;
}