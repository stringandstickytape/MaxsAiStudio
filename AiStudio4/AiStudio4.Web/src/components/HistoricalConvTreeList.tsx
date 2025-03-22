import { useState, useEffect } from 'react';
import { processAttachments } from '@/utils/attachmentUtils';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { ChevronDown, ChevronRight, Search, X, Calendar, MessageSquare } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';


export const HistoricalConvTreeList = () => {
    const [searchTerm, setSearchTerm] = useState<string>('');


    const { clientId } = useWebSocketStore();
    const { createConv, addMessage, setActiveConv, convs: currentConvs } = useConvStore();
    const { convs, isLoading, fetchAllConvs, addOrUpdateConv } = useHistoricalConvsStore();


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





    const { getConv } = useChatManagement();

    const handleNodeClick = async (nodeId: string, convId: string) => {
        if (!clientId) return;
        
        // Dispatch an event to notify that a conversation was selected
        window.dispatchEvent(new CustomEvent('historical-conv-selected', { detail: { convId, nodeId } }));

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

                
                const nonRootMessages = sortedMessages.filter((msg) => msg.id !== rootMessage.id);



                nonRootMessages.forEach((message) => {
                    
                    if (message.attachments && message.attachments.length > 0) {
                        message.attachments = processAttachments(message.attachments);
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
                            attachments: message.attachments,
                            durationMs: message.durationMs
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
                
            }
        } catch (error) {
            
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


        <div className="overflow-y-auto">
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
                                    className="rounded-lg overflow-hidden transition-all duration-200 hover:bg-gray-800/40"
                                >

                                    <div
                                        className="flex items-center cursor-pointer p-2 pl-1"
                                        onClick={async () => {
                                            if (conv.convGuid) {
                                                const convData = await getConv(conv.convGuid);
                                                if (convData && convData.messages && convData.messages.length > 0) {
                                                    const sortedMessages = [...convData.messages].sort((a, b) => b.timestamp - a.timestamp);
                                                    
                                                    const lastUserMessage = sortedMessages.find(msg => msg.source === 'user');
                                                    const lastAiMessage = sortedMessages.find(msg => msg.source === 'ai');
                                                    
                                                    const nodeToClick = lastAiMessage ? lastAiMessage.id : 
                                                                        lastUserMessage ? lastUserMessage.id : 
                                                                        sortedMessages[0].id;
                                                    
                                                    handleNodeClick(nodeToClick, conv.convGuid);
                                                }
                                            }
                                        }}
                                    >
                                        <div className="text-gray-400 mr-2">
                                            <MessageSquare size={16} className="text-gray-500" />
                                        </div>

                                        <div className="flex-1 min-w-0">
                                            <div className="font-medium text-sm text-gray-200 break-words">
                                                {conv.summary}
                                            </div>
                                            <div className="text-xs text-gray-400">
                                                {formatDate(conv.lastModified)}
                                            </div>
                                        </div>
                                    </div>


                                    {/* We no longer show the expanded conversation tree here */}
                                </div>
                            ))}
                        </div>
                    </ScrollArea>
                )}
            </div>
        </div>
    );
};