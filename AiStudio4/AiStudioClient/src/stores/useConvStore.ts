import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Message, Conv } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { processAttachments, cleanupAttachmentUrls } from '@/utils/attachmentUtils';

interface ConvState {
    convs: Record<string, Conv>;
    activeConvId: string | null;
    slctdMsgId: string | null;
    editingMessageId: string | null;
    createConv: (p: { id?: string; rootMessage: Message; slctdMsgId?: string }) => void;
    addMessage: (p: { convId: string; message: Message; slctdMsgId?: string | null }) => void;
    setActiveConv: (p: { convId: string; slctdMsgId?: string | null }) => void;
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
    console.log('useConvStore');
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('conv:upd', ({ content }) => {
            if (!content) return;
            const { activeConvId, slctdMsgId, addMessage, createConv, setActiveConv, getConv } = get();
            
            if (activeConvId) {
                console.log('UCS 1');
                const conv = getConv(activeConvId);
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

                
                const attachments = content.attachments && Array.isArray(content.attachments) 
                    ? processAttachments(content.attachments)
                    : undefined;

                // First add the message to the conversation
                addMessage({
                    convId: activeConvId,
                    message: {
                        id: content.id,
                        content: content.content,
                        source: content.source,
                        parentId,
                        timestamp: content.timestamp || Date.now(),
                        durationMs: content.durationMs, // Ensure this is explicitly included
                        costInfo: content.costInfo || null,
                        attachments: attachments || undefined
                    },
                    slctdMsgId: isAiMessage ? content.id : false,
                });
                
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
                console.log('UCS 2');
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
                    },
                });
                setActiveConv({ convId, slctdMsgId: content.id });
            }
        });

        listenToWebSocketEvent('conv:load', ({ content }) => {
            if (!content?.messages?.length) return;
            const { convId, messages } = content;

            const slctdMsgId = new URLSearchParams(window.location.search).get('messageId');
            const { createConv, addMessage, setActiveConv } = get();

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
                            attachments: attachments || undefined
                        },
                    });
                });

            setActiveConv({
                convId,
                slctdMsgId: slctdMsgId || messages[messages.length - 1].id,
            });
        });
    }

    return {
        convs: {},
        activeConvId: null,
        slctdMsgId: null,
        editingMessageId: null,

        createConv: ({ id = `conv_${uuidv4()}`, rootMessage, slctdMsgId }) =>
            set(s => ({
                convs: { ...s.convs, [id]: { id, messages: [rootMessage] } },
                activeConvId: id,
                slctdMsgId: slctdMsgId || rootMessage.id,
            })),

        addMessage: ({ convId, message, slctdMsgId }) =>
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
                    parentId: message.parentId || s.slctdMsgId || null,
                    costInfo: message.costInfo,
                    attachments: message.attachments
                };
                
                const newSelectedMsgId = slctdMsgId ?? s.slctdMsgId;
                
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: [...conv.messages, updMsg] } },
                    slctdMsgId: newSelectedMsgId,
                };
            }),

        setActiveConv: ({ convId, slctdMsgId }) => {
            return set(s => {
                if (!s.convs[convId]) {
                    return s;
                }
                const newSelectedMsgId = slctdMsgId ?? s.slctdMsgId;
                return {
                    activeConvId: convId,
                    slctdMsgId: newSelectedMsgId,
                };
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
                            cleanupAttachmentUrls(message.attachments);
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
        cancelEditMessage: () => set(() => ({ editingMessageId: null })),
    };
});