// AiStudioClient\src\components\ConvView\ConvView.tsx
import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import { useProjectPotatoStore } from '@/stores/useProjectPotatoStore';
import { StickToBottom } from 'use-stick-to-bottom';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';

import { MessageItem } from './MessageItem';
import { StreamingMessage } from './StreamingMessage';
// import { ConversationControls } from './ConversationControls';

// Define themeable properties for the ConvView component
export const themeableProps = {
};

interface ConvViewProps {
    streamTokens: string[];
    isCancelling?: boolean;
    isStreaming?: boolean;
    lastStreamedContent?: string;
}

export const ConvView = ({
    streamTokens,
    isCancelling = false,
    isStreaming = false,
    lastStreamedContent = ''
}: ConvViewProps) => {
    // Create a ref for the scroll container
    const scrollContainerRef = useRef<HTMLDivElement>(null);
    
    // Get the stick-to-bottom state from ProjectPotato store
    const { isEnabled } = useProjectPotatoStore();
    // Get necessary state from stores
    const { activeConvId, slctdMsgId, convs } = useConvStore();

    // Get search results from search store
    const { searchResults } = useSearchStore();

    // Add scroll event listener to detect manual scrolling
    useEffect(() => {

        // Common handler function
        const handleScroll = (e: Event) => {


            const target = e.target as HTMLElement;
            // Check if this is a ConvView-related scroll
            let isConvViewScroll = false;

            // Check if the target is the ConvView or a child of it
            if (scrollContainerRef.current && scrollContainerRef.current.contains(target)) {
                isConvViewScroll = true;
            }

            // Only process if it's a ConvView scroll
            if (isConvViewScroll) {
                // Get the current state
                const { isEnabled } = useProjectPotatoStore.getState();

                if (isEnabled) {
                    useProjectPotatoStore.getState().setIsEnabled(false);
                }
            }
        };

        // Add event listeners to all possible elements
        if (scrollContainerRef.current) {
            scrollContainerRef.current.addEventListener('scroll', handleScroll, { capture: true });
        }

        // this doesn't work because race condition
        const convViewElement2 = document.querySelector('.ConvViewMain');
        if (convViewElement2) {
            convViewElement2.addEventListener('scroll', handleScroll, { capture: true });
        }

        document.addEventListener('scroll', handleScroll, { capture: true });

        return () => {
            if (scrollContainerRef.current) {
                scrollContainerRef.current.removeEventListener('scroll', handleScroll, { capture: true });
            }
            if (convViewElement2) {
                convViewElement2.removeEventListener('scroll', handleScroll, { capture: true });
            }
            document.removeEventListener('scroll', handleScroll, { capture: true });
            window.removeEventListener('scroll', handleScroll, { capture: true });
        };
    }, []);

    // Calculate message chain based on selected message or latest message
    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

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
            cumulativeCost: msg.cumulativeCost,
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

    // No need to update visibleCount when conversation changes, always show all messages


    // Listen for stream:clear event to reset streamTokens
    useEffect(() => {
        const handleClearStream = () => {
            if (streamTokens.length > 0) {
                // This will clear the displayed tokens in the parent component
                streamTokens = [];
            }
        };

        window.addEventListener('stream:clear', handleClearStream);
        return () => {
            window.removeEventListener('stream:clear', handleClearStream);
        };
    }, [streamTokens]);

    // No need for handleLoadMore, always show all messages


    if (!activeConvId) return null;
    if (!messageChain.length) return null;

    // Get the current conversation's search results if any
    const currentConvSearchResult = searchResults?.find(result => result.conversationId === activeConvId);

    // Filter messages based on search results if we have them
    let visibleMessages = messageChain;
    if (searchResults && currentConvSearchResult) {
        visibleMessages = visibleMessages.filter(message =>
            currentConvSearchResult.matchingMessageIds.includes(message.id)
        );
    }

    return (
        <StickToBottom 
            className="ConvView ConvViewMain h-full relative overflow-y-auto"
            ref={scrollContainerRef}
            stickToBottom={isEnabled}
            style={{
                backgroundColor: 'var(--global-background-color, transparent)',
                color: 'var(--global-text-color, #ffffff)',
                borderColor: 'var(--global-border-color, rgba(55, 65, 81, 0.3))',
                fontFamily: 'var(--global-font-family, inherit)',
                fontSize: 'var(--global-font-size, inherit)',
                borderRadius: 'var(--global-border-radius, 0)',
                boxShadow: 'var(--global-box-shadow, none)',
                ...(window?.theme?.ConvView?.style || {})
            }}
        >
            <StickToBottom.Content className="ConvView flex flex-col gap-4 p-4">
                {/* ConversationControls removed: always show all messages */}

                {visibleMessages.map((message) => {
                    // Skip system messages
                    if (message.source === 'system') return null;

                    // Force add durationMs property to the message if it doesn't exist
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

                    return (
                        <MessageItem
                            key={message.id}
                            message={enhancedMessage}
                            activeConvId={activeConvId}
                        />
                    );
                })}

                <StreamingMessage
                    streamTokens={streamTokens}
                    isStreaming={isStreaming}
                    lastStreamedContent={lastStreamedContent}
                />
            </StickToBottom.Content>
        </StickToBottom>
    );
};