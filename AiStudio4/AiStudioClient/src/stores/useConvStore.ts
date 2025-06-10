import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Message, Conv } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { processAttachments } from '@/utils/attachmentUtils';
import { useAttachmentStore } from '@/stores/useAttachmentStore';

interface ConvState {
    convs: Record<string, Conv>;
    activeConvId: string | null;
    selectedMessageId: string | null;
    editingMessageId: string | null;
    createConv: (p: { id?: string; rootMessage: Message }) => void;
    addMessage: (p: { convId: string; message: Message }) => void;
    setActiveConv: (p: { convId: string }) => void;
    setSelectedMessage: (p: { convId: string; messageId: string | null }) => void;
    getConv: (convId: string) => Conv | undefined;
    getActiveConv: () => Conv | undefined;
    updateMessage: (p: { convId: string; messageId: string; content: string }) => void;
    deleteMessage: (p: { convId: string; messageId: string }) => void;
    clearConv: (convId: string) => void;
    deleteConv: (convId: string) => void;
    editMessage: (messageId: string | null) => void;
    cancelEditMessage: () => void;
}



export const useConvStore = create<ConvState>((set, get) => {
    
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('conv:upd', ({ content }) => {
            if (!content) return;
            const { activeConvId, selectedMessageId, addMessage, createConv, setActiveConv, getConv } = get();
            
            // Get the convId from the message if available, otherwise use activeConvId
            const targetConvId = content.convId || activeConvId;
            
            if (targetConvId) {
                
                const conv = getConv(targetConvId);
                let parentId = content.parentId;

                if (!parentId && content.source === 'user') parentId = selectedMessageId;

                // Check for both 'ai' and 'assistant' sources
                const isAiMessage = content.source === 'ai' || content.source === 'assistant';
                
                if (!parentId && conv?.messages.length > 0 && isAiMessage) {
                    const userMsgs = conv.messages
                        .filter(m => m.source === 'user')
                        .sort((a, b) => b.timestamp - a.timestamp);

                    parentId = userMsgs.length ? userMsgs[0].id : conv.messages[conv.messages.length - 1].id;
                }

                
                const attachments = content.attachments && Array.isArray(content.attachments) 
                    ? processAttachments(content.attachments)
                    : undefined;

                // Store processed attachments in the attachment store if they exist
                if (attachments) {
                    useAttachmentStore.getState().addAttachmentsForId(content.id, attachments);
                }
                console.log('temp = ', content.temperature);
                
                // Check if message already exists (for updates)
                const existingMessageIndex = conv?.messages.findIndex(m => m.id === content.id) ?? -1;
                
                if (existingMessageIndex !== -1) {
                    // --- UPDATE EXISTING MESSAGE ---
                    set(state => {
                        const updatedMessages = [...conv.messages];
                        const existingMessage = updatedMessages[existingMessageIndex];
                        updatedMessages[existingMessageIndex] = { 
                            ...existingMessage, 
                            content: content.content,
                            durationMs: content.durationMs,
                            costInfo: content.costInfo || existingMessage.costInfo,
                            cumulativeCost: content.cumulativeCost || existingMessage.cumulativeCost,
                            temperature: content.temperature || existingMessage.temperature
                        };
                        
                        return {
                            convs: {
                                ...state.convs,
                                [targetConvId]: { ...conv, messages: updatedMessages }
                            }
                        };
                    });
                } else {
                    // --- ADD NEW MESSAGE (Placeholder or new message) ---
                    addMessage({
                        convId: targetConvId,
                        message: {
                            id: content.id,
                            content: content.content,
                            source: content.source,
                            parentId,
                            timestamp: content.timestamp || Date.now(),
                            durationMs: content.durationMs, // Ensure this is explicitly included
                            costInfo: content.costInfo || null,
                            cumulativeCost: content.cumulativeCost,
                            attachments: attachments || undefined,
                            temperature: content.temperature
                        },
                    });
                    
                    // If it's an AI message, automatically select it
                    if (isAiMessage) {
                        setSelectedMessage({ convId: targetConvId, messageId: content.id });
                    }
                }
                
                // Special handling for AI messages: ensure they're selected in the UI
                //if (isAiMessage) {
                //    // 1. Directly update the store state to select this message
                //    //set(state => ({
                //    //    ...state,
                //    //    slctdMsgId: content.id
                //    //}));
                //
                //    // don't think we need these:
                //    // 2. Use a small delay to ensure selection persists even if other operations
                //    // might interfere with state updates
                //    //setTimeout(() => {
                //    //    set(state => ({
                //    //        ...state,
                //    //        slctdMsgId: content.id
                //    //    }));
                //    //}, 10);
                //    
                //    // 3. Also update via the standard setActiveConv method for completeness
                //    //setActiveConv({ convId: activeConvId, slctdMsgId: content.id });
                //}
                
            } else {
                
                const convId = `conv_${Date.now()}`;
                createConv({
                    id: convId,
                    rootMessage: {
                        id: content.id,
                        content: content.content,
                        source: content.source,
                        parentId: null,
                        timestamp: content.timestamp || Date.now(),
                        costInfo: content.costInfo || null,
                        cumulativeCost: content.cumulativeCost,
                    },
                });
                setSelectedMessage({ convId, messageId: content.id });
            }
        });

        listenToWebSocketEvent('conv:load', ({ content }) => {
            if (!content?.messages?.length) return;
            const { convId, messages } = content;

            const selectedMsgId = new URLSearchParams(window.location.search).get('messageId');
            const { createConv, addMessage, setSelectedMessage } = get();

            const graph = new MessageGraph(messages);
            const rootMsg = graph.getRootMessages()[0] || messages[0];
            
            createConv({
                id: convId,
                rootMessage: {
                    id: rootMsg.id,
                    content: rootMsg.content,
                    source: rootMsg.source as 'user' | 'ai' | 'system',
                    parentId: null,
                    timestamp: rootMsg.timestamp || Date.now(),
                    costInfo: rootMsg.costInfo || null,
                    cumulativeCost: rootMsg.cumulativeCost,
                },
            });

            messages.filter(m =>
                m.id !== rootMsg.id && (m.parentId || graph.getMessagePath(m.id).length > 1)
            )
                .sort((a, b) => a.timestamp - b.timestamp)
                .forEach(m => {
                    
                    const attachments = m.attachments && Array.isArray(m.attachments)
                        ? processAttachments(m.attachments)
                        : undefined;
                    
                    // Store processed attachments in the attachment store if they exist
                    if (attachments) {
                        useAttachmentStore.getState().addAttachmentsForId(m.id, attachments);
                    }

                    addMessage({
                        convId,
                        message: {
                            id: m.id,
                            content: m.content,
                            source: m.source as 'user' | 'ai' | 'system',
                            parentId: m.parentId,
                            timestamp: m.timestamp || Date.now(),
                            durationMs: m.durationMs,
                            costInfo: m.costInfo || null,
                            cumulativeCost: m.cumulativeCost,
                            attachments: attachments || undefined,
                            temperature: m.temperature
                        },
                    });
                });

            setSelectedMessage({
                convId,
                messageId: selectedMsgId || messages[messages.length - 1].id,
            });
        });
    }

    return {
        convs: {},
        activeConvId: null,
        selectedMessageId: null,
        editingMessageId: null,

        createConv: ({ id = `conv_${uuidv4()}`, rootMessage }) =>
            set(s => ({
                convs: { ...s.convs, [id]: { id, messages: [rootMessage] } },
                activeConvId: id,
                selectedMessageId: rootMessage.id,
            })),

        addMessage: ({ convId, message }) =>
            set(s => {
                const conv = s.convs[convId];
                if (!conv) {
                    return s;
                }
                
                // Create message with all properties explicitly copied
                const updMsg = { 
                    ...message, 
                    id: message.id,
                    content: message.content,
                    source: message.source,
                    timestamp: message.timestamp,
                    durationMs: message.durationMs, // Explicitly include durationMs
                    parentId: message.parentId || s.selectedMessageId || null,
                    costInfo: message.costInfo,
                    cumulativeCost: message.cumulativeCost,
                    attachments: message.attachments,
                    temperature: message.temperature
                };
                
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: [...conv.messages, updMsg] } },
                };
            }),

        setActiveConv: ({ convId }) => {
            const conv = get().convs[convId];
            if (!conv) return;

            // Default to selecting the latest message when a conversation becomes active
            const lastMessage = conv.messages.length > 0 ? conv.messages[conv.messages.length - 1] : null;
            set({
                activeConvId: convId,
                selectedMessageId: lastMessage ? lastMessage.id : null,
            });
        },
        
        setSelectedMessage: ({ convId, messageId }) => {
            // This action can also ensure the conversation is active
            set({ 
                activeConvId: convId, 
                selectedMessageId: messageId 
            });
        },

        getConv: convId => get().convs[convId],

        getActiveConv: () => {
            const { activeConvId, convs } = get();
            return activeConvId ? convs[activeConvId] : undefined;
        },

        updateMessage: ({ convId, messageId, content }) =>
            set(s => {
                const conv = s.convs[convId];
                if (!conv) return s;
                const idx = conv.messages.findIndex(m => m.id === messageId);
                if (idx === -1) return s;
                const msgs = [...conv.messages];
                msgs[idx] = { ...msgs[idx], content };

                import('../services/api/apiClient').then(({ updateMessage }) =>
                    updateMessage({ convId, messageId, content })
                        .catch(e => console.error('Failed to update message on server:', e))
                );

                return { convs: { ...s.convs, [convId]: { ...conv, messages: msgs } } };
            }),

        deleteMessage: ({ convId, messageId }) =>
            set(s => {
                const conv = s.convs[convId];
                if (!conv) return s;
                
                // Find all descendant message IDs including the message itself
                const toDelete = new Set<string>();
                
                // Helper function to recursively find all descendants
                const findDescendants = (id: string) => {
                    toDelete.add(id);
                    // Find all messages that have this message as parent
                    const children = conv.messages.filter(m => m.parentId === id);
                    // Recursively process each child
                    children.forEach(child => findDescendants(child.id));
                };
                
                // Start the recursive search
                findDescendants(messageId);
                
                // Filter out all messages marked for deletion
                const msgs = conv.messages.filter(m => !toDelete.has(m.id));
                
                // Update selected message ID if needed
                let newSelectedMsgId = s.selectedMessageId;
                if (toDelete.has(s.selectedMessageId || '')) {
                    const deletedMsg = conv.messages.find(m => m.id === messageId);
                    newSelectedMsgId = deletedMsg?.parentId || (msgs.length ? msgs[msgs.length - 1].id : null);
                }
                
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: msgs } },
                    selectedMessageId: newSelectedMsgId,
                };
            }),

        clearConv: convId =>
            set(s => {
                const conv = s.convs[convId];
                if (!conv) return s;
                const root = conv.messages.find(m => !m.parentId);
                if (!root) return s;
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: [root] } },
                    selectedMessageId: root.id,
                };
            }),

        deleteConv: convId =>
            set(s => {
                const { [convId]: convToDelete, ...rest } = s.convs;
                let newActive = s.activeConvId;
                let newSlctd = s.selectedMessageId;

                
                if (convToDelete) {
                    convToDelete.messages.forEach(message => {
                        if (message.attachments) {
                            // Use the attachment store to clean up attachments
                            useAttachmentStore.getState().removeAttachmentsForId(message.id);
                        }
                    });
                }

                if (s.activeConvId === convId) {
                    const ids = Object.keys(rest);
                    newActive = ids.length ? ids[0] : null;
                    newSlctd = newActive ? (rest[newActive].messages[0]?.id || null) : null;
                }

                return { convs: rest, activeConvId: newActive, selectedMessageId: newSlctd };
            }),

        editMessage: id => set(() => ({ editingMessageId: id })),
        cancelEditMessage: () => set(() => ({ editingMessageId: null })),
    };
});