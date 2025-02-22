import { useState, useEffect } from 'react';
import { useWebSocketMessage } from '@/hooks/useWebSocketMessage';

interface CachedConversation {
    convGuid: string;
    summary: string;
    fileName: string;
    lastModified: string;
    highlightColour?: string;
}

interface TreeNode {
    id: string;
    text: string;
    children?: TreeNode[];
}

export const CachedConversationList = () => {
    const [conversations, setConversations] = useState<CachedConversation[]>([]);
    const [expandedConversation, setExpandedConversation] = useState<string | null>(null);

    // State for tree data
    const [treeData, setTreeData] = useState<TreeNode[]>([]);

    // Function to fetch conversation tree data
    const fetchConversationTree = async (convId: string) => {
        try {
            setTreeData([]);
            const response = await fetch('/api/cachedconversation', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    conversationId: convId
                })
            });
            
            const data = await response.json();
            setTreeData(data.treeData || []);
        } catch (error) {
            console.error('Error fetching conversation tree:', error);
            setTreeData([]);
        }
    };

    const handleNewCachedConversation = (conversation: CachedConversation) => {
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

    // Subscribe to cached conversation messages
    useWebSocketMessage('cachedconversation', handleNewCachedConversation);


    return (
        <div className="flex flex-col space-y-2">
            {conversations.map((conversation) => (
                <div
                    key={conversation.convGuid}
                    className={`p-3 rounded transition-all duration-200`}
                    style={{
                        backgroundColor: conversation.highlightColour || '#374151',
                        color: conversation.highlightColour ? '#000' : '#fff'
                    }}
                >
                    {/* Make only the header clickable */}
                    <div
                        className="flex justify-between items-center cursor-pointer"
                        onClick={() => {
                            const newConvId = expandedConversation === conversation.convGuid ? null : conversation.convGuid;
                            setExpandedConversation(newConvId);
                            if (newConvId) fetchConversationTree(newConvId);
                        }}
                    >
                        <div className="flex-grow">
                            <div className="text-sm">
                                <div className="font-medium truncate mb-1">
                                    <span className="text-xs opacity-70 mr-2">
                                        {new Date(conversation.lastModified).toLocaleDateString()}
                                    </span>
                                    {conversation.summary}
                                </div>
                            </div>
                        </div>
                        <div className="text-sm">
                            {expandedConversation === conversation.convGuid ? '▼' : '▶'}
                        </div>
                    </div>

                    {/* Tree View */}
                    {expandedConversation === conversation.convGuid && (
                        <div className="mt-3 pl-4 border-l border-gray-600 transition-all duration-200">
                            {renderTree(treeData)}
                        </div>
                    )}
                </div>
            ))}
        </div>
    );
};

// Helper function to render the tree structure
const renderTree = (node: TreeNode) => {
    return (
        <div key={node.id} className="py-1">
            <div className="flex items-center">
                <div className="w-2 h-2 bg-gray-500 rounded-full mr-2"></div>
                <div className="text-sm text-gray-300 hover:text-white cursor-pointer">
                    {node.text}
                </div>
            </div>
            {node.children && node.children.length > 0 && (
                <div className="pl-4 mt-1">
                    {node.children.map(child => renderTree(child))}
                </div>
            )}
        </div>
    );
};
