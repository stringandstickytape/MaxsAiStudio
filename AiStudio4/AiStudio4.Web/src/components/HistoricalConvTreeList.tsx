// src/components/HistoricalConvTreeList.tsx
import { useState, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { HistoricalConvTree } from './HistoricalConvTree';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useChatManagement } from '@/hooks/useChatManagement';

interface TreeNode {
    id: string;
    text: string;
    children: TreeNode[];
}

export const HistoricalConvTreeList = () => {
    const [expandedConv, setExpandedConv] = useState<string | null>(null);
    const [treeData, setTreeData] = useState<TreeNode | null>(null);

    // Use Zustand stores
    const { clientId } = useWebSocketStore();
    const { createConv, addMessage, setActiveConv, convs: currentConvs } = useConvStore();
    const { 
        convs, 
        isLoading, 
        fetchAllConvs,
        fetchConvTree: fetchTreeData,
        addOrUpdateConv
    } = useHistoricalConvsStore();

    // Fetch all historical convs on component mount
    useEffect(() => {
        fetchAllConvs();
    }, [fetchAllConvs]);

    // Setup WebSocket subscription for new convs
    useEffect(() => {
        // Create a handler for historical conv tree events
        const handleHistoricalConv = (content: any) => {
            if (content) {
                addOrUpdateConv({
                    convGuid: content.convId || content.convGuid,
                    summary: content.summary || content.content || 'Untitled Conv',
                    fileName: `conv_${content.convId || content.convGuid}.json`,
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
                const event = new CustomEvent('check-historical-convs');
                window.dispatchEvent(event);
            }
        );

        // Add an event listener directly to catch historical conv updates
        const handleHistoricalEvent = (e: any) => {
            if (e.detail?.type === 'historicalConvTree') {
                handleHistoricalConv(e.detail.content);
            }
        };

        window.addEventListener('historical-conv', handleHistoricalEvent);

        // Clean up by removing the listener when component unmounts
        return () => {
            unsubscribe();
            window.removeEventListener('historical-conv', handleHistoricalEvent);
        };
    }, [addOrUpdateConv]);

    // Function to fetch conv tree data when expanding a conv
    const handleFetchConvTree = async (convId: string) => {
        const tree = await fetchTreeData(convId);
        setTreeData(tree);
    };

    // Handle node click to load conv by ID
    // Use custom hook for loading conv by ID
    const { getConv } = useChatManagement();

    const handleNodeClick = async (nodeId: string, convId: string) => {
        if (!clientId) return;

        try {
            console.log(`Loading conv ${convId} with selected message ${nodeId}`);

            // First check if conv already exists in store
            const existingConv = currentConvs[convId];

            if (existingConv) {
                console.log('Conv already exists in store, setting as active');
                // If conv already exists, just set it as active with the selected message
                setActiveConv({
                    convId,
                    selectedMessageId: nodeId
                });
                return;
            }

            // Use the hook to get the conv data
            const conv = await getConv(convId);

            if (conv && conv.messages && conv.messages.length > 0) {
                // Sort messages by parent-child relationship
                const sortedMessages = [...conv.messages];

                // Find the root message - either the first with no parent or the first message
                const rootMessage = sortedMessages.find(msg => !msg.parentId) || sortedMessages[0];

                // Create a new conv in the store with the root message
                createConv({
                    id: convId,
                    rootMessage: {
                        id: rootMessage.id,
                        content: rootMessage.content,
                        source: rootMessage.source,
                        parentId: null,
                        timestamp: rootMessage.timestamp,
                        tokenUsage: rootMessage.tokenUsage
                    }
                });

                // Add all non-root messages to the conv
                const nonRootMessages = sortedMessages.filter(msg => msg.id !== rootMessage.id);
                nonRootMessages.forEach(message => {
                    addMessage({
                        convId,
                        message: {
                            id: message.id,
                            content: message.content,
                            source: message.source,
                            parentId: message.parentId,
                            timestamp: message.timestamp,
                            tokenUsage: message.tokenUsage,
                            costInfo: message.costInfo
                        }
                    });
                });

                // Add messageId to URL
                window.history.pushState({}, '', `?messageId=${nodeId}`);

                // Set the active conv with the selected message
                setActiveConv({
                    convId,
                    selectedMessageId: nodeId
                });
            } else {
                console.error('Failed to load conv data or empty conv');
            }
        } catch (error) {
            console.error('Error loading conv:', error);
        }
    };
    return (
        <div className="flex flex-col">
            {isLoading ? (
                <div className="p-4 text-center">
                    Loading convs...
                </div>
            ) : convs.length === 0 ? (
                <div className="p-4 text-center">
                    No convs found
                </div>
            ) : convs.map((conv) => (
                <div
                    key={conv.convGuid}
                    className={`px-4 py-1 transition-all duration-200 relative hover:shadow-lg transform hover:-translate-y-0.5 backdrop-blur-sm max-w-full overflow-hidden ${conv.highlightColour ? 'text-black' : 'text-white'}`}
                >
                    {/* Make only the header clickable */}
                    <div
                        className="flex justify-between items-start cursor-pointer w-full"
                        onClick={() => {
                            const newConvId = expandedConv === conv.convGuid ? null : conv.convGuid;
                            setExpandedConv(newConvId);
                            if (newConvId) handleFetchConvTree(newConvId);
                        }}
                    >
                        <div className="flex-grow flex-1 max-w-[80%]" style={{ minWidth: 0 }}>
                            <div className="text-sm w-full overflow-hidden">
                                <div className="font-medium mb-1 overflow-hidden break-words" style={{ wordWrap: 'break-word', wordBreak: 'break-word' }}>
                                    {conv.summary}
                                </div>
                                <div className="text-xs opacity-70">
                                    {new Date(conv.lastModified).toLocaleDateString()}
                                </div>
                            </div>
                        </div>
                        <div className="text-sm flex-shrink-0 ml-2">
                            {expandedConv === conv.convGuid ? '▼' : '▶'}
                        </div>
                    </div>

                    {/* Tree View */}
                    {
                        expandedConv === conv.convGuid && (
                            <div className="mt-1 pl-2 border-l border-gray-600 transition-all duration-200">
                                {treeData ?
                                    <HistoricalConvTree
                                        treeData={treeData}
                                        onNodeClick={(nodeId) => handleNodeClick(nodeId, expandedConv!)}
                                    /> :
                                    <div className="text-sm text-gray-400">Loading conv...</div>
                                }
                            </div>
                        )
                    }
                </div>
            ))}
        </div>
    );
}