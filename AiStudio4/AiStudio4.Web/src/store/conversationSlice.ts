import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Message, Conversation, ConversationState } from '../types/conversation';

const initialState: ConversationState = {
    conversations: {},
    activeConversationId: null,
};

const conversationSlice = createSlice({
    name: 'conversations',
    initialState,
    reducers: {
        addMessage(state, action: PayloadAction<{ conversationId: string; message: Message }>) {
            const { conversationId, message } = action.payload;
            const conversation = state.conversations[conversationId];
            
            if (!conversation) return;

            conversation.messages.push(message);
        },
        createConversation(state, action: PayloadAction<{ id: string, rootMessage: Message }>) {
            const { id, rootMessage } = action.payload;
            console.log("New conversation: " + id);
            
            state.conversations[id] = {
                id: id,
                messages: [rootMessage]
            };
            state.activeConversationId = id;
        },
        setActiveConversation(state, action: PayloadAction<string>) {
            state.activeConversationId = action.payload;
        },
    },
});

export const { addMessage, createConversation, setActiveConversation } = conversationSlice.actions;
export default conversationSlice.reducer;