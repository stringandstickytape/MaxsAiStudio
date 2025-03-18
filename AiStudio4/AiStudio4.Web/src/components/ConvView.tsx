import { MarkdownPane } from '@/components/markdown-pane';
import { MessageAttachments } from '@/components/MessageAttachments';
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

// Helper function to format duration in a human-readable format
const formatDuration = (message?: any, propName: string = 'durationMs') => {
    // Safety check for null/undefined message
    if (!message) {
        console.warn('formatDuration called with null/undefined message');
        return 'Unknown';
    }
    
    // First try direct property access
    let durationMs = message[propName];
    
    // If that fails, try a few other approaches
    if (durationMs === undefined) {
        // Try Object.getOwnPropertyDescriptor
        const descriptor = Object.getOwnPropertyDescriptor(message, propName);
        if (descriptor) {
            durationMs = descriptor.value;
        }
    }
    debugger;
    // Add specific logging for debugging
    console.log(`formatDuration for ${message.id || 'unknown'}: ${durationMs} (${typeof durationMs})`);
    
    // Return early if the value is undefined or null
    if (durationMs === undefined || durationMs === null) {
        console.warn(`No valid duration value found for message ${message.id || 'unknown'}`);
        return "Unknown";
    }
    
    // Ensure we're working with a number
    const duration = Number(durationMs);
    
    // Check if conversion resulted in a valid number
    if (isNaN(duration)) {
        console.warn(`Invalid duration value: ${durationMs}`);
        return "Invalid";
    }
    
    // Handle zero case
    if (duration === 0) {
        return '0ms';
    }
    
    // Format based on duration length
    if (duration < 1000) {
        return `${duration}ms`;
    } else if (duration < 60000) {
        return `${(duration / 1000).toFixed(1)}s`;
    } else {
        const minutes = Math.floor(duration / 60000);
        const seconds = Math.floor((duration % 60000) / 1000);
        return `${minutes}m ${seconds}s`;
    }
};

// Helper function to format timestamp
const formatTimestamp = (timestamp?: number | null) => {
    if (!timestamp) return null;
    
    const date = new Date(timestamp);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
};

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


    useEffect(() => {

        window.scrollChatToBottom = scrollToBottom;
        window.getScrollButtonState = () => showScrollButton;


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

        const convMessages = conv.messages;
        
        // Log timing info for a few messages in the conversation for debugging
        console.log(`ConvView: Checking timing data for conv ${activeConvId}`);
        const samplesToLog = Math.min(convMessages.length, 3);
        for (let i = 0; i < samplesToLog; i++) {
            const msg = convMessages[i];
            console.log(`Original message ${i+1} (${msg.id}):`, {
                timestamp: msg.timestamp,
                timestampType: typeof msg.timestamp,
                durationMs: msg.durationMs,
                durationMsType: typeof msg.durationMs,
                hasOwnProperty: msg.hasOwnProperty('durationMs'),
                keys: Object.keys(msg)
            });
        }
        
        // Create copies of messages with explicit properties to avoid loss during graph processing
        const messages = conv.messages.map(msg => ({
            ...msg,
            // Explicitly include these properties to ensure they're not lost
            id: msg.id,
            content: msg.content,
            source: msg.source,
            timestamp: msg.timestamp,
            parentId: msg.parentId,
            durationMs: msg.durationMs,
            costInfo: msg.costInfo,
            attachments: msg.attachments
        }));
        
        // Get the starting message ID
        const startingMessageId = streamTokens.length > 0
            ? conv.messages[conv.messages.length - 1].id
            : slctdMsgId || conv.messages[conv.messages.length - 1].id;

        const graph = new MessageGraph(messages);
        const path = graph.getMessagePath(startingMessageId);
        
        // Verify properties are preserved
        console.log('Final messageChain:', path.map(msg => ({
            id: msg.id,
            durationMs: msg.durationMs,
            durationMsType: typeof msg.durationMs,
            hasOwnProperty: msg.hasOwnProperty('durationMs')
        })));
        
        return path;
    }, [activeConvId, slctdMsgId, convs, streamTokens.length]);


    useEffect(() => {
        setVisibleCount(Math.min(20, messageChain.length));
        setAutoScrollEnabled(true);

        // Debug: log message chain details
        console.log(`MessageChain for ${activeConvId}:`, messageChain.map(msg => ({
            id: msg.id,
            timestamp: msg.timestamp,
            timestampType: typeof msg.timestamp,
            durationMs: msg.durationMs,
            durationMsType: typeof msg.durationMs,
            durationMsFormatted: formatDuration(msg)
        })));
        
        // Special filter just for durationMs to see which messages have it
        const msgsWithDuration = messageChain.filter(msg => 
            msg.durationMs !== undefined && msg.durationMs !== null);
        console.log(`Messages with duration (${msgsWithDuration.length}):`, 
            msgsWithDuration.map(msg => ({ id: msg.id, durationMs: msg.durationMs })));

        if (containerRef.current) {
            containerRef.current.scrollTop = 0;
        }
    }, [activeConvId, slctdMsgId, messageChain]);


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


        const isScrollingUp = scrollTop < lastScrollTopRef.current;
        lastScrollTopRef.current = scrollTop;


        const bottom = scrollHeight - scrollTop - clientHeight;
        const bottomThreshold = 80;
        const isNearBottom = bottom < bottomThreshold;


        setIsAtBottom(isNearBottom);


        const shouldShowButton = !isNearBottom && scrollHeight > clientHeight + 100;
        if (shouldShowButton !== showScrollButton) {
            setShowScrollButton(shouldShowButton);

        }


        if (isScrollingUp && scrollTop < 200) {
            if (visibleCount < messageChain.length) {

                const previousScrollHeight = scrollHeight;

                setVisibleCount(prev => Math.min(prev + 10, messageChain.length));

                lastScrollHeightRef.current = previousScrollHeight;
            }
        }


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


                    {visibleMessages.map((message) => {
                        // Force add durationMs property to the message if it doesn't exist
                        // This ensures the property exists for rendering regardless of what happened upstream
                        const enhancedMessage = message;
                        
                        // If the message comes from the tree data, it might have the property in the raw data
                        if (enhancedMessage.durationMs === undefined) {
                            // Check if the message has a matching message in the original conv data
                            const conv = convs[activeConvId];
                            if (conv) {
                                const originalMsg = conv.messages.find(m => m.id === message.id);
                                if (originalMsg && 'durationMs' in originalMsg) {
                                    // Force the property to exist on our message
                                    Object.defineProperty(enhancedMessage, 'durationMs', {
                                        value: originalMsg.durationMs,
                                        enumerable: true,
                                        configurable: true,
                                        writable: true
                                    });
                                    console.log(`Recovered durationMs=${originalMsg.durationMs} for message ${message.id}`);
                                }
                            }
                        }
                        debugger;
                        // Debug the durationMs value for this specific message
                        console.log(`RENDER MESSAGE ${enhancedMessage.id}:`, {
                            durationMs: enhancedMessage.durationMs,
                            durationMsType: typeof enhancedMessage.durationMs,
                            durationMsJSON: JSON.stringify(enhancedMessage.durationMs),
                            hasOwnProperty: enhancedMessage.hasOwnProperty('durationMs'),
                            properties: Object.getOwnPropertyNames(enhancedMessage),
                            formattedDuration: formatDuration(enhancedMessage)
                        });
                        return message.source === 'system' ? null : (
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

                                {/* Display message attachments */}
                                {message.attachments && message.attachments.length > 0 && (
                                    <div className="mt-3 pt-3 border-t border-gray-700/30">
                                        <MessageAttachments attachments={message.attachments} />
                                    </div>
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


                                {(message.costInfo?.tokenUsage || message.costInfo || message.timestamp || message.durationMs) && (
                                <div className="text-small-gray-400 mt-2 border-t border-gray-700 pt-1" data-debug={`msgId=${message.id} timestamp=${message.timestamp} (${typeof message.timestamp}) durationMs=${message.durationMs} (${typeof message.durationMs})`}>
                                    {/* Debug element to force display the durationMs value */}
                                    {process.env.NODE_ENV !== 'production' && (
                                        <div className="hidden">
                                            Debug durationMs: {String(message.durationMs)} ({typeof message.durationMs})
                                        </div>
                                    )}
                                        <div className="flex flex-wrap items-center gap-x-4" title={`Debug: timestamp=${message.timestamp} (${typeof message.timestamp}), durationMs=${message.durationMs} (${typeof message.durationMs})`}>
                                            {/* Timestamp and duration info */}
                                            {(typeof message.timestamp === 'number' || typeof message.durationMs === 'number') && (
                                                <div className="flex items-center gap-x-2">
                                                    {typeof message.timestamp === 'number' && message.timestamp > 0 && (
                                                        <span title={new Date(message.timestamp).toLocaleString()}>
                                                            Time: {formatTimestamp(message.timestamp)}
                                                        </span>
                                                    )}
                                                    {typeof message.durationMs === 'number' && message.durationMs > 0 && (
                                                        <span title={`Response took ${message.durationMs}ms`}>
                                                            Duration: {formatDuration(message)}
                                                        </span>
                                                    )}
                                                </div>
                                            )}
                                            
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
                        )
                    }
                    </div>
                );
                })}


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
        </div >
    );
};