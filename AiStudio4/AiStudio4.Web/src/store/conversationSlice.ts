import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Message, ConversationState } from '@/types/conversation';
import { buildDebugTree } from '@/utils/treeUtils';

// Debug helper - exposes conversation state
export const debugConversations = () => {
    const state = (window as any).store.getState().conversations;
    console.group('Conversation State Debug');
    console.log('Active:', state.activeConversationId);
    console.log('All:', state.conversations);
    Object.entries(state.conversations).forEach(([id, conv]: [string, any]) => {
        console.group(`Conversation: ${id}`);
        console.log('Messages:', conv.messages);
        console.log('Count:', conv.messages.length);
        console.log('Tree:', buildDebugTree(conv.messages));
        console.groupEnd();
    });
    console.groupEnd();
    return state;
};

// Export for console access
(window as any).debugConversations = debugConversations;

const initialState: ConversationState = {
    conversations: {},
    activeConversationId: null,
    selectedMessageId: null
};

const conversationSlice = createSlice({
    name: 'conversations',
    initialState,
    reducers: {
        addMessage(state, action: PayloadAction<{ conversationId: string; message: Message; selectedMessageId?: string | null }>) {
            const { conversationId, message, selectedMessageId } = action.payload;
            const conversation = state.conversations[conversationId];

            if (!conversation) {
                console.warn('No conversation:', { conversationId, message });
                return;
            }

            if (!message.parentId && state.selectedMessageId) {
                message.parentId = state.selectedMessageId;
            }

            conversation.messages.push(message);
            if (selectedMessageId !== undefined) state.selectedMessageId = selectedMessageId;
        },
        createConversation(state, action: PayloadAction<{ id: string, rootMessage: Message, selectedMessageId?: string }>) {
            const { id, rootMessage, selectedMessageId } = action.payload;

            state.conversations[id] = {
                id: id,
                messages: [rootMessage]
            };
            state.activeConversationId = id;
            state.selectedMessageId = selectedMessageId || rootMessage.id;
        },
        setActiveConversation(state, action: PayloadAction<{ conversationId: string; selectedMessageId?: string | null }>) {
            const { conversationId, selectedMessageId } = action.payload;
            state.activeConversationId = conversationId;
            if (selectedMessageId !== undefined) {
                state.selectedMessageId = selectedMessageId;
            }
        },
    },
});

export const { addMessage, createConversation, setActiveConversation } = conversationSlice.actions;
export default conversationSlice.reducer;