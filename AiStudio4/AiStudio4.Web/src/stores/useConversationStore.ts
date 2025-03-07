// src/stores/useConversationStore.ts
import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Message, Conversation } from '@/types/conversation';
import { buildDebugTree } from '@/utils/treeUtils';

interface ConversationState {
  // State
  conversations: Record<string, Conversation>;
  activeConversationId: string | null;
  selectedMessageId: string | null;
  
  // Actions
  createConversation: (params: {
    id?: string;
    rootMessage: Message;
    selectedMessageId?: string;
  }) => void;
  
  addMessage: (params: {
    conversationId: string;
    message: Message;
    selectedMessageId?: string | null;
  }) => void;
  
  setActiveConversation: (params: {
    conversationId: string;
    selectedMessageId?: string | null;
  }) => void;
  
  getConversation: (conversationId: string) => Conversation | undefined;
  getActiveConversation: () => Conversation | undefined;
  
  updateMessage: (params: {
    conversationId: string;
    messageId: string;
    content: string;
  }) => void;
  
  deleteMessage: (params: {
    conversationId: string;
    messageId: string;
  }) => void;
  
  clearConversation: (conversationId: string) => void;
  
  deleteConversation: (conversationId: string) => void;
}

export const useConversationStore = create<ConversationState>((set, get) => ({
  // Initial state
  conversations: {},
  activeConversationId: null,
  selectedMessageId: null,
  
  // Create a new conversation
  createConversation: ({ id = `conv_${uuidv4()}`, rootMessage, selectedMessageId }) => set((state) => {
    // Create the new conversation
    const newConversation: Conversation = {
      id,
      messages: [rootMessage]
    };
    
    return {
      conversations: {
        ...state.conversations,
        [id]: newConversation
      },
      activeConversationId: id,
      selectedMessageId: selectedMessageId || rootMessage.id
    };
  }),
  
  // Add a message to a conversation
  addMessage: ({ conversationId, message, selectedMessageId }) => set((state) => {
    const conversation = state.conversations[conversationId];
    if (!conversation) {
      console.warn('No conversation found with ID:', conversationId);
      return state;
    }
    
    // If no parent specified but we have a selected message, use that as parent
    let updatedMessage = { ...message };
    if (!updatedMessage.parentId && state.selectedMessageId) {
      updatedMessage.parentId = state.selectedMessageId;
    }
    
    // Create a new array with the message added
    const updatedMessages = [...conversation.messages, updatedMessage];
    
    return {
      conversations: {
        ...state.conversations,
        [conversationId]: {
          ...conversation,
          messages: updatedMessages
        }
      },
      // Only update selected message ID if it was provided
      selectedMessageId: selectedMessageId !== undefined ? selectedMessageId : state.selectedMessageId
    };
  }),
  
  // Set the active conversation
  setActiveConversation: ({ conversationId, selectedMessageId }) => set((state) => {
    if (!state.conversations[conversationId]) {
      console.warn('Trying to set active conversation that does not exist:', conversationId);
      return state;
    }
    
    return {
      activeConversationId: conversationId,
      selectedMessageId: selectedMessageId !== undefined 
        ? selectedMessageId 
        : state.selectedMessageId
    };
  }),
  
  // Get a specific conversation
  getConversation: (conversationId) => {
    return get().conversations[conversationId];
  },
  
  // Get the active conversation
  getActiveConversation: () => {
    const { activeConversationId, conversations } = get();
    return activeConversationId ? conversations[activeConversationId] : undefined;
  },
  
  // Update a message's content
  updateMessage: ({ conversationId, messageId, content }) => set((state) => {
    const conversation = state.conversations[conversationId];
    if (!conversation) return state;
    
    const messageIndex = conversation.messages.findIndex(msg => msg.id === messageId);
    if (messageIndex === -1) return state;
    
    const updatedMessages = [...conversation.messages];
    updatedMessages[messageIndex] = {
      ...updatedMessages[messageIndex],
      content
    };
    
    return {
      conversations: {
        ...state.conversations,
        [conversationId]: {
          ...conversation,
          messages: updatedMessages
        }
      }
    };
  }),
  
  // Delete a message
  deleteMessage: ({ conversationId, messageId }) => set((state) => {
    const conversation = state.conversations[conversationId];
    if (!conversation) return state;
    
    // Filter out the message to delete
    const updatedMessages = conversation.messages.filter(msg => msg.id !== messageId);
    
    // If we're deleting the selected message, reset the selected message
    const updatedSelectedMessageId = 
      state.selectedMessageId === messageId 
        ? (updatedMessages.length > 0 ? updatedMessages[updatedMessages.length - 1].id : null)
        : state.selectedMessageId;
    
    return {
      conversations: {
        ...state.conversations,
        [conversationId]: {
          ...conversation,
          messages: updatedMessages
        }
      },
      selectedMessageId: updatedSelectedMessageId
    };
  }),
  
  // Clear all messages in a conversation (except the root)
  clearConversation: (conversationId) => set((state) => {
    const conversation = state.conversations[conversationId];
    if (!conversation) return state;
    
    // Keep only the root message
    const rootMessage = conversation.messages.find(msg => !msg.parentId);
    if (!rootMessage) return state;
    
    return {
      conversations: {
        ...state.conversations,
        [conversationId]: {
          ...conversation,
          messages: [rootMessage]
        }
      },
      selectedMessageId: rootMessage.id
    };
  }),
  
  // Delete a conversation
  deleteConversation: (conversationId) => set((state) => {
    const { [conversationId]: _, ...remainingConversations } = state.conversations;
    
    // If we're deleting the active conversation, set a new active conversation
    let newActiveId = state.activeConversationId;
    let newSelectedMessageId = state.selectedMessageId;
    
    if (state.activeConversationId === conversationId) {
      const conversationIds = Object.keys(remainingConversations);
      newActiveId = conversationIds.length > 0 ? conversationIds[0] : null;
      
      if (newActiveId) {
        const newActiveConversation = remainingConversations[newActiveId];
        newSelectedMessageId = newActiveConversation.messages.length > 0 
          ? newActiveConversation.messages[0].id 
          : null;
      } else {
        newSelectedMessageId = null;
      }
    }
    
    return {
      conversations: remainingConversations,
      activeConversationId: newActiveId,
      selectedMessageId: newSelectedMessageId
    };
  })
}));

// Debug helper for console
export const debugConversations = () => {
  const state = useConversationStore.getState();
  console.group('Conversation State Debug');
  console.log('Active:', state.activeConversationId);
  console.log('Selected Message:', state.selectedMessageId);
  console.log('All:', state.conversations);
  
  Object.entries(state.conversations).forEach(([id, conv]) => {
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