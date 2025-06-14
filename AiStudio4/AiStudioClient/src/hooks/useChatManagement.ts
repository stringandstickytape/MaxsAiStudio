import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { v4 as uuidv4 } from 'uuid';
import { prepareAttachmentsForTransmission, isTextFile } from '@/utils/attachmentUtils';
import { useAttachmentStore } from '@/stores/useAttachmentStore';
import { Attachment } from '@/types/attachment';
import { useModelStore } from '@/stores/useModelStore';
import { useToolStore } from '@/stores/useToolStore';
import { createResourceHook } from './useResourceFactory';

// --- FIX: Moved this hook definition to the top of the file, before it is used. ---
const useChatConfigResource = createResourceHook<{
    models: Array<{ guid: string; name: string; friendlyName: string }>;
    defaultModel: string;
    defaultModelGuid: string;
    secondaryModel: string;
    secondaryModelGuid: string;
}>({
    endpoints: {
        fetch: '/api/getConfig',
    },
    storeActions: {
        setItems: () => { },
    },
    options: {
        transformFetchResponse: (data) => [
            {
                models: data.models || [],
                defaultModel: data.defaultModel || '',
                defaultModelGuid: data.defaultModelGuid || '',
                secondaryModel: data.secondaryModel || '',
                secondaryModelGuid: data.secondaryModelGuid || '',
            },
        ],
    },
});


interface SendMessageParams {
    convId: string;
    message: string;
    systemPromptId?: string;
    systemPromptContent?: string;
    messageId?: string;
    attachments?: Attachment[];
}

export function useChatManagement() {
    const { isLoading, error, executeApiCall, clearError } = useApiCallState();

    // --- FIX: Correctly call the resource hook and alias fetchItems to getConfig ---
    const { fetchItems: getConfig } = useChatConfigResource();

    const sendMessage = useCallback(
        async (params: Omit<SendMessageParams, 'model'>) => {
            return executeApiCall(async () => {
                const { slctdMsgId } = useConvStore.getState(); // Get it from the store
                const parentMessageId = slctdMsgId; // Use the current selected message ID
                
                const newMessageId = params.messageId || (parentMessageId ? uuidv4() : undefined);
                
                // Get current active tools from store to ensure they're fresh
                const { activeTools } = useToolStore.getState();
                
                let requestParams: any = { ...params, parentMessageId, toolIds: activeTools };


                if (params.attachments && params.attachments.length > 0) {
                    const binaryAttachments = params.attachments.filter(att => !att.textContent && !isTextFile(att.type));
                    requestParams.attachments = prepareAttachmentsForTransmission(binaryAttachments);
                    if (newMessageId) {
                        useAttachmentStore.getState().addAttachmentsForId(newMessageId, params.attachments);
                    }
                }

                const { models, selectedPrimaryModelGuid } = useModelStore.getState();
                const modelToUse = models.find(m => m.guid === selectedPrimaryModelGuid);

                if (!modelToUse) {
                    throw new Error("No primary model selected or found.");
                }
                

                const sendMessageRequest = createApiRequest('/api/chat', 'POST');
                const data = await sendMessageRequest({
                    ...requestParams,
                    newMessageId,
                    model: modelToUse.friendlyName,
                });

                return {
                    messageId: data.messageId,
                    success: true,
                };
            });
        },
        [executeApiCall],
    );

    // --- FIX: The redundant getConfig function has been removed. ---
    // The one from useChatConfigResource is used instead.

    const setDefaultModel = useCallback(
        async (modelIdentifier: string, isGuid = true) => {
            return executeApiCall(async () => {
                const setDefaultModelRequest = createApiRequest('/api/setDefaultModel', 'POST');
                const payload = isGuid ? { modelGuid: modelIdentifier } : { modelName: modelIdentifier };
                await setDefaultModelRequest(payload);
                return true;
            });
        },
        [executeApiCall],
    );

    const setSecondaryModel = useCallback(
        async (modelIdentifier: string, isGuid = true) => {
            return executeApiCall(async () => {
                const setSecondaryModelRequest = createApiRequest('/api/setSecondaryModel', 'POST');
                const payload = isGuid ? { modelGuid: modelIdentifier } : { modelName: modelIdentifier };
                await setSecondaryModelRequest(payload);
                return true;
            });
        },
        [executeApiCall],
    );

    const getConv = useCallback(
        async (convId: string) => {
            const { convs } = useConvStore.getState();
            const localConv = convs[convId];
            if (localConv) {
                return {
                    id: convId,
                    messages: localConv.messages,
                };
            }

            return executeApiCall(async () => {
                const { fetchConvTree } = useHistoricalConvsStore.getState();
                const treeData = await fetchConvTree(convId);
                if (!treeData) {
                    throw new Error('Failed to get conv tree');
                }
                const { processAttachments } = await import('@/utils/attachmentUtils');
                const { MessageGraph } = await import('@/utils/messageGraph');

                const extractNodes = (node: any, nodes: any[] = []) => {
                    if (!node) return nodes;
                    const { children, ...rest } = node;
                    nodes.push(rest);
                    if (children && Array.isArray(children)) {
                        for (const child of children) { extractNodes(child, nodes); }
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
                return { id: convId, messages, summary: 'Loaded Conv' };
            });
        },
        [executeApiCall],
    );

    const getSystemPromptForConv = useCallback(
        (convId: string) => {
            const { prompts, convPrompts, defaultPromptId } = useSystemPromptStore.getState();
            let promptId = convId ? convPrompts[convId] : null;
            if (!promptId) {
                promptId = defaultPromptId;
            }
            if (promptId) {
                const prompt = prompts.find((p) => p.guid === promptId);
                if (prompt) {
                    return { id: prompt.guid, content: prompt.content };
                }
            }
            const defaultPrompt = prompts.find((p) => p.isDefault);
            if (defaultPrompt) {
                return { id: defaultPrompt.guid, content: defaultPrompt.content };
            }
            return null;
        },
        [],
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
        sendMessage,
        cancelMessage,
        getConfig, // <-- Now correctly refers to the function from the resource hook
        setDefaultModel,
        setSecondaryModel,
        getConv,
        getSystemPromptForConv,
        clearError,
    };
}