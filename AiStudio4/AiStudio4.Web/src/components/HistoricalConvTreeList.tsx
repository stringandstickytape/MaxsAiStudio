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
                backgroundColor: 'var(--historylist-bg, transparent)',
                color: 'var(--historylist-text-color, #e5e7eb)',
                ...(window?.theme?.HistoricalConvTreeList?.style || {})
            }}
        >
            {/* Search bar at the top */}
            <div className="HistoricalConvTreeList px-3 pt-2 pb-1 backdrop-blur-sm"
                style={{
                    backgroundColor: 'var(--historylist-search-bg, rgba(17, 24, 39, 0.9))',
                    backdropFilter: 'var(--historylist-search-blur, blur(4px))',
                    ...(window?.theme?.HistoricalConvTreeList?.searchContainerStyle || {})
                }}
            >
                <div className="relative">
                    <div className="flex items-center">
                        <Search size={16} className="HistoricalConvTreeList absolute left-3" 
                            style={{
                                color: 'var(--historylist-search-icon-color, #9ca3af)',
                                ...(window?.theme?.HistoricalConvTreeList?.searchIconStyle || {})
                            }}
                        />
                        <input
                            type="text"
                            placeholder="Search conversations..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="HistoricalConvTreeList w-full py-1.5 pl-10 pr-8 text-sm rounded-md border"
                            style={{
                                backgroundColor: 'var(--historylist-search-input-bg, rgba(31, 41, 55, 0.8))',
                                borderColor: 'var(--historylist-search-input-border, #374151)',
                                color: 'var(--historylist-search-input-text, #e5e7eb)',
                                caretColor: 'var(--historylist-search-input-caret, #60a5fa)',
                                '::placeholder': {
                                    color: 'var(--historylist-search-input-placeholder, #6b7280)'
                                },
                                ':focus': {
                                    outline: 'none',
                                    borderColor: 'var(--historylist-search-input-focus-border, #3b82f6)',
                                    boxShadow: 'var(--historylist-search-input-focus-shadow, 0 0 0 1px #3b82f6)'
                                },
                                ...(window?.theme?.HistoricalConvTreeList?.searchInputStyle || {})
                            }}
                        />
                        {searchTerm && (
                            <button
                                onClick={() => setSearchTerm('')}
                                className="HistoricalConvTreeList absolute right-2 flex items-center"
                                style={{
                                    color: 'var(--historylist-search-clear-color, #9ca3af)',
                                    ':hover': {
                                        color: 'var(--historylist-search-clear-hover-color, #e5e7eb)'
                                    },
                                    ...(window?.theme?.HistoricalConvTreeList?.searchClearStyle || {})
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
                    ...(window?.theme?.HistoricalConvTreeList?.listContainerStyle || {})
                }}
            >
                {filteredConvs.length === 0 ? (
                    <div className="HistoricalConvTreeList p-4 text-center flex flex-col items-center"
                        style={{
                            color: 'var(--historylist-empty-text-color, #9ca3af)',
                            ...(window?.theme?.HistoricalConvTreeList?.emptyStateStyle || {})
                        }}
                    >
                        {searchTerm ? (
                            <>
                                <MessageSquare size={24} className="HistoricalConvTreeList mb-2" 
                                    style={{
                                        color: 'var(--historylist-empty-icon-color, #6b7280)',
                                        ...(window?.theme?.HistoricalConvTreeList?.emptyIconStyle || {})
                                    }}
                                />
                                <span>No conversations matching "{searchTerm}"</span>
                            </>
                        ) : (
                            <>
                                <MessageSquare size={24} className="HistoricalConvTreeList mb-2" 
                                    style={{
                                        color: 'var(--historylist-empty-icon-color, #6b7280)',
                                        ...(window?.theme?.HistoricalConvTreeList?.emptyIconStyle || {})
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
                                        color: 'var(--historylist-item-text-color, #e5e7eb)',
                                        backgroundColor: 'var(--historylist-item-bg, transparent)',
                                        ':hover': {
                                            backgroundColor: 'var(--historylist-item-hover-bg, rgba(31, 41, 55, 0.4))'
                                        },
                                        ...(window?.theme?.HistoricalConvTreeList?.itemStyle || {})
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
    searchBackground: {
        cssVar: '--historylist-search-bg',
        description: 'Search bar background color',
        default: 'rgba(17, 24, 39, 0.9)',
    },
    searchBlur: {
        cssVar: '--historylist-search-blur',
        description: 'Search bar backdrop blur effect',
        default: 'blur(4px)',
    },
    searchIconColor: {
        cssVar: '--historylist-search-icon-color',
        description: 'Search icon color',
        default: '#9ca3af',
    },
    searchInputBackground: {
        cssVar: '--historylist-search-input-bg',
        description: 'Search input background color',
        default: 'rgba(31, 41, 55, 0.8)',
    },
    searchInputBorder: {
        cssVar: '--historylist-search-input-border',
        description: 'Search input border color',
        default: '#374151',
    },
    searchInputText: {
        cssVar: '--historylist-search-input-text',
        description: 'Search input text color',
        default: '#e5e7eb',
    },
    searchInputCaret: {
        cssVar: '--historylist-search-input-caret',
        description: 'Search input caret color',
        default: '#60a5fa',
    },
    searchInputPlaceholder: {
        cssVar: '--historylist-search-input-placeholder',
        description: 'Search input placeholder color',
        default: '#6b7280',
    },
    searchInputFocusBorder: {
        cssVar: '--historylist-search-input-focus-border',
        description: 'Search input focus border color',
        default: '#3b82f6',
    },
    searchInputFocusShadow: {
        cssVar: '--historylist-search-input-focus-shadow',
        description: 'Search input focus shadow',
        default: '0 0 0 1px #3b82f6',
    },
    searchClearColor: {
        cssVar: '--historylist-search-clear-color',
        description: 'Search clear button color',
        default: '#9ca3af',
    },
    searchClearHoverColor: {
        cssVar: '--historylist-search-clear-hover-color',
        description: 'Search clear button hover color',
        default: '#e5e7eb',
    },
    emptyTextColor: {
        cssVar: '--historylist-empty-text-color',
        description: 'Empty state text color',
        default: '#9ca3af',
    },
    emptyIconColor: {
        cssVar: '--historylist-empty-icon-color',
        description: 'Empty state icon color',
        default: '#6b7280',
    },
    itemTextColor: {
        cssVar: '--historylist-item-text-color',
        description: 'Conversation item text color',
        default: '#e5e7eb',
    },
    itemBackground: {
        cssVar: '--historylist-item-bg',
        description: 'Conversation item background color',
        default: 'transparent',
    },
    itemHoverBackground: {
        cssVar: '--historylist-item-hover-bg',
        description: 'Conversation item hover background color',
        default: 'rgba(31, 41, 55, 0.4)',
    },
    
    // Style overrides
    style: {
        description: 'Arbitrary CSS style for HistoricalConvTreeList root',
        default: {},
    },
    searchContainerStyle: {
        description: 'Arbitrary CSS style for search container',
        default: {},
    },
    searchIconStyle: {
        description: 'Arbitrary CSS style for search icon',
        default: {},
    },
    searchInputStyle: {
        description: 'Arbitrary CSS style for search input',
        default: {},
    },
    searchClearStyle: {
        description: 'Arbitrary CSS style for search clear button',
        default: {},
    },
    listContainerStyle: {
        description: 'Arbitrary CSS style for conversation list container',
        default: {},
    },
    emptyStateStyle: {
        description: 'Arbitrary CSS style for empty state',
        default: {},
    },
    emptyIconStyle: {
        description: 'Arbitrary CSS style for empty state icon',
        default: {},
    },
    itemStyle: {
        description: 'Arbitrary CSS style for conversation items',
        default: {},
    },
}