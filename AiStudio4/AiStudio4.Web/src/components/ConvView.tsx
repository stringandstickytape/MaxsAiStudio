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
import { StickToBottom, useStickToBottomContext } from 'use-stick-to-bottom';

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
    
    // Return early if the value is undefined or null
    if (durationMs === undefined || durationMs === null) {
        return "Unknown";
    }
    
    // Ensure we're working with a number
    const duration = Number(durationMs);
    
    // Check if conversion resulted in a valid number
    if (isNaN(duration)) {
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
    const [visibleCount, setVisibleCount] = useState(20);

    // Set up global functions for compatibility with existing code
    const { scrollToBottom, isAtBottom } = useStickToBottomContext();

    useEffect(() => {
        window.scrollChatToBottom = scrollToBottom;
        window.getScrollButtonState = () => !isAtBottom;

        return () => {
            delete window.scrollChatToBottom;
            delete window.getScrollButtonState;
        };
    }, [scrollToBottom, isAtBottom]);



    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

        const convMessages = conv.messages;


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


        return path;
    }, [activeConvId, slctdMsgId, convs, streamTokens.length]);


    useEffect(() => {
        setVisibleCount(Math.min(20, messageChain.length));
    }, [activeConvId, slctdMsgId, messageChain]);


    // ScrollToBottom component that uses the parent isAtBottom and scrollToBottom
    const ScrollToBottom = () => {
        // Notify InputBar about scroll button state changes
        useEffect(() => {
            window.dispatchEvent(new CustomEvent('scroll-button-state-change', {
                detail: { visible: !isAtBottom }
            }));
        }, [isAtBottom]);

        return !isAtBottom ? (
            <Button
                className="bg-gray-700 hover:bg-gray-600 text-gray-300 px-4 py-1 rounded-md h-8 flex items-center justify-center transition-opacity duration-200 opacity-100 absolute bottom-4 right-4 z-10"
                onClick={scrollToBottom}
                aria-label="Scroll to bottom"
            >
                <ArrowDown className="h-4 w-4 mr-1" />
                <span className="text-xs">Scroll to bottom</span>
            </Button>
        ) : null;
    };



    if (!activeConvId) return null;
    if (!messageChain.length) {
        return null;
    }


    const visibleMessages = messageChain.slice(-visibleCount);
    const hasMoreToLoad = visibleCount < messageChain.length;

    return (
        <div className="w-full h-full flex flex-col gap-4 p-4">

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
                        }
                    }
                }
                return message.source === 'system' ? null : (
                <div
                        key={message.id}
                        className="w-full group flex flex-col relative"
                        data-message-id={message.id}
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
                            <div className="text-small-gray-400 mt-2 border-t border-gray-700 pt-1">
                                <div className="flex flex-wrap items-center gap-x-4">
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
                        <div className="w-full mb-4">
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
            <ScrollToBottom />
        </div>
    );
};