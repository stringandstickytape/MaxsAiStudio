import { useState, useEffect } from 'react';
import { processAttachments } from '@/utils/attachmentUtils';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';
import { useHistoricalConvsStore } from '@/stores/useHistoricalConvsStore';
import { useChatManagement } from '@/hooks/useChatManagement';
import { Search, X, MessageSquare } from 'lucide-react';
import { ScrollArea } from '@/components/ui/scroll-area';


export const HistoricalConvTreeList = () => {
    const [searchTerm, setSearchTerm] = useState<string>('');


    const { clientId } = useWebSocketStore();
    const { createConv, addMessage, setActiveConv, convs: currentConvs } = useConvStore();
    const { convs, isLoading, fetchAllConvs, addOrUpdateConv, deleteConv } = useHistoricalConvsStore();


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
                backgroundColor: 'var(--historylist-bg, var(--global-background-color, transparent))',
                color: 'var(--historylist-text-color, var(--global-text-color, #e5e7eb))',
                borderColor: 'var(--historylist-border-color, var(--global-border-color, #374151))',
                fontFamily: 'var(--historylist-font-family, var(--global-font-family, inherit))',
                fontSize: 'var(--historylist-font-size, var(--global-font-size, inherit))',
                ...(window?.theme?.HistoricalConvTreeList?.style || {})
            }}
        >
            {/* Search bar at the top */}
            <div className="HistoricalConvTreeList px-3 pt-2 pb-1 backdrop-blur-sm"
                style={{
                    backgroundColor: 'var(--historylist-search-bg, var(--global-background-color, rgba(17, 24, 39, 0.9)))',
                    backdropFilter: 'blur(4px)',
                    borderColor: 'var(--historylist-border-color, var(--global-border-color, #374151))',
                    ...(window?.theme?.HistoricalConvTreeList?.style || {})
                }}
            >
                <div className="relative">
                    <div className="flex items-center">
                        <Search size={16} className="HistoricalConvTreeList absolute left-3" 
                            style={{
                                color: 'var(--historylist-text-color, #9ca3af)',
                                ...(window?.theme?.HistoricalConvTreeList?.style || {})
                            }}
                        />
                        <input
                            type="text"
                            placeholder="Search conversations..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="HistoricalConvTreeList w-full py-1.5 pl-10 pr-8 text-sm rounded-md border"
                            style={{
                                backgroundColor: 'var(--historylist-bg, var(--global-background-color, rgba(31, 41, 55, 0.8)))',
                                borderColor: 'var(--historylist-border-color, var(--global-border-color, #374151))',
                                color: 'var(--historylist-text-color, var(--global-text-color, #e5e7eb))',
                                caretColor: 'var(--historylist-accent-color, var(--global-primary-color, #60a5fa))',
                                '::placeholder': {
                                    color: 'var(--historylist-text-color, var(--global-text-color, #6b7280))'
                                },
                                ':focus': {
                                    outline: 'none',
                                    borderColor: 'var(--historylist-accent-color, var(--global-primary-color, #3b82f6))',
                                    boxShadow: '0 0 0 1px var(--historylist-accent-color, var(--global-primary-color, #3b82f6))'
                                },
                                ...(window?.theme?.HistoricalConvTreeList?.style || {})
                            }}
                        />
                        {searchTerm && (
                            <button
                                onClick={() => setSearchTerm('')}
                                className="HistoricalConvTreeList absolute right-2 flex items-center"
                                style={{
                                    color: 'var(--historylist-text-color, #9ca3af)',
                                    ':hover': {
                                        color: 'var(--historylist-accent-color, #e5e7eb)'
                                    },
                                    ...(window?.theme?.HistoricalConvTreeList?.style || {})
                                }}
                            >
                                <X size={14} />
                            </button>
                        )}
                    </div>
                </div>
            </div>

            {/* Conversation list with scrolling */}
            <div className="HistoricalConvTreeList flex-1 overflow-y-auto mt-2"
                style={{
                    ...(window?.theme?.HistoricalConvTreeList?.style || {})
                }}
            >
                {filteredConvs.length === 0 ? (
                    <div className="HistoricalConvTreeList p-4 text-center flex flex-col items-center"
                        style={{
                            color: 'var(--historylist-text-color, #9ca3af)',
                            ...(window?.theme?.HistoricalConvTreeList?.style || {})
                        }}
                    >
                        {searchTerm ? (
                            <>
                                <MessageSquare size={24} className="HistoricalConvTreeList mb-2" 
                                    style={{
                                        color: 'var(--historylist-text-color, #6b7280)',
                                        ...(window?.theme?.HistoricalConvTreeList?.style || {})
                                    }}
                                />
                                <span>No conversations matching "{searchTerm}"</span>
                            </>
                        ) : (
                            <>
                                <MessageSquare size={24} className="HistoricalConvTreeList mb-2" 
                                    style={{
                                        color: 'var(--historylist-text-color, #6b7280)',
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
                            {filteredConvs.map((conv) => (
                                <div
                                    key={conv.convGuid}
                                    className="HistoricalConvTreeList text-sm cursor-pointer px-2 py-0.5 rounded overflow-hidden text-ellipsis whitespace-normal break-words mb-1"
                                    style={{ 
                                        display: 'block', 
                                        wordBreak: 'break-word',
                                        color: 'var(--historylist-text-color, var(--global-text-color, #e5e7eb))',
                                        backgroundColor: 'var(--historylist-bg, var(--global-background-color, transparent))',
                                        ':hover': {
                                            backgroundColor: 'var(--historylist-accent-color, var(--global-primary-color, rgba(31, 41, 55, 0.4)))',
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
                                </div>
                            ))}
                        </div>
                    </ScrollArea>
                )}
            </div>
        </div>
    );
};

// Export themeable properties for ThemeManager
export const themeableProps = {
    backgroundColor: {
        cssVar: '--historylist-bg',
        description: 'History list background color',
        default: 'transparent',
    },
    textColor: {
        cssVar: '--historylist-text-color',
        description: 'History list text color',
        default: '#e5e7eb',
    },
    borderColor: {
        cssVar: '--historylist-border-color',
        description: 'History list border color',
        default: '#374151',
    },
    accentColor: {
        cssVar: '--historylist-accent-color',
        description: 'History list accent color (used for highlights, focus states)',
        default: '#3b82f6',
    },
    // Only keeping one extra property that's essential for this component
    searchBackground: {
        cssVar: '--historylist-search-bg',
        description: 'Search bar background color',
        default: 'rgba(17, 24, 39, 0.9)',
    },
    // Style override
    style: {
        description: 'Arbitrary CSS style for HistoricalConvTreeList root',
        default: {},
    }
}