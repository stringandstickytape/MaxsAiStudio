import { useState, useEffect, useMemo, useCallback, memo } from 'react';
import { processAttachments } from '@/utils/attachmentUtils';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { useMessageSelection } from '@/hooks/useMessageSelection';
import { Search, X, MessageSquare } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';

// Memoized individual conversation item component
const ConversationItem = memo(({ 
    conv, 
    searchResult, 
    onMiddleClick, 
    onClick 
}: {
    conv: any;
    searchResult?: { matchingMessageIds: string[] } | undefined;
    onMiddleClick: (e: React.MouseEvent, convId: string) => void;
    onClick: () => void;
}) => {
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
            onMouseDown={(e) => onMiddleClick(e, conv.convGuid)}
            onClick={onClick}
        >
            {conv.summary}
            {searchResult && (
                <div className="text-xs text-blue-400 mt-1">
                    {searchResult.matchingMessageIds.length} matching message{searchResult.matchingMessageIds.length !== 1 ? 's' : ''}
                </div>
            )}
        </div>
    );
}, (prevProps, nextProps) => {
    // Custom comparison for conversation items
    return (
        prevProps.conv.convGuid === nextProps.conv.convGuid &&
        prevProps.conv.summary === nextProps.conv.summary &&
        prevProps.conv.lastModified === nextProps.conv.lastModified &&
        JSON.stringify(prevProps.searchResult) === JSON.stringify(nextProps.searchResult)
    );
});


interface HistoricalConvTreeListProps {
    searchResults?: {
        conversationId: string;
        matchingMessageIds: string[];
        summary: string;
        lastModified: string;
    }[] | null;
}

const HistoricalConvTreeListComponent = ({ searchResults }: HistoricalConvTreeListProps) => {
    const [searchTerm, setSearchTerm] = useState<string>('');


    // Optimize store subscriptions to reduce re-renders during livestream events
    const clientId = useWebSocketStore(state => state.clientId);
    const currentRequest = useWebSocketStore(state => state.currentRequest);
    const { createConv, addMessage, convs: currentConvs } = useConvStore();
    const { selectMessage } = useMessageSelection();
    const { convs, fetchAllConvs, addOrUpdateConv, deleteConv, isLoadingList } = useHistoricalConvsStore();
    const { highlightMessage } = useSearchStore();

    const isChatRequestOngoing = !!currentRequest;

    useEffect(() => {
        fetchAllConvs();
    }, [fetchAllConvs]);

    // Memoize the historical conversation handler to prevent recreation
    const handleHistoricalConv = useCallback((content: any) => {
        if (content) {
            addOrUpdateConv({
                convGuid: content.convId || content.convGuid,
                summary: content.summary || content.content || 'Untitled Conv',
                fileName: `conv_${content.convId || content.convGuid}.json`,
                lastModified: content.lastModified || new Date().toISOString(),
                highlightColour: content.highlightColour,
            });
        }
    }, [addOrUpdateConv]);

    // Memoize the historical event handler
    const handleHistoricalEvent = useCallback((e: any) => {
        if (e.detail?.type === 'historicalConvTree') {
            handleHistoricalConv(e.detail.content);
        }
    }, [handleHistoricalConv]);

    useEffect(() => {
        const unsubscribe = useWebSocketStore.subscribe(
            (state) => state.lastMessageTime,
            async () => {
                const event = new CustomEvent('check-historical-convs');
                window.dispatchEvent(event);
            },
        );

        window.addEventListener('historical-conv', handleHistoricalEvent);

        return () => {
            unsubscribe();
            window.removeEventListener('historical-conv', handleHistoricalEvent);
        };
    }, [handleHistoricalEvent]);





    const { getConv } = useChatManagement();

    const handleNodeClick = useCallback(async (nodeId: string, convId: string) => {
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
                    
                    selectMessage(selectedMessage.parentId, convId);
                } else {
                    
                    selectMessage(nodeId, convId);
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
                    
                    selectMessage(selectedMessage.parentId, convId);
                } else {
                    
                    selectMessage(nodeId, convId);
                }
            } else {
                
            }
        } catch (error) {
            
        }
    }, [clientId, searchResults, highlightMessage, currentConvs, selectMessage, getConv, createConv, addMessage]);

    // Filter conversations based on search results if available - memoized for performance
    const displayedConvs = useMemo(() => {
        return searchResults
            ? convs.filter(conv => 
                searchResults.some(result => result.conversationId === conv.convGuid))
            : convs.filter(conv =>
                searchTerm ? conv.summary.toLowerCase().includes(searchTerm.toLowerCase()) : true
            );
    }, [searchResults, convs, searchTerm]);

    // Handle middle-click to delete conversation
    const handleMiddleClick = useCallback(async (event: React.MouseEvent, convId: string) => {
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
    }, [deleteConv]);

    // Memoize the conversation click handler to avoid recreating it for each item
    const handleConversationClick = useCallback(async (conv: any) => {
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
    }, [getConv, handleNodeClick]);

    // Memoize the entire conversation list rendering to prevent recreation during livestream events
    const conversationItems = useMemo(() => {
        return displayedConvs.map((conv) => {
            // Find if this conv has search results
            const searchResult = searchResults?.find(r => r.conversationId === conv.convGuid);
            
            return (
                <ConversationItem
                    key={conv.convGuid}
                    conv={conv}
                    searchResult={searchResult}
                    onMiddleClick={handleMiddleClick}
                    onClick={() => handleConversationClick(conv)}
                />
            );
        });
    }, [displayedConvs, searchResults, handleMiddleClick, handleConversationClick]);

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
                {isLoadingList ? (
                    <div className="flex-center h-32">
                        <div className="loading-spinner h-8 w-8"></div>
                    </div>
                ) : displayedConvs.length === 0 ? (
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
                            {conversationItems}
                        </div>
                    </ScrollArea>
                )}
            </div>
        </div>
    );
};

// Memoize the component to prevent unnecessary re-renders
export const HistoricalConvTreeList = memo(HistoricalConvTreeListComponent, (prevProps, nextProps) => {
    // Custom comparison function to optimize re-renders
    // Only re-render if searchResults actually changed (deep comparison for the array)
    if (prevProps.searchResults === nextProps.searchResults) {
        return true; // props are equal, don't re-render
    }
    
    // If one is null/undefined and the other isn't, they're different
    if (!prevProps.searchResults !== !nextProps.searchResults) {
        return false; // props are different, re-render
    }
    
    // If both are null/undefined, they're equal
    if (!prevProps.searchResults && !nextProps.searchResults) {
        return true; // props are equal, don't re-render
    }
    
    // Both are arrays, do a shallow comparison
    if (prevProps.searchResults!.length !== nextProps.searchResults!.length) {
        return false; // different lengths, re-render
    }
    
    // Check if each search result is the same
    for (let i = 0; i < prevProps.searchResults!.length; i++) {
        const prev = prevProps.searchResults![i];
        const next = nextProps.searchResults![i];
        
        if (prev.conversationId !== next.conversationId ||
            prev.summary !== next.summary ||
            prev.lastModified !== next.lastModified ||
            prev.matchingMessageIds.length !== next.matchingMessageIds.length) {
            return false; // different, re-render
        }
        
        // Check matching message IDs
        for (let j = 0; j < prev.matchingMessageIds.length; j++) {
            if (prev.matchingMessageIds[j] !== next.matchingMessageIds[j]) {
                return false; // different, re-render
            }
        }
    }
    
    return true; // all equal, don't re-render
});

// Export themeable properties for ThemeManager
export const themeableProps = {
}