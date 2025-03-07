import { useState, useEffect } from 'react';
import { useWebSocket } from '@/hooks/useWebSocket';
import { HistoricalConversationTree } from './HistoricalConversationTree';
import { webSocketService } from '@/services/websocket/WebSocketService';
import { useConversationStore } from '@/stores/useConversationStore';

interface HistoricalConversation {
    convGuid: string;
    summary: string;
    fileName: string;
    lastModified: string;
    highlightColour?: string;
}

interface TreeNode {
    id: string;
    text: string;
    children: TreeNode[];
}

export const HistoricalConversationTreeList = () => {
    const [conversations, setConversations] = useState<HistoricalConversation[]>([]);
    const [expandedConversation, setExpandedConversation] = useState<string | null>(null);
    const [treeData, setTreeData] = useState<TreeNode | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    // Use Zustand store
    const { createConversation, addMessage, setActiveConversation } = useConversationStore();

    const handleNodeClick = async (nodeId: string, conversationId: string) => {
        try {
            // Fetch the full conversation data
            const response = await fetch('/api/getConversation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': webSocketService.getClientId() || ''
                },
                body: JSON.stringify({
                    conversationId: conversationId
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

                    // Create the conversation with the root message using Zustand
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

    const handleNewHistoricalConversation = (conversation: HistoricalConversation) => {
        setConversations(prevConversations => {
            // Check if conversation already exists
            const exists = prevConversations.some(conv => conv.convGuid === conversation.convGuid);
            if (exists) {
                // Update existing conversation
                return prevConversations.map(conv =>
                    conv.convGuid === conversation.convGuid ? conversation : conv
                );
            }
            // Add new conversation at the beginning of the list
            return [conversation, ...prevConversations];
        });
    };

    useWebSocket({
        subscriptions: { 'historicalConversationTree': handleNewHistoricalConversation }
    });

    // Fetch all historical conversations on component mount
    useEffect(() => {
        const fetchAllHistoricalConversations = async () => {
            try {
                setIsLoading(true);
                const response = await fetch('/api/getAllHistoricalConversationTrees', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Client-Id': webSocketService.getClientId() || ''
                    },
                    body: JSON.stringify({})
                });

                if (!response.ok) {
                    throw new Error('Failed to fetch historical conversations');
                }

                const data = await response.json();

                if (data.success && Array.isArray(data.conversations)) {
                    // Process and update state with the received conversations
                    const newConversations = data.conversations.map((conv: any) => ({
                        convGuid: conv.conversationId,
                        summary: conv.summary || 'Untitled Conversation',
                        fileName: `conv_${conv.conversationId}.json`,
                        lastModified: conv.lastModified || new Date().toISOString(),
                        highlightColour: undefined
                    }));

                    setConversations(newConversations);
                }
            } catch (error) {
                console.error('Error fetching historical conversations:', error);
            } finally {
                setIsLoading(false);
            }
        };

        fetchAllHistoricalConversations();
    }, []);

    // Function to fetch conversation tree data
    const fetchConversationTree = async (convId: string) => {
        try {
            setTreeData(null); // Clear previous data
            const response = await fetch('/api/historicalConversationTree', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Client-Id': webSocketService.getClientId() || ''
                },
                body: JSON.stringify({
                    conversationId: convId
                })
            });

            const data = await response.json();

            if (data.success && data.treeData) {
                // Convert flat array to hierarchical tree structure
                const flatNodes = data.treeData;
                const nodeMap = new Map();

                // First pass: create all nodes
                flatNodes.forEach(node => {
                    nodeMap.set(node.id, {
                        id: node.id,
                        text: node.text,
                        children: []
                    });
                });

                // Second pass: build the tree by connecting parents and children
                let rootNode = null;
                flatNodes.forEach(node => {
                    const treeNode = nodeMap.get(node.id);

                    if (!node.parentId) {
                        // This is a root node
                        rootNode = treeNode;
                    } else if (nodeMap.has(node.parentId)) {
                        // Add this node as a child of its parent
                        const parentNode = nodeMap.get(node.parentId);
                        parentNode.children.push(treeNode);
                    }
                });

                // Set the tree data to the root node
                if (rootNode) {
                    setTreeData(rootNode);
                } else if (flatNodes.length > 0) {
                    // If no explicit root found, use the first node
                    setTreeData(nodeMap.get(flatNodes[0].id));
                } else {
                    setTreeData(null);
                }
            } else {
                setTreeData(null);
            }
        } catch (error) {
            console.error('Error fetching conversation tree:', error);
            setTreeData(null);
        }
    };

    return (
        <div className="flex flex-col space-y-2">
            {isLoading ? (
                <div className="p-4 text-center text-gray-400">
                    Loading conversations...
                </div>
            ) : conversations.length === 0 ? (
                <div className="p-4 text-center text-gray-400">
                    No conversations found
                </div>
            ) : conversations.map((conversation) => (
                <div
                    key={conversation.convGuid}
                    className={`p-4 transition-all duration-200 relative hover:shadow-lg transform hover:-translate-y-0.5 backdrop-blur-sm max-w-full overflow-hidden ${conversation.highlightColour ? 'text-black' : 'text-white'}`}
                    style={{
                        backgroundColor: conversation.highlightColour || '#374151'
                    }}
                >
                    {/* Make only the header clickable */}
                    <div
                        className="flex justify-between items-start cursor-pointer w-full"
                        onClick={() => {
                            const newConvId = expandedConversation === conversation.convGuid ? null : conversation.convGuid;
                            setExpandedConversation(newConvId);
                            if (newConvId) fetchConversationTree(newConvId);
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
                            <div className="mt-3 pl-4 border-l border-gray-600 transition-all duration-200">
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
};