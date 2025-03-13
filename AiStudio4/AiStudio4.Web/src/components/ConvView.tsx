import { MarkdownPane } from '@/components/markdown-pane';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { Textarea } from '@/components/ui/textarea';
import { Clipboard, Pencil, Check, X } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { formatModelDisplay } from '@/utils/modelUtils';

interface ConvViewProps {
    streamTokens: string[]; 
}

export const ConvView = ({ streamTokens }: ConvViewProps) => {
    const { activeConvId, slctdMsgId, convs, editingMessageId, editMessage, cancelEditMessage, updateMessage } = useConvStore();
    const [editContent, setEditContent] = useState<string>('');
    const containerRef = useRef<HTMLDivElement>(null);
    const [visibleCount, setVisibleCount] = useState(20); 
    const [isAtBottom, setIsAtBottom] = useState(true);
    const [autoScrollEnabled, setAutoScrollEnabled] = useState(true);
    const lastScrollHeightRef = useRef<number>(0);
    const lastScrollTopRef = useRef<number>(0);

    
    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

        
        const graph = new MessageGraph(conv.messages);

        
        
        const startingMessageId =
            streamTokens.length > 0
                ? conv.messages[conv.messages.length - 1].id
                : slctdMsgId || conv.messages[conv.messages.length - 1].id;

        console.log('ConvView: Building message chain from:', {
            startingMessageId,
            slctdMsgId,
            streamActive: streamTokens.length > 0,
            messageCount: conv.messages.length,
        });

        
        return graph.getMessagePath(startingMessageId);
    }, [activeConvId, slctdMsgId, convs, streamTokens.length]);

    
    useEffect(() => {
        setVisibleCount(Math.min(20, messageChain.length));
        setAutoScrollEnabled(true);

        
        if (containerRef.current) {
            containerRef.current.scrollTop = 0;
        }
    }, [activeConvId, slctdMsgId]);

    
    useEffect(() => {
        if (!containerRef.current || !autoScrollEnabled) return;

        const scrollToBottom = () => {
            if (containerRef.current) {
                containerRef.current.scrollTop = containerRef.current.scrollHeight;
            }
        };

        
        if (messageChain.length > 0 || streamTokens.length > 0) {
            scrollToBottom();

            
            setTimeout(scrollToBottom, 100);
        }
    }, [messageChain.length, streamTokens.length, autoScrollEnabled]);

    
    const handleScroll = () => {
        if (!containerRef.current) return;

        const { scrollTop, scrollHeight, clientHeight } = containerRef.current;

        
        const isScrollingUp = scrollTop < lastScrollTopRef.current;
        lastScrollTopRef.current = scrollTop;

        
        const bottom = scrollHeight - scrollTop - clientHeight;
        const bottomThreshold = 50;
        setIsAtBottom(bottom < bottomThreshold);

        
        if (isScrollingUp && scrollTop < 200) {
            if (visibleCount < messageChain.length) {
                
                const previousScrollHeight = scrollHeight;

                
                setVisibleCount(prev => Math.min(prev + 10, messageChain.length));

                
                lastScrollHeightRef.current = previousScrollHeight;
            }
        }

        
        if (bottom > bottomThreshold && autoScrollEnabled) {
            setAutoScrollEnabled(false);
        }
    };

    
    useEffect(() => {
        if (!containerRef.current || lastScrollHeightRef.current === 0) return;

        
        const newScrollHeight = containerRef.current.scrollHeight;

        
        const addedHeight = newScrollHeight - lastScrollHeightRef.current;

        
        if (addedHeight > 0) {
            containerRef.current.scrollTop = addedHeight;
        }

        
        lastScrollHeightRef.current = 0;
    }, [visibleCount]);

    
    const ScrollToBottomButton = () => {
        if (isAtBottom || autoScrollEnabled) return null;

        return (
            <button
                className="fixed bottom-[250px] right-[30px] bg-blue-600 hover:bg-blue-700 text-white p-2 rounded-full shadow-lg z-10"
                onClick={() => {
                    if (containerRef.current) {
                        containerRef.current.scrollTop = containerRef.current.scrollHeight;
                        setAutoScrollEnabled(true);
                    }
                }}
            >
                <svg xmlns="http://www.w3.org/2000/svg" className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
                </svg>
            </button>
        );
    };

    if (!activeConvId) return null;
    if (!messageChain.length) {
        console.warn('No messages to display in conv:', activeConvId);
        return null;
    }

    
    const visibleMessages = messageChain.slice(-visibleCount);
    const hasMoreToLoad = visibleCount < messageChain.length;

    return (
        <div
            className="w-full h-full overflow-auto"
            ref={containerRef}
            onScroll={handleScroll}
        >
            <div className="conversation-container flex flex-col gap-4 p-4">
                
                {hasMoreToLoad && (
                    <button
                        className="self-center bg-gray-700 hover:bg-gray-600 text-white rounded-full px-4 py-2 my-2 text-sm"
                        onClick={() => setVisibleCount(prev => Math.min(prev + 10, messageChain.length))}
                    >
                        Load More Messages ({messageChain.length - visibleCount} remaining)
                    </button>
                )}

                
                {visibleMessages.map((message) => 
                    message.source === 'system' ? null : (
                    <div
                        key={message.id}
                        className="w-full group flex flex-col relative"
                    >
                        <div className={`message-container px-4 py-3 rounded-lg ${message.source === 'user' ? 'bg-blue-800' : 'bg-gray-800'} shadow-md w-full`}>
                        {editingMessageId === message.id ? (
                            <div className="w-full">
                                <Textarea 
                                    value={editContent} 
                                    onChange={(e) => setEditContent(e.target.value)}
                                    className="w-full h-40 bg-gray-700 border-gray-600 text-white mb-2 font-mono text-sm"
                                />
                                <div className="flex justify-end gap-2">
                                    <button
                                        onClick={() => {
                                            if (activeConvId) {
                                                updateMessage({
                                                    convId: activeConvId,
                                                    messageId: message.id,
                                                    content: editContent
                                                });
                                                cancelEditMessage();
                                            }
                                        }}
                                        className="bg-blue-600 hover:bg-blue-700 p-1.5 rounded-full"
                                        title="Save edits"
                                    >
                                        <Check size={16} />
                                    </button>
                                    <button
                                        onClick={() => cancelEditMessage()}
                                        className="bg-gray-700 hover:bg-gray-600 p-1.5 rounded-full"
                                        title="Cancel editing"
                                    >
                                        <X size={16} />
                                    </button>
                                </div>
                            </div>
                        ) : (
                            <MarkdownPane message={message.content} />
                        )}
                        
                        </div>

                        <div className="absolute top-3 right-3 flex gap-2 opacity-0 group-hover:opacity-100 transition-all duration-200">
                            <button
                                onClick={() => navigator.clipboard.writeText(message.content)}
                                className="text-gray-400 hover:text-white p-1.5 rounded-full bg-gray-700 bg-opacity-0 hover:bg-opacity-80 transition-all duration-200"
                                title="Copy message"
                            >
                                <Clipboard size={16} />
                            </button>
                            <button
                                onClick={() => {
                                    editMessage(message.id);
                                    setEditContent(message.content);
                                }}
                                className="text-gray-400 hover:text-white p-1.5 rounded-full bg-gray-700 bg-opacity-0 hover:bg-opacity-80 transition-all duration-200"
                                title="Edit raw message"
                            >
                                <Pencil size={16} />
                            </button>
                        </div>

                        
                        {(message.tokenUsage || message.costInfo) && (
                            <div className="text-small-gray-400 mt-2 border-t border-gray-700 pt-1">
                                <div className="flex flex-wrap items-center gap-x-4">
                                    {message.tokenUsage && (
                                        <div className="flex items-center gap-x-2">

                                        </div>
                                    )}
                                    {message.costInfo && (
                                        <div className="flex items-center gap-x-2">
                                            <span>
                                                Tokens: {message.costInfo.tokenUsage.inputTokens} in / {message.costInfo.tokenUsage.outputTokens} out
                                            </span>
                                            {(message.costInfo.tokenUsage.cacheCreationInputTokens > 0 ||
                                                message.costInfo.tokenUsage.cacheReadInputTokens > 0) && (
                                                    <span>
                                                    (Cache: {message.costInfo.tokenUsage.cacheCreationInputTokens} created,{' '}
                                                    {message.costInfo.tokenUsage.cacheReadInputTokens} read)
                                                    </span>
                                                )}
                                            <span className="flex items-center">Cost: ${message.costInfo.totalCost.toFixed(6)}</span>
                                            <span className="text-gray-500">
                                                (${message.costInfo.inputCostPer1M.toFixed(2)}/1M in, $
                                                {message.costInfo.outputCostPer1M.toFixed(2)}/1M out)
                                            </span>
                                            {message.costInfo.modelGuid && (
                                                <span className="ml-1 text-gray-400 text-xs font-medium bg-gray-700 px-2 py-0.5 rounded-full">
                                                    {formatModelDisplay(message.costInfo.modelGuid)}
                                                </span>
                                            )}
                                        </div>
                                    )}
                                </div>
                            </div>
                        )}
                    </div>
                ))}

                
                {streamTokens.length > 0 && (
                    <div className="p-4 mb-4 rounded-lg bg-gray-800 shadow-md">
                        {streamTokens.map((token, index) => (
                            <LiveStreamToken key={index} token={token} />
                        ))}
                    </div>
                )}
            </div>

            
            <ScrollToBottomButton />
        </div>
    );
};
