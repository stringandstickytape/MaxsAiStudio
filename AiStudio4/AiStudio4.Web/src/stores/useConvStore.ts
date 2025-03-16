
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

  createConv: (params: { id?: string; rootMessage: Message; slctdMsgId?: string }) => void;

  addMessage: (params: { convId: string; message: Message; slctdMsgId?: string | null }) => void;

  setActiveConv: (params: { convId: string; slctdMsgId?: string | null }) => void;

  getConv: (convId: string) => Conv | undefined;
  getActiveConv: () => Conv | undefined;

  updateMessage: (params: { convId: string; messageId: string; content: string }) => void;

  deleteMessage: (params: { convId: string; messageId: string }) => void;

  clearConv: (convId: string) => void;

  deleteConv: (convId: string) => void;
  
  editMessage: (messageId: string | null) => void;
  cancelEditMessage: () => void;
}

export const useConvStore = create<ConvState>((set, get) => {
  if (typeof window !== 'undefined') {
    listenToWebSocketEvent('conv:new', (detail) => {
      const content = detail.content;
      if (!content) return;

      const { activeConvId, slctdMsgId, addMessage, createConv, setActiveConv, getConv } = get();

      if (activeConvId) {
        const conv = getConv(activeConvId);
        
        let parentId = content.parentId;
        
        if (!parentId && content.source === 'user') {
          parentId = slctdMsgId;
        }
        
        if (!parentId && conv?.messages.length > 0 && content.source === 'ai') {
          const userMessages = conv.messages
            .filter(m => m.source === 'user')
            .sort((a, b) => b.timestamp - a.timestamp);
          
          parentId = userMessages.length > 0 
            ? userMessages[0].id 
            : conv.messages[conv.messages.length - 1].id;
        }

        addMessage({
          convId: activeConvId,
          message: {
            id: content.id,
            content: content.content,
            source: content.source,
            parentId: parentId,
            timestamp: Date.now(),
            costInfo: content.costInfo || null,
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

        setActiveConv({
          convId,
          slctdMsgId: content.id,
        });
      }
    });

    listenToWebSocketEvent('conv:load', (detail) => {
      const content = detail.content;
      if (!content) return;

      const { convId, messages } = content;
      if (!messages?.length) return;
      
      const slctdMsgId = new URLSearchParams(window.location.search).get('messageId');
      const { createConv, addMessage, setActiveConv } = get();

      const graph = new MessageGraph(messages);
      const rootMessage = graph.getRootMessages()[0] || messages[0];

      createConv({
        id: convId,
        rootMessage: {
          id: rootMessage.id,
          content: rootMessage.content,
          source: rootMessage.source as 'user' | 'ai' | 'system',
          parentId: null,
          timestamp: rootMessage.timestamp || Date.now(),
          costInfo: rootMessage.costInfo || null,
        },
      });

      messages.filter(msg => 
        msg.id !== rootMessage.id && (msg.parentId || graph.getMessagePath(msg.id).length > 1)
      )
      .sort((a, b) => a.timestamp - b.timestamp)
      .forEach(message => {
          addMessage({
            convId,
            message: {
              id: message.id,
              content: message.content,
              source: message.source as 'user' | 'ai' | 'system',
              parentId: message.parentId,
              timestamp: message.timestamp || Date.now(),
              costInfo: message.costInfo || null,
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
      set(state => {
        const newConv: Conv = {
          id,
          messages: [rootMessage],
        };

        return {
          convs: {
            ...state.convs,
            [id]: newConv,
          },
          activeConvId: id,
          slctdMsgId: slctdMsgId || rootMessage.id,
        };
      }),

    addMessage: ({ convId, message, slctdMsgId }) =>
      set(state => {
        const conv = state.convs[convId];
        if (!conv) {
          return state;
        }

        const updatedMessage = { 
          ...message, 
          parentId: message.parentId || state.slctdMsgId || null 
        };
        const updatedMessages = [...conv.messages, updatedMessage];

        return {
          convs: {
            ...state.convs,
            [convId]: {
              ...conv,
              messages: updatedMessages,
            },
          },
          slctdMsgId: slctdMsgId ?? state.slctdMsgId,
        };
      }),

    setActiveConv: ({ convId, slctdMsgId }) =>
      set(state => {
        if (!state.convs[convId]) {
          return state;
        }

        return {
          activeConvId: convId,
          slctdMsgId: slctdMsgId ?? state.slctdMsgId,
        };
      }),

    getConv: convId => get().convs[convId],

    getActiveConv: () => {
      const { activeConvId, convs } = get();
      return activeConvId ? convs[activeConvId] : undefined;
    },

    updateMessage: ({ convId, messageId, content }) =>
      set(state => {
        const conv = state.convs[convId];
        if (!conv) return state;

        const messageIndex = conv.messages.findIndex((msg) => msg.id === messageId);
        if (messageIndex === -1) return state;

        const updatedMessages = [...conv.messages];
        updatedMessages[messageIndex] = { ...updatedMessages[messageIndex], content };
        
        import('../services/api/apiClient').then(({ updateMessage }) => 
          updateMessage({ convId, messageId, content })
            .catch(error => console.error('Failed to update message on server:', error))
        );

        return {
          convs: {
            ...state.convs,
            [convId]: {
              ...conv,
              messages: updatedMessages,
            },
          },
        };
      }),

    deleteMessage: ({ convId, messageId }) =>
      set(state => {
        const conv = state.convs[convId];
        if (!conv) return state;

        const updatedMessages = conv.messages.filter((msg) => msg.id !== messageId);

        const updatedSlctdMsgId = state.slctdMsgId === messageId
          ? updatedMessages.length > 0 ? updatedMessages[updatedMessages.length - 1].id : null
          : state.slctdMsgId;

        return {
          convs: {
            ...state.convs,
            [convId]: {
              ...conv,
              messages: updatedMessages,
            },
          },
          slctdMsgId: updatedSlctdMsgId,
        };
      }),

    clearConv: convId =>
      set(state => {
        const conv = state.convs[convId];
        if (!conv) return state;

        const rootMessage = conv.messages.find((msg) => !msg.parentId);
        if (!rootMessage) return state;

        return {
          convs: {
            ...state.convs,
            [convId]: {
              ...conv,
              messages: [rootMessage],
            },
          },
          slctdMsgId: rootMessage.id,
        };
      }),

      deleteConv: convId =>
          set(state => {
              const { [convId]: _, ...remainingConvs } = state.convs;

              let newActiveId = state.activeConvId;
              let newSlctdMsgId = state.slctdMsgId;

              if (state.activeConvId === convId) {
                  const convIds = Object.keys(remainingConvs);
                  newActiveId = convIds.length > 0 ? convIds[0] : null;
                  newSlctdMsgId = newActiveId ? (remainingConvs[newActiveId].messages[0]?.id || null) : null;
              }

              return {
                  convs: remainingConvs,
                  activeConvId: newActiveId,
                  slctdMsgId: newSlctdMsgId,
              };
          }),

      
      editMessage: messageId => set(() => ({ editingMessageId: messageId })),
      
      cancelEditMessage: () => set(() => ({ editingMessageId: null })),
  };
});

