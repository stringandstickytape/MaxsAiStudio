
import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { v4 as uuidv4 } from 'uuid';
import { createResourceHook } from './useResourceFactory';


const useChatConfigResource = createResourceHook<{
  models: string[];
  defaultModel: string;
  secondaryModel: string;
}>({
  endpoints: {
    fetch: '/api/getConfig',
  },
  storeActions: {
    setItems: () => {}, 
  },
  options: {
    transformFetchResponse: (data) => [
      {
        models: data.models || [],
        defaultModel: data.defaultModel || '',
        secondaryModel: data.secondaryModel || '',
      },
    ],
  },
});
import { Attachment } from '@/types/attachment';

interface SendMessageParams {
  convId: string;
  parentMessageId: string;
  message: string;
  model: string;
  toolIds: string[];
  systemPromptId?: string;
  systemPromptContent?: string;
  messageId?: string;
  attachments?: Attachment[];
}

export function useChatManagement() {
  
  const { isLoading, error, executeApiCall, clearError } = useApiCallState();

  
  const { fetchItems: fetchConfigData } = useChatConfigResource();

  
  const { addMessage, createConv, activeConvId, convs } = useConvStore();

  const { prompts, convPrompts, defaultPromptId } = useSystemPromptStore();

  
  const { fetchConvTree } = useHistoricalConvsStore();

  
  const sendMessage = useCallback(
    async (params: SendMessageParams) => {
      return executeApiCall(async () => {
        
        const newMessageId = params.messageId || params.parentMessageId ? uuidv4() : undefined;

        // Add message with attachments to local state first
        if (params.convId && params.message) {
          addMessage({
            convId: params.convId,
            message: {
              id: params.messageId || `msg_${uuidv4()}`,
              content: params.message,
              source: 'user',
              parentId: params.parentMessageId,
              timestamp: Date.now(),
              attachments: params.attachments
            }
          });
        }

        // For server request, we need to convert ArrayBuffer to base64
        let requestParams = {...params};
        
        if (params.attachments && params.attachments.length > 0) {
          // Convert ArrayBuffer to base64 string for API transmission
          requestParams.attachments = params.attachments.map(attachment => ({
            ...attachment,
            content: arrayBufferToBase64(attachment.content)
          }));
        }

        const sendMessageRequest = createApiRequest('/api/chat', 'POST');
        const data = await sendMessageRequest({
          ...requestParams,
          newMessageId,
        });

        return {
          messageId: data.messageId,
          success: true,
        };
      });
    },
    [executeApiCall, addMessage],
  );

  // Helper function to convert ArrayBuffer to base64
  const arrayBufferToBase64 = (buffer: ArrayBuffer): string => {
    let binary = '';
    const bytes = new Uint8Array(buffer);
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
  };

  

  const getConfig = useCallback(async () => {
    const config = await fetchConfigData();
    return (
      config?.[0] || {
        models: [],
        defaultModel: '',
        secondaryModel: '',
      }
    );
  }, [fetchConfigData]);

  
  const setDefaultModel = useCallback(
    async (modelName: string) => {
      return (
        executeApiCall(async () => {
          const setDefaultModelRequest = createApiRequest('/api/setDefaultModel', 'POST');
          await setDefaultModelRequest({ modelName });
          return true;
        }) || false
      );
    },
    [executeApiCall],
  );

  
  const setSecondaryModel = useCallback(
    async (modelName: string) => {
      return (
        executeApiCall(async () => {
          const setSecondaryModelRequest = createApiRequest('/api/setSecondaryModel', 'POST');
          await setSecondaryModelRequest({ modelName });
          return true;
        }) || false
      );
    },
    [executeApiCall],
  );

  
  const getConv = useCallback(
    async (convId: string) => {
      
      const localConv = convs[convId];
      if (localConv) {
        return {
          id: convId,
          messages: localConv.messages,
        };
      }

      
      return executeApiCall(
        async () => {
          
          const treeData = await fetchConvTree(convId);

          if (!treeData) {
            throw new Error('Failed to get conv tree');
          }

          
          
          const extractNodes = (node: any, nodes: any[] = []) => {
            if (!node) return nodes;

            nodes.push({
              id: node.id,
              text: node.text,
              parentId: node.parentId,
              source: node.source,
              costInfo: node.costInfo
            });

            if (node.children && Array.isArray(node.children)) {
              for (const child of node.children) {
                extractNodes(child, nodes);
              }
            }

            return nodes;
          };

          const flatNodes = extractNodes(treeData);

          
          const messages = flatNodes.map((node) => ({
            id: node.id,
            content: node.text,
            source:
              node.source ||
              (node.id.includes('user') ? 'user' : node.id.includes('ai') || node.id.includes('msg') ? 'ai' : 'system'),
            parentId: node.parentId,
            timestamp: Date.now(), 
            costInfo: node.costInfo || null,
          }));

          return {
            id: convId,
            messages: messages,
            summary: 'Loaded Conv', 
          };
        },
        convs,
        fetchConvTree,
      );
    },
    [convs, fetchConvTree, executeApiCall],
  );

  
  const getSystemPromptForConv = useCallback(
    (convId: string) => {
      
      let promptId = convId ? convPrompts[convId] : null;

      
      if (!promptId) {
        promptId = defaultPromptId;
      }

      
      if (promptId) {
        const prompt = prompts.find((p) => p.guid === promptId);
        if (prompt) {
          return {
            id: prompt.guid,
            content: prompt.content,
          };
        }
      }

      
      const defaultPrompt = prompts.find((p) => p.isDefault);
      if (defaultPrompt) {
        return {
          id: defaultPrompt.guid,
          content: defaultPrompt.content,
        };
      }

      
      return null;
    },
    [prompts, convPrompts, defaultPromptId],
  );

  const cancelMessage = useCallback(
    async (params: { convId: string; messageId: string }) => {
      return executeApiCall(async () => {
        const cancelRequest = createApiRequest('/api/cancelRequest', 'POST');
        const data = await cancelRequest(params);
        return data;
      });
    },
    [executeApiCall]
  );

  return {
    
    isLoading,
    error,
    activeConvId,
    convs,

    
    sendMessage,
    cancelMessage,
    getConfig,
    setDefaultModel,
    setSecondaryModel,
    getConv,
    getSystemPromptForConv,
    clearError,
  };
}

