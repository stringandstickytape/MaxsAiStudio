// src/components/HistoricalConversationTreeList.tsx
import { useState, useEffect, useCallback } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { HistoricalConversationTree } from './HistoricalConversationTree';
import { useConversationStore } from '@/stores/useConversationStore';
import { useHistoricalConversationsStore } from '@/stores/useHistoricalConversationsStore';

interface TreeNode {
    id: string;
    text: string;
    children: TreeNode[];
}

export const HistoricalConversationTreeList = () => {
    const [expandedConversation, setExpandedConversation] = useState<string | null>(null);
    const [treeData, setTreeData] = useState<TreeNode | null>(null);

    // Use Zustand stores
    const { clientId } = useWebSocketStore();
    const { createConversation, addMessage, setActiveConversation } = useConversationStore();
    const { 
        conversations, 
        isLoading, 
        fetchAllConversations,
        fetchConversationTree: fetchTreeData,
        addOrUpdateConversation
    } = useHistoricalConversationsStore();

    // Fetch all historical conversations on component mount
    useEffect(() => {
        fetchAllConversations();
    }, [fetchAllConversations]);

    // Setup WebSocket subscription for new conversations
    useEffect(() => {
        // Create a handler for historical conversation tree events
        const handleHistoricalConversation = (content: any) => {
            if (content) {
                addOrUpdateConversation({
                    convGuid: content.conversationId || content.convGuid,
                    summary: content.summary || content.content || 'Untitled Conversation',
                    fileName: `conv_${content.conversationId || content.convGuid}.json`,
                    lastModified: content.lastModified || new Date().toISOString(),
                    highlightColour: content.highlightColour
                });
            }
        };

        // Add a WebSocket event listener
        const unsubscribe = useWebSocketStore.subscribe(
            state => state.lastMessageTime,
            async () => {
                // This will be called whenever a new WebSocket message is received
                const event = new CustomEvent('check-historical-conversations');
                window.dispatchEvent(event);
            }
        );

        // Add an event listener directly to catch historical conversation updates
        const handleHistoricalEvent = (e: any) => {
            if (e.detail?.type === 'historicalConversationTree') {
                handleHistoricalConversation(e.detail.content);
            }
        };

        window.addEventListener('historical-conversation', handleHistoricalEvent);

        // Clean up by removing the listener when component unmounts
        return () => {
            unsubscribe();
            window.removeEventListener('historical-conversation', handleHistoricalEvent);
        };
    }, [addOrUpdateConversation]);

    // Function to fetch conversation tree data when expanding a conversation
    const handleFetchConversationTree = async (convId: string) => {
        const tree = await fetchTreeData(convId);
        setTreeData(tree);
    };

    // Handle node click to load conversation by ID
    const handleNodeClick = async (nodeId: string, conversationId: string) => {
        if (!clientId) return;
        
        try {
            // Fetch the full conversation data
            const response = await fetch('/api/getConversation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': clientId
                },
                body: JSON.stringify({
                    conversationId
                })
            });

            if (!response.ok) {
                throw new Error('Failed to fetch conversation');
            }

            const data = await response.json();

            if (data.success && data.conversation && data.conversation.messages) {
                // First create the conversation in the store
                const messages = data.conversation.messages;
                if (messages.length > 0) {
                    // Find the root message
                    const rootMessage = messages.find(msg => !msg.parentId) || messages[0];

                    // Create the conversation with the root message
                    createConversation({
                        id: conversationId,
                        rootMessage: {
                            id: rootMessage.id,
                            content: rootMessage.content,
                            source: rootMessage.source,
                            parentId: null,
                            timestamp: rootMessage.timestamp || Date.now()
                        }
                    });

                    // Add the rest of the messages
                    const nonRootMessages = messages.filter(msg => msg.id !== rootMessage.id);
                    for (const message of nonRootMessages) {
                        addMessage({
                            conversationId,
                            message: {
                                id: message.id,
                                content: message.content,
                                source: message.source,
                                parentId: message.parentId,
                                timestamp: message.timestamp || Date.now()
                            }
                        });
                    }

                    // Set the active conversation and selected message
                    setActiveConversation({
                        conversationId,
                        selectedMessageId: nodeId
                    });
                }
            }
        } catch (error) {
            console.error('Error loading conversation:', error);
        }
    };

    return (
        <div className="flex flex-col">
            {isLoading ? (
                <div className="p-4 text-center">
                    Loading conversations...
                </div>
            ) : conversations.length === 0 ? (
                <div className="p-4 text-center">
                    No conversations found
                </div>
            ) : conversations.map((conversation) => (
                <div
                    key={conversation.convGuid}
                    className={`px-4 py-1 transition-all duration-200 relative hover:shadow-lg transform hover:-translate-y-0.5 backdrop-blur-sm max-w-full overflow-hidden ${conversation.highlightColour ? 'text-black' : 'text-white'}`}
                >
                    {/* Make only the header clickable */}
                    <div
                        className="flex justify-between items-start cursor-pointer w-full"
                        onClick={() => {
                            const newConvId = expandedConversation === conversation.convGuid ? null : conversation.convGuid;
                            setExpandedConversation(newConvId);
                            if (newConvId) handleFetchConversationTree(newConvId);
                        }}
                    >
                        <div className="flex-grow flex-1 max-w-[80%]" style={{ minWidth: 0 }}>
                            <div className="text-sm w-full overflow-hidden">
                                <div className="font-medium mb-1 overflow-hidden break-words" style={{ wordWrap: 'break-word', wordBreak: 'break-word' }}>
                                    {conversation.summary}
                                </div>
                                <div className="text-xs opacity-70">
                                    {new Date(conversation.lastModified).toLocaleDateString()}
                                </div>
                            </div>
                        </div>
                        <div className="text-sm flex-shrink-0 ml-2">
                            {expandedConversation === conversation.convGuid ? '▼' : '▶'}
                        </div>
                    </div>

                    {/* Tree View */}
                    {
                        expandedConversation === conversation.convGuid && (
                            <div className="mt-1 pl-2 border-l border-gray-600 transition-all duration-200">
                                {treeData ?
                                    <HistoricalConversationTree
                                        treeData={treeData}
                                        onNodeClick={(nodeId) => handleNodeClick(nodeId, expandedConversation!)}
                                    /> :
                                    <div className="text-sm text-gray-400">Loading conversation...</div>
                                }
                            </div>
                        )
                    }
                </div>
            ))}
        </div>
    );
}