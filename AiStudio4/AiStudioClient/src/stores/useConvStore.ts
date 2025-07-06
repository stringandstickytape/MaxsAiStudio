import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Message, Conv } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { processAttachments } from '@/utils/attachmentUtils';
import type { ContentBlock } from '@/types/conv';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { MessageUtils } from '@/utils/messageUtils';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';

interface ConvState {
    convs: Record<string, Conv>;
    activeConvId: string | null;
    slctdMsgId: string | null;
    editingMessageId: string | null;
    editingBlock: { messageId: string; blockIndex: number; } | null;
    createConv: (p: { id?: string; rootMessage: Message; slctdMsgId?: string }) => void;
    addMessage: (p: { convId: string; message: Message }) => void;
    setActiveConv: (convId: string) => void;
    selectMessage: (convId: string, messageId: string) => void;
    getConv: (convId: string) => Conv | undefined;
    getActiveConv: () => Conv | undefined;
    updateMessage: (p: { convId: string; messageId: string; newBlocks: ContentBlock[] }) => void;
    updateMessageBlock: (convId: string, messageId: string, blockIndex: number, newContent: string) => void;
    deleteMessage: (p: { convId: string; messageId: string }) => void;
    clearConv: (convId: string) => void;
    deleteConv: (convId: string) => void;
    editMessage: (messageId: string | null) => void;
    editBlock: (messageId: string, blockIndex: number) => void;
    cancelEditMessage: () => void;
    cancelEditBlock: () => void;
    loadOrGetConv: (convId: string) => Promise<{ id: string; messages: Message[]; summary?: string } | null>;
}



export const useConvStore = create<ConvState>((set, get) => {
    
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('conv:upd', ({ content }) => {
            if (!content) return;
            const { activeConvId, slctdMsgId, addMessage, createConv, setActiveConv, getConv } = get();
            
            // Get the convId from the message if available, otherwise use activeConvId
            const targetConvId = content.convId || activeConvId;
            
            if (targetConvId) {
                
                const conv = getConv(targetConvId);
                let parentId = content.parentId;

                if (!parentId && content.source === 'user') parentId = slctdMsgId;

                // Check for both 'ai' and 'assistant' sources
                const isAiMessage = content.source === 'ai' || content.source === 'assistant';
                
                if (!parentId && conv?.messages.length > 0 && isAiMessage) {
                    const userMsgs = conv.messages
                        .filter(m => m.source === 'user')
                        .sort((a, b) => b.timestamp - a.timestamp);

                    parentId = userMsgs.length ? userMsgs[0].id : conv.messages[conv.messages.length - 1].id;
                }

                
                const contentBlocks: ContentBlock[] = content.contentBlocks ?? [];

                const attachments = content.attachments && Array.isArray(content.attachments) 
                    ? processAttachments(content.attachments)
                    : undefined;

                // Store processed attachments in the attachment store if they exist
                if (attachments) {
                    useAttachmentStore.getState().addAttachmentsForId(content.id, attachments);
                }
                
                // Check if message already exists (for updates)
                const existingMessageIndex = conv?.messages.findIndex(m => m.id === content.id) ?? -1;
                
                if (existingMessageIndex !== -1) {
                    // --- UPDATE EXISTING MESSAGE ---
                    set(state => {
                        const updatedMessages = [...conv.messages];
                        const existingMessage = updatedMessages[existingMessageIndex];
                        updatedMessages[existingMessageIndex] = { 
                            ...existingMessage, 
                            contentBlocks: contentBlocks,
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
                            contentBlocks: contentBlocks,
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
                    
                    // For AI messages with empty content blocks, immediately mark as streaming
                    // This ensures the UI component can set up listeners before first cfrag arrives
                    const isAiMessage = content.source === 'ai' || content.source === 'assistant';
                    const hasEmptyContent = !contentBlocks?.length || contentBlocks.every(block => !block.content?.trim());
                    console.log(`🏗️ ConvStore: Added message ${content.id}, isAi: ${isAiMessage}, hasEmptyContent: ${hasEmptyContent}`);
                    if (isAiMessage && hasEmptyContent) {
                        console.log(`🎯 ConvStore: Pre-marking ${content.id} as streaming before first cfrag`);
                        // Import and use WebSocket store to mark as streaming immediately
                        import('@/stores/useWebSocketStore').then(({ useWebSocketStore }) => {
                            useWebSocketStore.getState().addStreamingMessage(content.id);
                            console.log(`✅ ConvStore: Successfully pre-marked ${content.id} as streaming`);
                        });
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
                        contentBlocks: content.contentBlocks ?? [],
                        source: content.source,
                        parentId: null,
                        timestamp: content.timestamp || Date.now(),
                        costInfo: content.costInfo || null,
                        cumulativeCost: content.cumulativeCost,
                    },
                    slctdMsgId: content.id,
                });
                setActiveConv(convId);
            }
        });

        listenToWebSocketEvent('conv:load', ({ content }) => {
            if (!content?.messages?.length) return;
            const { convId, messages } = content as any;

            const slctdMsgId = new URLSearchParams(window.location.search).get('messageId');
            const { createConv, addMessage, setActiveConv } = get();

            const graph = new MessageGraph(messages);
            const rootMsg = graph.getRootMessages()[0] || messages[0];
            
            createConv({
                id: convId,
                rootMessage: {
                    id: rootMsg.id,
                    contentBlocks: rootMsg.contentBlocks ?? [] as any,
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

                    const mBlocks: ContentBlock[] = m.contentBlocks ?? [];

                    addMessage({
                        convId,
                        message: {
                            id: m.id,
                            contentBlocks: mBlocks,
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

            setActiveConv(convId);
            
            // Select the specific message if provided, otherwise select the latest
            const targetMessageId = slctdMsgId || messages[messages.length - 1].id;
            // Use setTimeout to avoid infinite loop by deferring the selectMessage call
            setTimeout(() => {
                get().selectMessage(convId, targetMessageId);
            }, 0);
        });
    }

    return {
        convs: {},
        activeConvId: null,
        slctdMsgId: null,
        editingMessageId: null,
        editingBlock: null,

        createConv: ({ id = `conv_${uuidv4()}`, rootMessage, slctdMsgId }) =>
            set(s => ({
                convs: { ...s.convs, [id]: { id, messages: [rootMessage] } },
                activeConvId: id,
                slctdMsgId: slctdMsgId || rootMessage.id,
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
                    contentBlocks: message.contentBlocks ?? [],
                    source: message.source,
                    timestamp: message.timestamp,
                    durationMs: message.durationMs, // Explicitly include durationMs
                    parentId: message.parentId || s.slctdMsgId || null,
                    costInfo: message.costInfo,
                    cumulativeCost: message.cumulativeCost,
                    attachments: message.attachments,
                    temperature: message.temperature
                };
                
                // Automatically select AI messages when they are added
                const isAiMessage = message.source === 'ai' || message.source === 'assistant';
                const newSelectedMsgId = isAiMessage ? message.id : s.slctdMsgId;
                
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: [...conv.messages, updMsg] } },
                    slctdMsgId: newSelectedMsgId,
                };
            }),

        setActiveConv: (convId: string) => {
            return set(s => {
                const conv = s.convs[convId];
                if (!conv) return s;
                
                // Automatically select the latest message when switching conversations
                const latestMessage = conv.messages.reduce((latest, current) => 
                    current.timestamp > latest.timestamp ? current : latest
                );
                
                return {
                    ...s,
                    activeConvId: convId,
                    slctdMsgId: latestMessage.id,
                };
            });
        },

        selectMessage: (convId: string, messageId: string) =>
            set(state => {
                if (!state.convs[convId]) return state; // Ensure conv exists

                return {
                    ...state,
                    activeConvId: convId, // Also ensures the conv is active
                    slctdMsgId: messageId
                };
            }),

        getConv: convId => get().convs[convId],

        getActiveConv: () => {
            const { activeConvId, convs } = get();
            return activeConvId ? convs[activeConvId] : undefined;
        },

        updateMessage: ({ convId, messageId, newBlocks }) =>
            set(s => {
                const conv = s.convs[convId];
                if (!conv) return s;
                const idx = conv.messages.findIndex(m => m.id === messageId);
                if (idx === -1) return s;
                const msgs = [...conv.messages];
                msgs[idx] = { 
                    ...msgs[idx], 
                    contentBlocks: newBlocks
                };

                import('../services/api/apiClient').then(({ updateMessage }) =>
                    updateMessage({ convId, messageId, contentBlocks: newBlocks })
                        .catch(e => { /* Failed to update message on server */ })
                );

                return { convs: { ...s.convs, [convId]: { ...conv, messages: msgs } } };
            }),

        updateMessageBlock: (convId, messageId, blockIndex, newContent) =>
            set(s => {
                const conv = s.convs[convId];
                if (!conv) return s;
                const idx = conv.messages.findIndex(m => m.id === messageId);
                if (idx === -1) return s;
                const message = conv.messages[idx];
                if (!message.contentBlocks || blockIndex >= message.contentBlocks.length) return s;
                
                const msgs = [...conv.messages];
                const updatedBlocks = [...message.contentBlocks];
                updatedBlocks[blockIndex] = { 
                    ...updatedBlocks[blockIndex],
                    content: newContent 
                };
                msgs[idx] = { 
                    ...msgs[idx], 
                    contentBlocks: updatedBlocks
                };

                import('../services/api/apiClient').then(({ updateMessage }) =>
                    updateMessage({ convId, messageId, contentBlocks: updatedBlocks })
                        .catch(e => { /* Failed to update message on server */ })
                );

                return { 
                    convs: { ...s.convs, [convId]: { ...conv, messages: msgs } },
                    editingBlock: null
                };
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
                let newSelectedMsgId = s.slctdMsgId;
                if (toDelete.has(s.slctdMsgId || '')) {
                    newSelectedMsgId = msgs.length ? msgs[msgs.length - 1].id : null;
                }
                
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: msgs } },
                    slctdMsgId: newSelectedMsgId,
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
                    slctdMsgId: root.id,
                };
            }),

        deleteConv: convId =>
            set(s => {
                const { [convId]: convToDelete, ...rest } = s.convs;
                let newActive = s.activeConvId;
                let newSlctd = s.slctdMsgId;

                
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

                return { convs: rest, activeConvId: newActive, slctdMsgId: newSlctd };
            }),

        editMessage: id => set(() => ({ editingMessageId: id })),
        editBlock: (messageId, blockIndex) => set(() => ({ editingBlock: { messageId, blockIndex } })),
        cancelEditMessage: () => set(() => ({ editingMessageId: null })),
        cancelEditBlock: () => set(() => ({ editingBlock: null })),
        
        loadOrGetConv: async (convId: string) => {
            const { convs } = get();
            const localConv = convs[convId];
            
            if (localConv) {
                return {
                    id: convId,
                    messages: localConv.messages,
                };
            }

            try {
                const { fetchConvTree } = useHistoricalConvsStore.getState();
                const treeData = await fetchConvTree(convId);
                if (!treeData) {
                    return null;
                }

                const { processAttachments } = await import('@/utils/attachmentUtils');
                const { MessageGraph } = await import('@/utils/messageGraph');

                const extractNodes = (node: any, nodes: any[] = []): any[] => {
                    if (!node) return nodes;
                    const { children, ...rest } = node;
                    nodes.push(rest);
                    if (children && Array.isArray(children)) {
                        for (const child of children) { 
                            extractNodes(child, nodes); 
                        }
                    }
                    return nodes;
                };

                const flatNodes = extractNodes(treeData);
                const messages = flatNodes.map((node) => {
                    let attachments = node.attachments;
                    if (attachments && Array.isArray(attachments)) {
                        attachments = processAttachments(attachments);
                    }
                    return {
                        id: node.id,
                        contentBlocks: node.contentBlocks ?? 
                            (node.text ? [{ content: node.text, contentType: 'text' }] : []),
                        source: node.source || (node.id.includes('user') ? 'user' : 'ai'),
                        parentId: node.parentId,
                        timestamp: typeof node.timestamp === 'number' ? node.timestamp : Date.now(),
                        durationMs: typeof node.durationMs === 'number' ? node.durationMs : undefined,
                        costInfo: node.costInfo || null,
                        cumulativeCost: node.cumulativeCost,
                        attachments: attachments || undefined,
                        temperature: node.temperature || null
                    };
                });

                // Add the loaded conversation to the store
                if (messages.length > 0) {
                    const rootMessage = messages.find(m => !m.parentId) || messages[0];
                    const newConv: Conv = {
                        id: convId,
                        messages: messages
                    };
                    
                    set(state => ({
                        convs: { ...state.convs, [convId]: newConv }
                    }));
                }

                return { id: convId, messages, summary: 'Loaded Conv' };
            } catch (error) {
                console.error('Failed to load conversation:', error);
                return null;
            }
        },
    };
});

/* ────────────────────────────────
Debug helper for the Conv store
──────────────────────────────── */
export const debugConvStore = () => {
    const state = useConvStore.getState();


    return state; // handy if you want to inspect it further in the console
};

/* Expose globally only when we’re in a browser */
if (typeof window !== 'undefined') {
    (window as any).debugConvStore = debugConvStore;
}