import { useCallback } from 'react';
import { useApiCallState, createApiRequest } from '@/utils/apiUtils';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { v4 as uuidv4 } from 'uuid';
import { createResourceHook } from './useResourceFactory';
import { prepareAttachmentsForTransmission, isTextFile } from '@/utils/attachmentUtils';

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

                
                let requestParams = { ...params };

                if (params.attachments && params.attachments.length > 0) {
                    
                    
                    const binaryAttachments = params.attachments.filter(att => !att.textContent && !isTextFile(att.type));

                    
                    requestParams.attachments = prepareAttachmentsForTransmission(binaryAttachments);
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
                    
                    // Log detailed timing information from the tree data
                    console.log('Timing information in tree root:', {
                        id: treeData.id,
                        timestamp: treeData.timestamp,
                        timestampType: typeof treeData.timestamp,
                        durationMs: treeData.durationMs,
                        durationMsType: typeof treeData.durationMs
                    });
                    
                    if (treeData.children && treeData.children.length > 0) {
                        console.log('Timing information in first child:', {
                            id: treeData.children[0].id,
                            timestamp: treeData.children[0].timestamp,
                            timestampType: typeof treeData.children[0].timestamp,
                            durationMs: treeData.children[0].durationMs,
                            durationMsType: typeof treeData.children[0].durationMs
                        });
                    }
                    
                    console.log('Looking at attachments in tree data nodes:', treeData.children?.map(c => ({ id: c.id, hasAttachments: !!c.attachments, attachments: c.attachments })));



                    const extractNodes = (node: any, nodes: any[] = []) => {
                        if (!node) return nodes;

                        // Debug timing info on every node
                        console.log(`Node ${node.id} timing info:`, {
                            timestamp: node.timestamp,
                            timestampType: typeof node.timestamp,
                            durationMs: node.durationMs,
                            durationMsType: typeof node.durationMs,
                            durationMsJSON: JSON.stringify(node.durationMs)
                        });
                        
                        if (node.attachments) {
                            console.log(`Node ${node.id} has attachments:`, node.attachments);
                        }

                        // Explicitly extract each property to ensure nothing is lost
                        const extractedNode = {
                            id: node.id,
                            text: node.text,
                            parentId: node.parentId,
                            source: node.source,
                            costInfo: node.costInfo,
                            attachments: node.attachments,
                            timestamp: node.timestamp,
                            // For durationMs, convert to number if it's a string, or keep as is
                            durationMs: typeof node.durationMs === 'string' ? 
                                Number(node.durationMs) : node.durationMs
                        };
                        
                        // Add the node to the array
                        nodes.push(extractedNode);
                        
                        // Verify the durationMs was properly retained
                        console.log(`Extracted node ${extractedNode.id} durationMs: ${extractedNode.durationMs} (${typeof extractedNode.durationMs})`);

                        if (node.children && Array.isArray(node.children)) {
                            for (const child of node.children) {
                                extractNodes(child, nodes);
                            }
                        }

                        return nodes;
                    };

                    const flatNodes = extractNodes(treeData);
                    
                    
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
                        
                        let attachments = node.attachments;
                        console.log('Node attachments:', attachments);
                        if (attachments && Array.isArray(attachments)) {
                            attachments = attachments.map(att => {
                                if (typeof att.content === 'string') {
                                    
                                    const binaryString = window.atob(att.content);
                                    const bytes = new Uint8Array(binaryString.length);
                                    for (let i = 0; i < binaryString.length; i++) {
                                        bytes[i] = binaryString.charCodeAt(i);
                                    }
                                    const buffer = bytes.buffer;
                                    
                                    
                                    let previewUrl;
                                    if (att.type.startsWith('image/') || att.type === 'application/pdf') {
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
                            timestamp: typeof node.timestamp === 'number' ? node.timestamp : Date.now(),
                            durationMs: typeof node.durationMs === 'number' ? node.durationMs : undefined,
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