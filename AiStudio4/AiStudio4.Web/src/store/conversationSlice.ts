import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { Message, Conversation, ConversationState } from '@/types/conversation';

// Debug helper to expose conversation state
export const debugConversations = () => {
    const state = (window as any).store.getState().conversations;
    console.group('Conversation State Debug');
    console.log('Active Conversation:', state.activeConversationId);
    console.log('All Conversations:', state.conversations);
    Object.entries(state.conversations).forEach(([id, conv]: [string, any]) => {
        console.group(`Conversation: ${id}`);
        console.log('Messages:', conv.messages);
        console.log('Message Count:', conv.messages.length);
        console.log('Message Tree Structure:', buildDebugTree(conv.messages));
        console.groupEnd();
    });
    console.groupEnd();
    return state;
};

// Export to window for console access
(window as any).debugConversations = debugConversations;

import { buildDebugTree } from '@/utils/treeUtils';

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
                console.warn('Attempted to add message to non-existent conversation:', { conversationId, message });
                return;
            }

            // Set parent ID based on selected message if no parent specified
            if (!message.parentId && state.selectedMessageId) {
                message.parentId = state.selectedMessageId;
            }

            console.log('Adding message to conversation:', {
                conversationId,
                messageId: message.id,
                parentId: message.parentId,
                selectedMessageId: state.selectedMessageId,
                messageContent: message.content,
                currentMessages: conversation.messages
            });

            conversation.messages.push(message);
            
            // Update selected message ID if provided
            if (selectedMessageId !== undefined) {
                state.selectedMessageId = selectedMessageId;
            }
        },
        createConversation(state, action: PayloadAction<{ id: string, rootMessage: Message, selectedMessageId?: string }>) {
            const { id, rootMessage, selectedMessageId } = action.payload;
            
            console.log('Creating new conversation:', {
                conversationId: id,
                rootMessageId: rootMessage.id,
                rootMessageContent: rootMessage.content,
                existingConversations: Object.keys(state.conversations),
                selectedMessageId
            });

            state.conversations[id] = {
                id: id,
                messages: [rootMessage]
            };
            state.activeConversationId = id;
            
            // Set selected message ID if provided, otherwise use root message ID
            state.selectedMessageId = selectedMessageId || rootMessage.id;
        },
        setActiveConversation(state, action: PayloadAction<{ conversationId: string; selectedMessageId?: string | null }>) {
            const { conversationId, selectedMessageId } = action.payload;
            console.log('Setting active conversation:', {
                newActiveId: conversationId,
                selectedMessageId,
                previousActiveId: state.activeConversationId,
                conversationExists: !!state.conversations[conversationId]
            });
            state.activeConversationId = conversationId;
            if (selectedMessageId !== undefined) {
                state.selectedMessageId = selectedMessageId;
            }
        },
    },
});

export const { addMessage, createConversation, setActiveConversation } = conversationSlice.actions;
export default conversationSlice.reducer;