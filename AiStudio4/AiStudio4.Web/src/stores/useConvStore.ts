import { create } from 'zustand';
import { v4 as uuidv4 } from 'uuid';
import { Message, Conv } from '@/types/conv';
import { MessageGraph } from '@/utils/messageGraph';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';

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

// Helper functions for handling attachments
const base64ToArrayBuffer = (base64: string): ArrayBuffer => {
    const binaryString = window.atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return bytes.buffer;
};

const createPreviewUrl = (attachment: any): string | undefined => {
    if (!attachment.type.startsWith('image/')) return undefined;

    try {
        const content = attachment.content;
        const blob = new Blob([content], { type: attachment.type });
        return URL.createObjectURL(blob);
    } catch (error) {
        console.error('Failed to create preview URL:', error);
        return undefined;
    }
};

export const useConvStore = create<ConvState>((set, get) => {
    if (typeof window !== 'undefined') {
        listenToWebSocketEvent('conv:new', ({ content }) => {
            if (!content) return;
            const { activeConvId, slctdMsgId, addMessage, createConv, setActiveConv, getConv } = get();

            if (activeConvId) {
                const conv = getConv(activeConvId);
                let parentId = content.parentId;

                if (!parentId && content.source === 'user') parentId = slctdMsgId;

                if (!parentId && conv?.messages.length > 0 && content.source === 'ai') {
                    const userMsgs = conv.messages
                        .filter(m => m.source === 'user')
                        .sort((a, b) => b.timestamp - a.timestamp);

                    parentId = userMsgs.length ? userMsgs[0].id : conv.messages[conv.messages.length - 1].id;
                }

                // Process attachments if they exist
                let attachments = content.attachments;
                if (attachments && Array.isArray(attachments)) {
                    // Convert base64 content back to ArrayBuffer if needed
                    attachments = attachments.map(att => {
                        if (typeof att.content === 'string') {
                            return {
                                ...att,
                                content: base64ToArrayBuffer(att.content),
                                previewUrl: att.previewUrl || createPreviewUrl(att)
                            };
                        }
                        return att;
                    });
                }

                addMessage({
                    convId: activeConvId,
                    message: {
                        id: content.id,
                        content: content.content,
                        source: content.source,
                        parentId,
                        timestamp: Date.now(),
                        costInfo: content.costInfo || null,
                        attachments: attachments || undefined
                    },
                    slctdMsgId: content.source === 'ai' ? content.id : undefined,
                });
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
                    // Process attachments if they exist
                    let attachments = m.attachments;
                    if (attachments && Array.isArray(attachments)) {
                        // Convert base64 content back to ArrayBuffer if needed
                        attachments = attachments.map(att => {
                            if (typeof att.content === 'string') {
                                return {
                                    ...att,
                                    content: base64ToArrayBuffer(att.content),
                                    previewUrl: att.previewUrl || createPreviewUrl(att)
                                };
                            }
                            return att;
                        });
                    }

                    addMessage({
                        convId,
                        message: {
                            id: m.id,
                            content: m.content,
                            source: m.source as 'user' | 'ai' | 'system',
                            parentId: m.parentId,
                            timestamp: m.timestamp || Date.now(),
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
                if (!conv) return s;
                const updMsg = { ...message, parentId: message.parentId || s.slctdMsgId || null };
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: [...conv.messages, updMsg] } },
                    slctdMsgId: slctdMsgId ?? s.slctdMsgId,
                };
            }),

        setActiveConv: ({ convId, slctdMsgId }) =>
            set(s => !s.convs[convId] ? s : {
                activeConvId: convId,
                slctdMsgId: slctdMsgId ?? s.slctdMsgId,
            }),

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
                const msgs = conv.messages.filter(m => m.id !== messageId);
                return {
                    convs: { ...s.convs, [convId]: { ...conv, messages: msgs } },
                    slctdMsgId: s.slctdMsgId === messageId ? (msgs.length ? msgs[msgs.length - 1].id : null) : s.slctdMsgId,
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

                // Cleanup attachment preview URLs
                if (convToDelete) {
                    convToDelete.messages.forEach(message => {
                        if (message.attachments) {
                            message.attachments.forEach(attachment => {
                                if (attachment.previewUrl) {
                                    URL.revokeObjectURL(attachment.previewUrl);
                                }
                                if (attachment.thumbnailUrl) {
                                    URL.revokeObjectURL(attachment.thumbnailUrl);
                                }
                            });
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