import { useState, useEffect } from 'react';
import { processAttachments } from '@/utils/bufferUtils';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { HistoricalConvTree } from './HistoricalConvTree';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { ChevronDown, ChevronRight, Search, X, Calendar, MessageSquare } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';

interface TreeNode {
    id: string;
    text: string;
    children: TreeNode[];
}

export const HistoricalConvTreeList = () => {
    const [expandedConv, setExpandedConv] = useState<string | null>(null);
    const [treeData, setTreeData] = useState<TreeNode | null>(null);
    const [searchTerm, setSearchTerm] = useState<string>('');
    const [isLoadingTree, setIsLoadingTree] = useState<boolean>(false);


    const { clientId } = useWebSocketStore();
    const { createConv, addMessage, setActiveConv, convs: currentConvs } = useConvStore();
    const { convs, isLoading, fetchAllConvs, fetchConvTree: fetchTreeData, addOrUpdateConv } = useHistoricalConvsStore();


    useEffect(() => {
        fetchAllConvs();
    }, [fetchAllConvs]);


    useEffect(() => {

        const handleHistoricalConv = (content: any) => {
            if (content) {
                addOrUpdateConv({
                    convGuid: content.convId || content.convGuid,
                    summary: content.summary || content.content || 'Untitled Conv',
                    fileName: `conv_${content.convId || content.convGuid}.json`,
                    lastModified: content.lastModified || new Date().toISOString(),
                    highlightColour: content.highlightColour,
                });
            }
        };


        const unsubscribe = useWebSocketStore.subscribe(
            (state) => state.lastMessageTime,
            async () => {

                const event = new CustomEvent('check-historical-convs');
                window.dispatchEvent(event);
            },
        );


        const handleHistoricalEvent = (e: any) => {
            if (e.detail?.type === 'historicalConvTree') {
                handleHistoricalConv(e.detail.content);
            }
        };

        window.addEventListener('historical-conv', handleHistoricalEvent);


        return () => {
            unsubscribe();
            window.removeEventListener('historical-conv', handleHistoricalEvent);
        };
    }, [addOrUpdateConv]);


    const handleFetchConvTree = async (convId: string) => {
        setIsLoadingTree(true);
        try {
            const tree = await fetchTreeData(convId);


            if (tree) {


                if (tree.text === 'Conv Root' || tree.text.includes('system') || tree.text.includes('System')) {
                    tree.text = 'Conversation Root';
                }
            }

            setTreeData(tree);
        } catch (error) {
            console.error('Error fetching conversation tree:', error);
        } finally {
            setIsLoadingTree(false);
        }
    };



    const { getConv } = useChatManagement();

    const handleNodeClick = async (nodeId: string, convId: string) => {
        if (!clientId) return;

        try {


            const existingConv = currentConvs[convId];

            if (existingConv) {

                const selectedMessage = existingConv.messages.find(msg => msg.id === nodeId);


                if (selectedMessage && selectedMessage.source === 'user' && selectedMessage.parentId) {


                    setActiveConv({
                        convId,
                        slctdMsgId: selectedMessage.parentId,
                    });
                } else {

                    setActiveConv({
                        convId,
                        slctdMsgId: nodeId,
                    });
                }
                return;
            }

            const conv = await getConv(convId);

            if (conv && conv.messages && conv.messages.length > 0) {

                const sortedMessages = [...conv.messages];


                const rootMessage = sortedMessages.find((msg) => !msg.parentId) || sortedMessages[0];


                createConv({
                    id: convId,
                    rootMessage: {
                        id: rootMessage.id,
                        content: rootMessage.content,
                        source: rootMessage.source,
                        parentId: null,
                        timestamp: rootMessage.timestamp,
                    },
                });

                // Add all non-root messages
                const nonRootMessages = sortedMessages.filter((msg) => msg.id !== rootMessage.id);



                nonRootMessages.forEach((message) => {
                    // Process any attachments in the message
                    if (message.attachments && message.attachments.length > 0) {
                        message.attachments = message.attachments.map(att => {
                            // Convert base64 to ArrayBuffer if needed
                            if (typeof att.content === 'string') {
                                try {
                                    const binaryString = window.atob(att.content);
                                    const bytes = new Uint8Array(binaryString.length);
                                    for (let i = 0; i < binaryString.length; i++) {
                                        bytes[i] = binaryString.charCodeAt(i);
                                    }

                                    // Create preview URL for images
                                    let previewUrl;
                                    if (att.type.startsWith('image/')) {
                                        const blob = new Blob([bytes.buffer], { type: att.type });
                                        previewUrl = URL.createObjectURL(blob);
                                    }

                                    return {
                                        ...att,
                                        content: bytes.buffer,
                                        previewUrl
                                    };
                                } catch (error) {
                                    return att;
                                }
                            }
                            return att;
                        });
                    }

                    addMessage({
                        convId,
                        message: {
                            id: message.id,
                            content: message.content,
                            source: message.source,
                            parentId: message.parentId,
                            timestamp: message.timestamp,
                            costInfo: message.costInfo,
                            attachments: message.attachments
                        },
                    });
                });


                const selectedMessage = conv.messages.find(msg => msg.id === nodeId);


                if (selectedMessage && selectedMessage.source === 'user' && selectedMessage.parentId) {

                    window.history.pushState({}, '', `?messageId=${selectedMessage.parentId}`);


                    setActiveConv({
                        convId,
                        slctdMsgId: selectedMessage.parentId,
                    });
                } else {

                    window.history.pushState({}, '', `?messageId=${nodeId}`);


                    setActiveConv({
                        convId,
                        slctdMsgId: nodeId,
                    });
                }
            } else {
                // Failed to load conv data or empty conv
            }
        } catch (error) {
            // Error handling for loading conversation
        }
    };

    const filteredConvs = convs.filter(conv =>
        searchTerm ? conv.summary.toLowerCase().includes(searchTerm.toLowerCase()) : true
    );


    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        return new Intl.DateTimeFormat('en-GB', {
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        }).format(date);
    };

    return (
        <div className="flex flex-col h-full space-y-2">

            <div className="px-3 pt-2 pb-1 sticky top-0 z-10 bg-gray-900/90 backdrop-blur-sm">
                <div className="relative">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <Search size={16} className="text-gray-400" />
                    </div>
                    <input
                        type="text"
                        placeholder="Search conversations..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="w-full py-1.5 pl-10 pr-8 text-sm rounded-md border border-gray-700 bg-gray-800/80 
                      text-gray-200 placeholder-gray-500 focus:outline-none focus:ring-1 focus:ring-blue-500 focus:border-blue-500"
                    />
                    {searchTerm && (
                        <button
                            onClick={() => setSearchTerm('')}
                            className="absolute inset-y-0 right-2 flex items-center text-gray-400 hover:text-gray-200"
                        >
                            <X size={14} />
                        </button>
                    )}
                </div>
            </div>


            <div className="flex-1 overflow-y-auto">
                {isLoading ? (
                    <div className="p-4 text-center text-gray-400 flex flex-col items-center">
                        <div className="animate-spin rounded-full h-6 w-6 border-t-2 border-b-2 border-blue-500 mb-2"></div>
                        <span>Loading conversations...</span>
                    </div>
                ) : filteredConvs.length === 0 ? (
                    <div className="p-4 text-center text-gray-400 flex flex-col items-center">
                        {searchTerm ? (
                            <>
                                <MessageSquare size={24} className="mb-2 text-gray-500" />
                                <span>No conversations matching "{searchTerm}"</span>
                            </>
                        ) : (
                            <>
                                <MessageSquare size={24} className="mb-2 text-gray-500" />
                                <span>No conversations found</span>
                            </>
                        )}
                    </div>
                ) : (
                    <ScrollArea className="h-full pr-1">
                        <div className="space-y-1 px-1">
                            {filteredConvs.map((conv) => (
                                <div
                                    key={conv.convGuid}
                                    className={`rounded-lg overflow-hidden transition-all duration-200 ${expandedConv === conv.convGuid ? 'bg-gray-800/60' : 'hover:bg-gray-800/40'}`}
                                >

                                    <div
                                        className="flex items-center cursor-pointer"
                                        onClick={() => {
                                            const newConvId = expandedConv === conv.convGuid ? null : conv.convGuid;
                                            setExpandedConv(newConvId);
                                            if (newConvId) handleFetchConvTree(newConvId);
                                        }}
                                    >
                                        <div className="mr-2 text-gray-400">
                                            {expandedConv === conv.convGuid ?
                                                <ChevronDown size={18} className="text-blue-400" /> :
                                                <ChevronRight size={18} />}
                                        </div>

                                        <div className="flex-1 min-w-0">
                                            <div className="font-medium text-sm text-gray-200 break-words">
                                                {conv.summary}
                                            </div>
                                        </div>
                                    </div>


                                    {expandedConv === conv.convGuid && (
                                        <div className="transition-all duration-300 overflow-hidden">
                                            <div className="border-t border-gray-700/50 mx-2"></div>
                                            <div className="py-1 px-1">
                                                {isLoadingTree ? (
                                                    <div className="flex items-center justify-center py-4 text-gray-400 text-sm">
                                                        <div className="animate-spin rounded-full h-4 w-4 border-t-2 border-b-2 border-blue-500 mr-2"></div>
                                                        Loading messages...
                                                    </div>
                                                ) : treeData ? (
                                                    <HistoricalConvTree
                                                        treeData={treeData}
                                                        onNodeClick={(nodeId) => handleNodeClick(nodeId, expandedConv!)}
                                                    />
                                                ) : (
                                                    <div className="text-sm text-gray-400 p-2 text-center">No messages found</div>
                                                )}
                                            </div>
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </ScrollArea>
                )}
            </div>
        </div>
    );
};