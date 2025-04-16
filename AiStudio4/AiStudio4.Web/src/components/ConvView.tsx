// AiStudio4.Web\src\components\ConvView.tsx
import { MarkdownPane } from '@/components/MarkdownPane';
import { MessageAttachments } from '@/components/MessageAttachments';
import { LiveStreamToken } from '@/components/LiveStreamToken';
import { Textarea } from '@/components/ui/textarea';
import { Clipboard, Pencil, Check, X, ArrowDown, Save } from 'lucide-react'; // Use Save (floppy disk) icon for Save As
import { LoadingTimer } from './LoadingTimer';
import { useEffect, useMemo, useState } from 'react';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { formatModelDisplay } from '@/utils/modelUtils';
import { Button } from '@/components/ui/button';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { StickToBottom, useStickToBottomContext } from 'use-stick-to-bottom'; // Added import

// Define themeable properties for the ConvView component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--convview-bg',
    description: 'Background color for the conversation view',
    default: 'transparent',
  },
  textColor: {
    cssVar: '--convview-text-color',
    description: 'Text color for messages',
    default: '#ffffff',
  },
  borderColor: {
    cssVar: '--convview-border-color',
    description: 'Border color for dividers',
    default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
  },
  accentColor: {
    cssVar: '--convview-accent-color',
    description: 'Accent color for buttons and highlights',
    default: '#2563eb', // blue-600
  },
  style: {
    description: 'Arbitrary CSS style for the root container',
    default: {},
  },
  userMessageBackground: {
    cssVar: '--user-message-background',
    description: 'Background for user messages (supports gradients, images, etc.)',
    default: '#1e40af', // blue-800
  },
  aiMessageBackground: {
    cssVar: '--ai-message-background',
    description: 'Background for assistant messages (supports gradients, images, etc.)',
    default: '#1f2937', // gray-800
  },
  userMessageBorderColor: {
    cssVar: '--user-message-border-color',
    description: 'Border color for user messages',
    default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
  },
  aiMessageBorderColor: {
    cssVar: '--ai-message-border-color',
    description: 'Border color for assistant messages',
    default: 'rgba(55, 65, 81, 0.3)', // gray-700/30
  },
  userMessageBorderWidth: {
    cssVar: '--user-message-border-width',
    description: 'Border width for user messages',
    default: '0px',
  },
  aiMessageBorderWidth: {
    cssVar: '--ai-message-border-width',
    description: 'Border width for assistant messages',
    default: '0px',
  },
  userMessageBorderStyle: {
    cssVar: '--user-message-border-style',
    description: 'Border style for user messages',
    default: 'solid',
  },
  aiMessageBorderStyle: {
    cssVar: '--ai-message-border-style',
    description: 'Border style for assistant messages',
    default: 'solid',
  }
};


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
    // Get necessary state and actions from stores
    const { isCancelling: isCancel } = useWebSocketStore();
    const { activeConvId, slctdMsgId, convs, editingMessageId, editMessage, cancelEditMessage, updateMessage } = useConvStore();
    
    // Debug selected message ID changes
    useEffect(() => {
        console.debug('🔍 ConvView - Selected message changed:', { 
            activeConvId, 
            slctdMsgId,
            hasConv: activeConvId ? Boolean(convs[activeConvId]) : false,
            msgCount: activeConvId && convs[activeConvId] ? convs[activeConvId].messages.length : 0
        });
        
        // Debug message costInfo for AI messages
        if (activeConvId && convs[activeConvId]) {
            const messages = convs[activeConvId].messages;
            const aiMessages = messages.filter(msg => msg.source === 'ai');
            
            console.log('🔍 ConvView - AI Messages CostInfo Debug:');
            aiMessages.forEach((msg, idx) => {
                console.log(`AI Message ${idx+1}/${aiMessages.length} (${msg.id}):`, {
                    hasCostInfo: !!msg.costInfo,
                    modelGuid: msg.costInfo?.modelGuid,
                    tokenUsage: msg.costInfo?.tokenUsage ? `${msg.costInfo.tokenUsage.inputTokens} in / ${msg.costInfo.tokenUsage.outputTokens} out` : 'N/A',
                    content: msg.content.substring(0, 30) + '...',
                });
            });
        }
    }, [activeConvId, slctdMsgId, convs]);
    
    const [editContent, setEditContent] = useState<string>('');
    const [visibleCount, setVisibleCount] = useState(20);


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

    // Listen for stream:clear event to reset streamTokens
    useEffect(() => {
        const handleClearStream = () => {
            console.log("handleClearStream");
            if (streamTokens.length > 0) {
                // This will clear the displayed tokens in the parent component
                // by passing an empty array to setStreamTokens
                console.log("handleClearStream2");
                streamTokens = [];
                
            }
        };
        
        window.addEventListener('stream:clear', handleClearStream);
        return () => {
            console.log("stream:clear");
            window.removeEventListener('stream:clear', handleClearStream);
        };
    }, [streamTokens]);

    // Removed: ScrollToBottom component definition

    // Define ScrollToBottom button component using the context
    const ScrollToBottom = () => {
        const { isAtBottom, scrollToBottom } = useStickToBottomContext();
        return (
            !isAtBottom && (
                <button
                    className="ConvView absolute bottom-1 left-1/2 -translate-x-1/2 px-4 py-2 text-xs font-medium rounded-full flex items-center gap-2 shadow-lg transition-all duration-300 ease-in-out focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-900 focus:ring-blue-500"
                    onClick={() => scrollToBottom()}
                    title="Scroll to bottom"
                    style={{
                        backgroundColor: 'var(--convview-bg, rgba(31, 41, 55, 0.7))',
                        color: 'var(--convview-text-color, #ffffff)',
                        ':hover': {
                            backgroundColor: 'var(--convview-accent-color, rgba(55, 65, 81, 0.9))'
                        }
                    }}
                >
                    <ArrowDown size={16} className="flex-shrink-0" />
                    <span>Scroll to bottom</span>
                </button>
            )
        );
    }

    if (!activeConvId) return null;
    if (!messageChain.length) {
        return null;
    }


    const visibleMessages = messageChain.slice(-visibleCount);
    const hasMoreToLoad = visibleCount < messageChain.length;

    return (
        <StickToBottom className="ConvView h-full relative overflow-y-auto" resize="smooth" initial="smooth" style={{
            backgroundColor: 'var(--convview-bg, transparent)',
            ...(window?.theme?.ConvView?.style || {})
        }}>
            <StickToBottom.Content className="ConvView flex flex-col gap-4 p-4">

            {hasMoreToLoad && (
                <button
                    className="ConvView self-center rounded-full px-4 py-2 my-2 text-sm"
                    onClick={() => setVisibleCount(prev => Math.min(prev + 10, messageChain.length))}
                    style={{
                        backgroundColor: 'var(--convview-bg, #374151)',
                        color: 'var(--convview-text-color, #ffffff)',
                        ':hover': {
                            backgroundColor: 'var(--convview-accent-color, #4b5563)'
                        }
                    }}
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
                        className="ConvView w-full group flex flex-col relative markdown-pane"
                        data-message-id={message.id}
                    >
                        <div 
                            className={`ConvView message-container px-4 py-3 shadow-md w-full`}
                            style={{
                                background: message.source === 'user' 
                                    ? 'var(--user-message-background, #1e40af)' 
                                    : 'var(--ai-message-background, #1f2937)',
                                color: 'var(--convview-text-color, #ffffff)',
                                borderRadius: 'var(--convview-border-radius, 0.5rem)',
                                borderColor: message.source === 'user'
                                    ? 'var(--user-message-border-color, rgba(55, 65, 81, 0.3))'
                                    : 'var(--ai-message-border-color, rgba(55, 65, 81, 0.3))',
                                borderWidth: message.source === 'user'
                                    ? 'var(--user-message-border-width, 0px)'
                                    : 'var(--ai-message-border-width, 0px)',
                                borderStyle: message.source === 'user'
                                    ? 'var(--user-message-border-style, solid)'
                                    : 'var(--ai-message-border-style, solid)',
                                ...(window?.theme?.ConvView?.style || {})
                            }}>
                            {editingMessageId === message.id ? (
                                <div className="w-full">
                                    <Textarea
                                        value={editContent}
                                        onChange={(e) => setEditContent(e.target.value)}
                                        className="ConvView w-full h-40 mb-2 font-mono text-sm"
                                        style={{
                                            backgroundColor: 'var(--convview-bg, #374151)',
                                            borderColor: 'var(--convview-border-color, #4b5563)',
                                            color: 'var(--convview-text-color, #ffffff)'
                                        }}
                                    />
                                    <div className="ConvView flex justify-end gap-2">
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
                                            className="ConvView p-1.5 rounded-full"
                                            style={{
                                                backgroundColor: 'var(--convview-accent-color, #2563eb)',
                                                ':hover': {
                                                    backgroundColor: 'var(--convview-accent-color, #1d4ed8)'
                                                }
                                            }}
                                            title="Save edits"
                                        >
                                            <Check size={16} />
                                        </button>
                                        <button
                                            onClick={() => cancelEditMessage()}
                                            className="ConvView p-1.5 rounded-full"
                                            style={{
                                                backgroundColor: 'var(--convview-bg, #374151)',
                                                ':hover': {
                                                    backgroundColor: 'var(--convview-bg, #4b5563)'
                                                }
                                            }}
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
                            <div className="ConvView mt-3 pt-3 border-t" style={{
                                borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))'
                            }}>
                                <MessageAttachments attachments={message.attachments} />
                            </div>
                        )}

                        <div className="ConvView absolute top-3 right-3 flex gap-2 opacity-0 group-hover:opacity-100 transition-all duration-200">
                            <button
                                onClick={() => navigator.clipboard.writeText(message.content)}
                                className="ConvView p-1.5 rounded-full transition-all duration-200"
                                style={{
                                    color: 'var(--convview-text-color, #9ca3af)',
                                    backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
                                    ':hover': {
                                        color: 'var(--convview-text-color, #ffffff)',
                                        backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
                                    }
                                }}
                                title="Copy message"
                            >
                                <Clipboard size={16} />
                            </button>
                            <button
                                onClick={() => {
                                    editMessage(message.id);
                                    setEditContent(message.content);
                                }}
                                className="ConvView p-1.5 rounded-full transition-all duration-200"
                                style={{
                                    color: 'var(--convview-text-color, #9ca3af)',
                                    backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
                                    ':hover': {
                                        color: 'var(--convview-text-color, #ffffff)',
                                        backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
                                    }
                                }}
                                title="Edit raw message"
                            >
                                <Pencil size={16} />
                            </button>
                            <button
                                onClick={async () => {
                                    let suggestedFilename = `message.txt`;
                                    try {
                                        const { saveCodeBlockAsFile } = await import('@/services/api/apiClient');
                                        await saveCodeBlockAsFile({ content: message.content, suggestedFilename });
                                    } catch (e) {
                                        console.error('Save As failed:', e);
                                    }
                                }}
                                className="ConvView p-1.5 rounded-full transition-all duration-200"
                                style={{
                                    color: 'var(--convview-text-color, #9ca3af)',
                                    backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0))',
                                    ':hover': {
                                        color: 'var(--convview-text-color, #ffffff)',
                                        backgroundColor: 'var(--convview-bg, rgba(55, 65, 81, 0.8))'
                                    }
                                }}
                                title="Save message as file"
                            >
                                <Save size={16} />
                            </button>
                        </div>


                        {(message.costInfo?.tokenUsage || message.costInfo || message.timestamp || message.durationMs) && (
                            <div className="ConvView text-small mt-2 border-t pt-1" style={{
                                borderColor: 'var(--convview-border-color, rgba(55, 65, 81, 0.3))',
                                color: 'var(--convview-text-color, #9ca3af)'
                            }}>
                                <div className="ConvView flex flex-wrap items-center gap-x-4">
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
                        className="ConvView w-full group flex flex-col relative mb-4">
                        
                    <div className="ConvView message-container px-4 py-3 shadow-md w-full break-words" style={{
                        background: 'var(--ai-message-background, #1f2937)',
                        color: 'var(--convview-text-color, #ffffff)',
                        borderRadius: '0.5rem',
                        borderColor: 'var(--ai-message-border-color, rgba(55, 65, 81, 0.3))',
                        borderWidth: 'var(--ai-message-border-width, 0px)',
                        borderStyle: 'var(--ai-message-border-style, solid)',
                        ...(window?.theme?.ConvView?.style || {})
                    }}>
                        {(isCancelling || isCancel) && (
                            <div className="ConvView mb-2 p-2 rounded border text-sm" style={{
                                backgroundColor: 'rgba(146, 64, 14, 0.2)',
                                borderColor: 'var(--convview-border-color, rgba(146, 64, 14, 0.5))',
                                color: 'var(--convview-accent-color, #fbbf24)'
                            }}>
                                Cancelling request...
                            </div>
                            )}
                            <div className="overflow-hidden">  
                                <LoadingTimer />
                            </div>
                        <div className="w-full mb-4">
                            {streamTokens.length > 0 ? (

                                <div className="streaming-content">
                                    {streamTokens.map((token, index) => (
                                        <LiveStreamToken key={index} token={token} />
                                    ))}
                                        
                                </div>
                            ) : isStreaming ? (
                                <div className="streaming-content">
                                    {lastStreamedContent ? (
                                        <span className="whitespace-pre-wrap">{lastStreamedContent}</span>
                                    ) : (
                                        <div/>
                                    )}
                                </div>
                            ) : null}
                        </div>
                    </div>
                </div>
            )}
            {/* Removed: <ScrollToBottom /> */}
            </StickToBottom.Content>
            <ScrollToBottom />
        </StickToBottom>
    );
};