/ src/stores / useConversationStore.ts
import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Message, Conversation } from '@/types/conversation';
import { buildDebugTree } from '@/utils/treeUtils';
import { MessageGraph } from '@/utils/messageGraph';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

interface ConversationState {
    conversations: Record<string, Conversation>;
    activeConversationId: string | null;
    selectedMessageId: string | null;

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

export const useConversationStore = create<ConversationState>((set, get) => {
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('conversation:new', (detail) => {
            const content = detail.content;
            if (!content) return;

            const store = get();
            const {
                activeConversationId,
                selectedMessageId,
                addMessage,
                createConversation,
                setActiveConversation,
                getConversation
            } = store;

            console.log('Handling conversation message from event:', {
                activeConversationId,
                selectedMessageId,
                messageId: content.id,
                messageSource: content.source,
                parentIdFromContent: content.parentId
            });

            if (activeConversationId) {
                const conversation = getConversation(activeConversationId);

                let parentId = content.parentId;

                if (!parentId && content.source === 'user') {
                    parentId = selectedMessageId;
                }

                if (!parentId && conversation && conversation.messages.length > 0) {
                    const graph = new MessageGraph(conversation.messages);

                    if (content.source === 'ai') {
                        const userMessages = conversation.messages
                            .filter(m => m.source === 'user')
                            .sort((a, b) => b.timestamp - a.timestamp);

                        if (userMessages.length > 0) {
                            parentId = userMessages[0].id;
                        } else {
                            parentId = conversation.messages[conversation.messages.length - 1].id;
                        }
                    }
                }

                console.log('Message parentage determined:', {
                    finalParentId: parentId,
                    messageId: content.id
                });

                addMessage({
                    conversationId: activeConversationId,
                    message: {
                        id: content.id,
                        content: content.content,
                        source: content.source,
                        parentId: parentId,
                        timestamp: Date.now(),
                        tokenUsage: content.tokenUsage || null,
                        costInfo: content.costInfo || null
                    },
                    selectedMessageId: content.source === 'ai' ? content.id : undefined
                });
            } else {
                const conversationId = `conv_${Date.now()}`;

                createConversation({
                    id: conversationId,
                    rootMessage: {
                        id: content.id,
                        content: content.content,
                        source: content.source,
                        parentId: null,
                        timestamp: content.timestamp || Date.now(),
                        tokenUsage: content.tokenUsage || null,
                        costInfo: content.costInfo || null
                    }
                });

                setActiveConversation({
                    conversationId,
                    selectedMessageId: content.id
                });
            }
        });

        listenToWebSocketEvent('conversation:load', (detail) => {
            const content = detail.content;
            if (!content) return;

            const { conversationId, messages } = content;
            const urlParams = new URLSearchParams(window.location.search);
            const selectedMessageId = urlParams.get('messageId');

            console.log('Loading conversation from event:', {
                conversationId,
                messageCount: messages?.length,
                selectedMessageId
            });

            if (!messages || messages.length === 0) return;

            const { createConversation, addMessage, setActiveConversation } = get();

            const graph = new MessageGraph(messages);

            const rootMessages = graph.getRootMessages();
            const rootMessage = rootMessages.length > 0 ? rootMessages[0] : messages[0];

            createConversation({
                id: conversationId,
                rootMessage: {
                    id: rootMessage.id,
                    content: rootMessage.content,
                    source: rootMessage.source as 'user' | 'ai' | 'system',
                    parentId: null,
                    timestamp: rootMessage.timestamp || Date.now(),
                    tokenUsage: rootMessage.tokenUsage || null,
                    costInfo: rootMessage.costInfo || null
                }
            });

            const nonRootMessages = messages.filter(msg =>
                msg.id !== rootMessage.id &&
                (msg.parentId || graph.getMessagePath(msg.id).length > 1)
            );

            nonRootMessages
                .sort((a, b) => a.timestamp - b.timestamp)
                .forEach((message) => {
                    addMessage({
                        conversationId,
                        message: {
                            id: message.id,
                            content: message.content,
                            source: message.source as 'user' | 'ai' | 'system',
                            parentId: message.parentId,
                            timestamp: message.timestamp || Date.now(),
                            tokenUsage: message.tokenUsage || null,
                            costInfo: message.costInfo || null
                        }
                    });
                });

            setActiveConversation({
                conversationId,
                selectedMessageId: selectedMessageId || messages[messages.length - 1].id
            });
        });
    }

    return {
        conversations: {},
        activeConversationId: null,
        selectedMessageId: null,

        createConversation: ({ id = `conv_${uuidv4()}`, rootMessage, selectedMessageId }) => set((state) => {
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

        addMessage: ({ conversationId, message, selectedMessageId }) => set((state) => {
            const conversation = state.conversations[conversationId];
            if (!conversation) {
                console.warn('No conversation found with ID:', conversationId);
                return state;
            }

            let updatedMessage = { ...message };
            if (!updatedMessage.parentId && state.selectedMessageId) {
                updatedMessage.parentId = state.selectedMessageId;
            }

            const updatedMessages = [...conversation.messages, updatedMessage];

            return {
                conversations: {
                    ...state.conversations,
                    [conversationId]: {
                        ...conversation,
                        messages: updatedMessages
                    }
                },
                selectedMessageId: selectedMessageId !== undefined ? selectedMessageId : state.selectedMessageId
            };
        }),

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

        getConversation: (conversationId) => {
            return get().conversations[conversationId];
        },

        getActiveConversation: () => {
            const { activeConversationId, conversations } = get();
            return activeConversationId ? conversations[activeConversationId] : undefined;
        },

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

        deleteMessage: ({ conversationId, messageId }) => set((state) => {
            const conversation = state.conversations[conversationId];
            if (!conversation) return state;

            const updatedMessages = conversation.messages.filter(msg => msg.id !== messageId);

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

        clearConversation: (conversationId) => set((state) => {
            const conversation = state.conversations[conversationId];
            if (!conversation) return state;

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

        deleteConversation: (conversationId) => set((state) => {
            const { [conversationId]: _, ...remainingConversations } = state.conversations;

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
    };
});

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

(window as any).debugConversations = debugConversations;