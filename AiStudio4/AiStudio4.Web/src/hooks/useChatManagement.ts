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
        setItems: () => { },
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
import { isTextFile } from './useAttachmentManager';
import { arrayBufferToBase64, processAttachments } from '@/utils/bufferUtils';

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
                // Generate a message ID if not provided
                const newMessageId = params.messageId || params.parentMessageId ? uuidv4() : undefined;

                // REMOVE THIS BLOCK: Don't add the message to local state before server confirmation
                // (removed)

                // For server request, we need to convert ArrayBuffer to base64
                let requestParams = { ...params };

                if (params.attachments && params.attachments.length > 0) {
                    // Filter out text files as they should already be in the message content
                    const binaryAttachments = params.attachments.filter(att => !att.textContent && !isTextFile(att.type));
                    // Convert ArrayBuffer to base64 string for API transmission
                    requestParams.attachments = binaryAttachments.map(attachment => ({
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

    // Using imported utility functions for conversions



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
            console.log('Getting conversation:', convId);

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
                    
                    console.log('Tree data received in getConv:', treeData);
                    console.log('Looking at attachments in tree data nodes:', treeData.children?.map(c => ({ id: c.id, hasAttachments: !!c.attachments, attachments: c.attachments })));



                    const extractNodes = (node: any, nodes: any[] = []) => {
                        if (!node) return nodes;

                        // Log the attachments for debugging
                        if (node.attachments) {
                            console.log(`Node ${node.id} has attachments:`, node.attachments);
                        }

                        nodes.push({
                            id: node.id,
                            text: node.text,
                            parentId: node.parentId,
                            source: node.source,
                            costInfo: node.costInfo,
                            attachments: node.attachments  // Explicitly include attachments
                        });

                        if (node.children && Array.isArray(node.children)) {
                            for (const child of node.children) {
                                extractNodes(child, nodes);
                            }
                        }

                        return nodes;
                    };

                    const flatNodes = extractNodes(treeData);
                    
                    // Debug: Log every node with its attachment status
                    console.log('Extracted nodes with attachment info:', 
                      flatNodes.map(node => ({
                        id: node.id, 
                        text: node.text?.substring(0, 20) + '...', 
                        hasAttachments: !!node.attachments,
                        attachmentsCount: node.attachments?.length
                      })));


                    console.log('Flat nodes to process in getConv:', flatNodes);
                    
                    const messages = flatNodes.map((node) => {
                        console.log('Processing node for message:', node);
                        // Process attachments if present
                        let attachments = node.attachments;
                        console.log('Node attachments:', attachments);
                        if (attachments && Array.isArray(attachments)) {
                            attachments = attachments.map(att => {
                                if (typeof att.content === 'string') {
                                    // Convert base64 to ArrayBuffer
                                    const binaryString = window.atob(att.content);
                                    const bytes = new Uint8Array(binaryString.length);
                                    for (let i = 0; i < binaryString.length; i++) {
                                        bytes[i] = binaryString.charCodeAt(i);
                                    }
                                    const buffer = bytes.buffer;
                                    
                                    // Create preview URL for images
                                    let previewUrl;
                                    if (att.type.startsWith('image/')) {
                                        try {
                                            const blob = new Blob([buffer], { type: att.type });
                                            previewUrl = URL.createObjectURL(blob);
                                        } catch (error) {
                                            console.error('Failed to create preview URL:', error);
                                        }
                                    }
                                    
                                    return {
                                        ...att,
                                        content: buffer,
                                        previewUrl
                                    };
                                }
                                return att;
                            });
                        }
                        
                        return {
                            id: node.id,
                            content: node.text,
                            source:
                                node.source ||
                                (node.id.includes('user') ? 'user' : node.id.includes('ai') || node.id.includes('msg') ? 'ai' : 'system'),
                            parentId: node.parentId,
                            timestamp: Date.now(),
                            costInfo: node.costInfo || null,
                            attachments: attachments || undefined
                        };
                    });

                    console.log('Final processed messages:', messages);
                    
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