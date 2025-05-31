import { useState, useEffect } from 'react';
import { processAttachments } from '@/utils/attachmentUtils';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { Search, X, MessageSquare } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';


interface HistoricalConvTreeListProps {
    searchResults?: {
        conversationId: string;
        matchingMessageIds: string[];
        summary: string;
        lastModified: string;
    }[] | null;
}

export const HistoricalConvTreeList = ({ searchResults }: HistoricalConvTreeListProps) => {
    const [searchTerm, setSearchTerm] = useState<string>('');


    const { clientId } = useWebSocketStore();
    const { createConv, addMessage, setActiveConv, convs: currentConvs } = useConvStore();
    const { convs, fetchAllConvs, addOrUpdateConv, deleteConv } = useHistoricalConvsStore();
    const { highlightMessage } = useSearchStore();

    // Use currentRequest from useWebSocketStore for correct chat request lifecycle
    const { currentRequest } = useWebSocketStore();
    const isChatRequestOngoing = !!currentRequest;

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

        // Find if this conv has search results
        const searchResult = searchResults?.find(r => r.conversationId === convId);
        
        // If there are matching messages, highlight the first one and reset index
        if (searchResult?.matchingMessageIds?.length) {
            highlightMessage(searchResult.matchingMessageIds[0]);
            // Reset the current match index to 0 when selecting a conversation
            useSearchStore.getState().currentMatchIndex = 0;
        } else {
            // Otherwise clear any highlighted message
            highlightMessage(null);
        }

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
                            cumulativeCost: message.cumulativeCost,
                            attachments: message.attachments,
                            temperature: message.temperature,
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

    // Filter conversations based on search results if available
    const displayedConvs = searchResults
        ? convs.filter(conv => 
            searchResults.some(result => result.conversationId === conv.convGuid))
        : convs.filter(conv =>
            searchTerm ? conv.summary.toLowerCase().includes(searchTerm.toLowerCase()) : true
        );

    // Handle middle-click to delete conversation
    const handleMiddleClick = async (event: React.MouseEvent, convId: string) => {
        // Middle mouse button is button 1
        if (event.button === 1) {
            event.preventDefault();
            event.stopPropagation();
            
            // Show confirmation dialog
            if (window.confirm('Delete this conversation?')) {
                try {
                    // Delete conversation using the store function
                    await deleteConv(convId);
                } catch (e) {
                    console.error('Failed to delete conversation:', e);
                }
            }
        }
    };

    // Removed formatDate function as it's no longer needed

    return (
        <div className="HistoricalConvTreeList flex flex-col h-full" 
            style={{
                backgroundColor: 'var(--global-background-color, transparent)',
                color: 'var(--global-text-color, #e5e7eb)',
                borderColor: 'var(--global-border-color, #374151)',
                fontFamily: 'var(--global-font-family, inherit)',
                fontSize: 'var(--global-font-size, inherit)',
                ...(window?.theme?.HistoricalConvTreeList?.style || {})
                //,
                //...(isChatRequestOngoing && { pointerEvents: 'none', opacity: 0.5 })
            }}
        >
            {/* Search bar removed - now using the one in Sidebar */}

            {/* Conversation list with scrolling */}
            <div className="HistoricalConvTreeList flex-1 overflow-y-auto mt-2"
                style={{
                    ...(window?.theme?.HistoricalConvTreeList?.style || {})
                }}
            >
                {displayedConvs.length === 0 ? (
                    <div className="HistoricalConvTreeList p-4 text-center flex flex-col items-center"
                        style={{
                            color: 'var(--global-text-color, #9ca3af)',
                            ...(window?.theme?.HistoricalConvTreeList?.style || {})
                        }}
                    >
                        {searchTerm ? (
                            <>
                                <MessageSquare size={24} className="HistoricalConvTreeList mb-2" 
                                    style={{
                                        color: 'var(--global-text-color, #6b7280)',
                                        ...(window?.theme?.HistoricalConvTreeList?.style || {})
                                    }}
                                />
                                <span>No conversations matching "{searchTerm}"</span>
                            </>
                        ) : (
                            <>
                                <MessageSquare size={24} className="HistoricalConvTreeList mb-2" 
                                    style={{
                                        color: 'var(--global-text-color, #6b7280)',
                                        ...(window?.theme?.HistoricalConvTreeList?.style || {})
                                    }}
                                />
                                <span>No conversations found</span>
                            </>
                        )}
                    </div>
                ) : (
                    <ScrollArea className="HistoricalConvTreeList h-full pr-1">
                        <div className="HistoricalConvTreeList px-1" style={{ display: 'block', minWidth: '100%' }}>
                            {displayedConvs.map((conv) => {
                                    // Find if this conv has search results
                                    const searchResult = searchResults?.find(r => r.conversationId === conv.convGuid);
                                    
                                    return (
                                <div
                                    key={conv.convGuid}
                                    className="HistoricalConvTreeList text-sm cursor-pointer px-2 py-0.5 rounded overflow-hidden text-ellipsis whitespace-normal break-words mb-1"
                                    title="Middle-click to delete conversation"
                                    style={{ 
                                        display: 'block', 
                                        wordBreak: 'break-word',
                                        color: 'var(--global-text-color, #e5e7eb)',
                                        backgroundColor: 'var(--global-background-color, transparent)',
                                        border: 'none',
                                        ':hover': {
                                            backgroundColor: 'var(--global-primary-color, rgba(31, 41, 55, 0.4))',
                                            opacity: 0.7
                                        },
                                        ...(window?.theme?.HistoricalConvTreeList?.style || {})
                                    }}
                                    onMouseDown={(e) => handleMiddleClick(e, conv.convGuid)}
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
                                    {conv.summary}
                                    {searchResult && (
                                        <div className="text-xs text-blue-400 mt-1">
                                            {searchResult.matchingMessageIds.length} matching message{searchResult.matchingMessageIds.length !== 1 ? 's' : ''}
                                        </div>
                                    )}
                                </div>
                                );
                            })}
                        </div>
                    </ScrollArea>
                )}
            </div>
        </div>
    );
};

// Export themeable properties for ThemeManager
export const themeableProps = {
}