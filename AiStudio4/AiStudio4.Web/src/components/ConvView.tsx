import { MarkdownPane } from '@/components/markdown-pane';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { Textarea } from '@/components/ui/textarea';
import { Clipboard, Pencil, Check, X, ArrowDown } from 'lucide-react';
import { SystemPromptComponent } from '@/components/SystemPrompt/SystemPromptComponent';
import { useEffect, useMemo, useRef, useState } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { formatModelDisplay } from '@/utils/modelUtils';
import { Button } from '@/components/ui/button';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

interface ConvViewProps {
    streamTokens: string[];
    isCancelling?: boolean;
    isStreaming?: boolean;
    lastStreamedContent?: string;
}

export const ConvView = ({ streamTokens, isCancelling = false, isStreaming = false, lastStreamedContent = '' }: ConvViewProps) => {
    const { isCancelling: isCancel } = useWebSocketStore();
    const { activeConvId, slctdMsgId, convs, editingMessageId, editMessage, cancelEditMessage, updateMessage } = useConvStore();
    const [editContent, setEditContent] = useState<string>('');
    const containerRef = useRef<HTMLDivElement>(null);
    const [visibleCount, setVisibleCount] = useState(20);
    const [isAtBottom, setIsAtBottom] = useState(true);
    const [autoScrollEnabled, setAutoScrollEnabled] = useState(true);
    const [showScrollButton, setShowScrollButton] = useState(false);
    const lastScrollHeightRef = useRef<number>(0);
    const lastScrollTopRef = useRef<number>(0);
    const scrollAnimationRef = useRef<number | null>(null);
    
    // Register scroll functions to window and manage event dispatch for scroll button state
    useEffect(() => {
        // Register scroll functions on window for external access
        window.scrollChatToBottom = scrollToBottom;
        window.getScrollButtonState = () => showScrollButton;
        
        // Dispatch scroll button state change event when showScrollButton changes
        window.dispatchEvent(new CustomEvent('scroll-button-state-change', { 
            detail: { visible: showScrollButton } 
        }));
        
        return () => {
            delete window.scrollChatToBottom;
            delete window.getScrollButtonState;
        };
    }, [showScrollButton]);


    
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

        const performScrollToBottom = () => {
            if (containerRef.current) {
                containerRef.current.scrollTop = containerRef.current.scrollHeight;
            }
        };

        
        if (messageChain.length > 0 || streamTokens.length > 0) {
            performScrollToBottom();

            
            const timeoutId = setTimeout(performScrollToBottom, 100);
            return () => clearTimeout(timeoutId);
        }
    }, [messageChain.length, streamTokens.length, autoScrollEnabled]);
    
    
    useEffect(() => {
        return () => {
            if (scrollAnimationRef.current) {
                cancelAnimationFrame(scrollAnimationRef.current);
            }
        };
    }, []);

    
    const handleScroll = () => {
        if (!containerRef.current) return;

        const { scrollTop, scrollHeight, clientHeight } = containerRef.current;

        // Detect scroll direction
        const isScrollingUp = scrollTop < lastScrollTopRef.current;
        lastScrollTopRef.current = scrollTop;

        // Check if we're near the bottom of the scroll area
        const bottom = scrollHeight - scrollTop - clientHeight;
        const bottomThreshold = 80;
        const isNearBottom = bottom < bottomThreshold;
        
        // Update bottom state
        setIsAtBottom(isNearBottom);
        
        // Set scroll button visibility based on scroll position
        const shouldShowButton = !isNearBottom && scrollHeight > clientHeight + 100;
        if (shouldShowButton !== showScrollButton) {
            setShowScrollButton(shouldShowButton);
            // Event is now dispatched in the useEffect that watches showScrollButton
        }

        // Load more messages when scrolling up near the top
        if (isScrollingUp && scrollTop < 200) {
            if (visibleCount < messageChain.length) {
                // Store scroll position for restoration
                const previousScrollHeight = scrollHeight;
                // Load more messages
                setVisibleCount(prev => Math.min(prev + 10, messageChain.length));
                // Save scroll height for position restoration
                lastScrollHeightRef.current = previousScrollHeight;
            }
        }

        // Disable auto-scrolling when user manually scrolls away from bottom
        if (!isNearBottom && autoScrollEnabled) {
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

    const scrollToBottom = () => {
        
        if (scrollAnimationRef.current) {
            cancelAnimationFrame(scrollAnimationRef.current);
            scrollAnimationRef.current = null;
        }

        if (containerRef.current) {
            const { scrollTop, scrollHeight, clientHeight } = containerRef.current;
            const targetScrollTop = scrollHeight - clientHeight;
            
            
            const startTime = performance.now();
            const startScrollTop = scrollTop;
            const distance = targetScrollTop - startScrollTop;
            const duration = 300; 
            
            
            const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3);
            
            const animateScroll = (currentTime: number) => {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const eased = easeOutCubic(progress);
                
                if (containerRef.current) {
                    containerRef.current.scrollTop = startScrollTop + distance * eased;
                }
                
                if (progress < 1) {
                    scrollAnimationRef.current = requestAnimationFrame(animateScroll);
                } else {
                    
                    scrollAnimationRef.current = null;
                    setAutoScrollEnabled(true);
                    setShowScrollButton(false);
                }
            };
            
            scrollAnimationRef.current = requestAnimationFrame(animateScroll);
        }
    };
    
    // Simplified scroll button - no need for a separate component
    const renderScrollToBottomButton = () => (
        <Button
            className={`bg-gray-700 hover:bg-gray-600 text-gray-300 px-4 py-1 rounded-md h-8 flex items-center justify-center transition-opacity duration-200 ${showScrollButton ? 'opacity-100' : 'opacity-0'}`}
            onClick={scrollToBottom}
            aria-label="Scroll to bottom"
        >
            <ArrowDown className="h-4 w-4 mr-1" />
            <span className="text-xs">Scroll to bottom</span>
        </Button>
    );

    if (!activeConvId) return null;
    if (!messageChain.length) {
        console.warn('No messages to display in conv:', activeConvId);
        return null;
    }

    
    const visibleMessages = messageChain.slice(-visibleCount);
    const hasMoreToLoad = visibleCount < messageChain.length;

    return (
        <div className="w-full h-full flex flex-col">
            <div
                className="flex-1 overflow-auto"
                ref={containerRef}
                onScroll={handleScroll}
            >
                <div className="conversation-container flex flex-col gap-4 p-4">
                
                <div className="mb-2 bg-gray-800/40 rounded-lg">
                    <SystemPromptComponent 
                        convId={activeConvId || undefined} 
                        onOpenLibrary={() => window.dispatchEvent(new CustomEvent('open-system-prompt-library'))} 
                    />
                </div>
                
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

                
                {(streamTokens.length > 0 || isStreaming) && (
                    <div key="streaming-message"
                         className="w-full group flex flex-col relative mb-4">
                        <div className="message-container px-4 py-3 rounded-lg bg-gray-800 shadow-md w-full">
                            {(isCancelling || isCancel) && (
                                <div className="mb-2 p-2 text-yellow-400 bg-yellow-900/20 rounded border border-yellow-800/50 text-sm">
                                    Cancelling request...
                                </div>
                            )}
                            <div className="w-full">
                                {streamTokens.length > 0 ? (
                                    <div className="streaming-content">
                                        {streamTokens.map((token, index) => (
                                            <LiveStreamToken key={index} token={token} />
                                        ))}
                                    </div>
                                ) : isStreaming ? (
                                    
                                    <div className="streaming-content">
                                        <span className="whitespace-pre-wrap">{lastStreamedContent}</span>
                                    </div>
                                ) : null}
                            </div>
                        </div>
                    </div>
                )}
                </div>
            </div>
        </div>
    );
};