import { useState, useEffect } from 'react';
import { useWebSocketMessage } from '@/hooks/useWebSocketMessage';
import { HistoricalConversationTree } from './HistoricalConversationTree';

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

    useWebSocketMessage('historicalConversationTree', handleNewHistoricalConversation);

    // Fetch all historical conversations on component mount
    useEffect(() => {
        const fetchAllHistoricalConversations = async () => {
            try {
                setIsLoading(true);
                const response = await fetch('/api/getAllHistoricalConversationTrees', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
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

    // treeData state already declared above

    // Function to fetch conversation tree data
    const fetchConversationTree = async (convId: string) => {
        try {
            setTreeData([]);
            const response = await fetch('/api/historicalConversationTree', {
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
                                {treeData && <HistoricalConversationTree treeData={treeData} />}
                            </div>
                        )
                    }
                </div>
            ))}
        </div>
    );
};