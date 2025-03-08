// src/components/HistoricalConversationTreeList.tsx
import { useState, useEffect, useCallback } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { HistoricalConversationTree } from './HistoricalConversationTree';
import { useConversationStore } from '@/stores/useConversationStore';
import { useHistoricalConversationsStore } from '@/stores/useHistoricalConversationsStore';
import { useChatManagement } from '@/hooks/useChatManagement';

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
    const { createConversation, addMessage, setActiveConversation, conversations: currentConversations } = useConversationStore();
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
    // Use custom hook for loading conversation by ID
    const { getConversation } = useChatManagement();

    const handleNodeClick = async (nodeId: string, conversationId: string) => {
        if (!clientId) return;

        try {
            console.log(`Loading conversation ${conversationId} with selected message ${nodeId}`);

            // First check if conversation already exists in store
            const existingConversation = currentConversations[conversationId];

            if (existingConversation) {
                console.log('Conversation already exists in store, setting as active');
                // If conversation already exists, just set it as active with the selected message
                setActiveConversation({
                    conversationId,
                    selectedMessageId: nodeId
                });
                return;
            }

            // Use the hook to get the conversation data
            const conversation = await getConversation(conversationId);

            if (conversation && conversation.messages && conversation.messages.length > 0) {
                // Sort messages by parent-child relationship
                const sortedMessages = [...conversation.messages];

                // Find the root message - either the first with no parent or the first message
                const rootMessage = sortedMessages.find(msg => !msg.parentId) || sortedMessages[0];

                // Create a new conversation in the store with the root message
                createConversation({
                    id: conversationId,
                    rootMessage: {
                        id: rootMessage.id,
                        content: rootMessage.content,
                        source: rootMessage.source,
                        parentId: null,
                        timestamp: rootMessage.timestamp
                    }
                });

                // Add all non-root messages to the conversation
                const nonRootMessages = sortedMessages.filter(msg => msg.id !== rootMessage.id);
                nonRootMessages.forEach(message => {
                    addMessage({
                        conversationId,
                        message: {
                            id: message.id,
                            content: message.content,
                            source: message.source,
                            parentId: message.parentId,
                            timestamp: message.timestamp
                        }
                    });
                });

                // Add messageId to URL
                window.history.pushState({}, '', `?messageId=${nodeId}`);

                // Set the active conversation with the selected message
                setActiveConversation({
                    conversationId,
                    selectedMessageId: nodeId
                });
            } else {
                console.error('Failed to load conversation data or empty conversation');
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