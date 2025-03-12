import { MarkdownPane } from '@/components/markdown-pane';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { Textarea } from '@/components/ui/textarea';
import { Clipboard, Pencil, Check, X } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { formatModelDisplay } from '@/utils/modelUtils';

interface ConvViewProps {
    streamTokens: string[]; // Receive the array of tokens
}

export const ConvView = ({ streamTokens }: ConvViewProps) => {
    const { activeConvId, slctdMsgId, convs, editingMessageId, editMessage, cancelEditMessage, updateMessage } = useConvStore();
    const [editContent, setEditContent] = useState<string>('');
    const containerRef = useRef<HTMLDivElement>(null);
    const [visibleCount, setVisibleCount] = useState(20); // Start with 20 messages
    const [isAtBottom, setIsAtBottom] = useState(true);
    const [autoScrollEnabled, setAutoScrollEnabled] = useState(true);
    const lastScrollHeightRef = useRef<number>(0);
    const lastScrollTopRef = useRef<number>(0);

    // Get the message chain (active message plus its ancestors)
    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

        // Create a message graph from the conv messages
        const graph = new MessageGraph(conv.messages);

        // If we're actively streaming (generating new messages), always use the most recent message
        // Otherwise, if we have a selected message ID, use that as the starting point for the message chain
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

        // Get the path from the starting message back to the root
        return graph.getMessagePath(startingMessageId);
    }, [activeConvId, slctdMsgId, convs, streamTokens.length]);

    // Reset visible count when message chain changes (new conversation or thread)
    useEffect(() => {
        setVisibleCount(Math.min(20, messageChain.length));
        setAutoScrollEnabled(true);

        // Reset scroll position when conversation changes
        if (containerRef.current) {
            containerRef.current.scrollTop = 0;
        }
    }, [activeConvId, slctdMsgId]);

    // Auto-scroll to bottom for new messages
    useEffect(() => {
        if (!containerRef.current || !autoScrollEnabled) return;

        const scrollToBottom = () => {
            if (containerRef.current) {
                containerRef.current.scrollTop = containerRef.current.scrollHeight;
            }
        };

        // Scroll when new messages are added or when streaming
        if (messageChain.length > 0 || streamTokens.length > 0) {
            scrollToBottom();

            // Also try scrolling after images and other content has loaded
            setTimeout(scrollToBottom, 100);
        }
    }, [messageChain.length, streamTokens.length, autoScrollEnabled]);

    // Load more messages when scrolling up
    const handleScroll = () => {
        if (!containerRef.current) return;

        const { scrollTop, scrollHeight, clientHeight } = containerRef.current;

        // Detect scroll direction
        const isScrollingUp = scrollTop < lastScrollTopRef.current;
        lastScrollTopRef.current = scrollTop;

        // Check if we're at the bottom (with a small threshold)
        const bottom = scrollHeight - scrollTop - clientHeight;
        const bottomThreshold = 50;
        setIsAtBottom(bottom < bottomThreshold);

        // If scrolling up and near the top, load more messages
        if (isScrollingUp && scrollTop < 200) {
            if (visibleCount < messageChain.length) {
                // Store current position for scroll restoration
                const previousScrollHeight = scrollHeight;

                // Increase visible messages
                setVisibleCount(prev => Math.min(prev + 10, messageChain.length));

                // Store height for position adjustment after render
                lastScrollHeightRef.current = previousScrollHeight;
            }
        }

        // Toggle auto-scroll based on whether user has manually scrolled away from bottom
        if (bottom > bottomThreshold && autoScrollEnabled) {
            setAutoScrollEnabled(false);
        }
    };

    // Maintain scroll position when loading more messages at the top
    useEffect(() => {
        if (!containerRef.current || lastScrollHeightRef.current === 0) return;

        // Get new scroll height
        const newScrollHeight = containerRef.current.scrollHeight;

        // Calculate how much content was added
        const addedHeight = newScrollHeight - lastScrollHeightRef.current;

        // Adjust scroll position to maintain relative position
        if (addedHeight > 0) {
            containerRef.current.scrollTop = addedHeight;
        }

        // Reset reference
        lastScrollHeightRef.current = 0;
    }, [visibleCount]);

    // Button to re-enable auto-scrolling
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

    // Calculate which messages to display
    const visibleMessages = messageChain.slice(-visibleCount);
    const hasMoreToLoad = visibleCount < messageChain.length;

    return (
        <div
            className="w-full h-full overflow-auto"
            ref={containerRef}
            onScroll={handleScroll}
        >
            <div className="conversation-container flex flex-col gap-4 p-4">
                {/* "Load More" button if there are more messages to load */}
                {hasMoreToLoad && (
                    <button
                        className="self-center bg-gray-700 hover:bg-gray-600 text-white rounded-full px-4 py-2 my-2 text-sm"
                        onClick={() => setVisibleCount(prev => Math.min(prev + 10, messageChain.length))}
                    >
                        Load More Messages ({messageChain.length - visibleCount} remaining)
                    </button>
                )}

                {/* Visible messages */}
                {visibleMessages.map((message) => (
                    <div
                        key={message.id}
                        className={`message-container px-4 py-3 rounded-lg ${message.source === 'user' ? 'bg-blue-800' : 'bg-gray-800'
                            } shadow-md w-full relative group`}
                    >
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

                        {/* Token usage info */}
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

                {/* Streaming tokens */}
                {streamTokens.length > 0 && (
                    <div className="p-4 mb-4 rounded-lg bg-gray-800 shadow-md">
                        {streamTokens.map((token, index) => (
                            <LiveStreamToken key={index} token={token} />
                        ))}
                    </div>
                )}
            </div>

            {/* Scroll to bottom button */}
            <ScrollToBottomButton />
        </div>
    );
};