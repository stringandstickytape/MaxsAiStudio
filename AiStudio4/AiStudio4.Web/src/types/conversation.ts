export interface Message {
    id: string;
    content: string;
    source: 'user' | 'ai' | 'system';
    timestamp: number;
    parentId?: string | null;
    children?: Message[];
}

export interface Conversation {
    id: string;
    messages: Message[];
}

export interface ConversationState {
    conversations: { [conversationId: string]: Conversation };
    activeConversationId: string | null;
    selectedMessageId: string | null; // Track currently selected message in tree view
}