﻿// AiStudioClient\src\components\ConvView\ConvView.tsx
import { useEffect, useMemo, useState, useRef, useCallback } from 'react';
import './ConvView.css';
import { StickToBottom } from 'use-stick-to-bottom';
import { MessageGraph } from '@/utils/messageGraph';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useAppearanceStore } from '@/stores/useAppearanceStore';

import { MessageItem } from './MessageItem';
import { ScrollToBottomButton } from './ScrollToBottomButton';
// import { ConversationControls } from './ConversationControls';

// Define themeable properties for the ConvView component
export const themeableProps = {
};

interface ConvViewProps {
    isCancelling?: boolean;
}

export const ConvView = ({
    isCancelling = false
}: ConvViewProps) => {
    const getWidthClass = (width: string = 'full') => {
        switch (width) {
            case 'sm': return 'max-w-sm';
            case 'md': return 'max-w-md';
            case 'lg': return 'max-w-lg';
            case 'xl': return 'max-w-xl';
            case '2xl': return 'max-w-2xl';
            case '3xl': return 'max-w-3xl';
            case '4xl': return 'max-w-4xl';
            case '5xl': return 'max-w-5xl';
            case '6xl': return 'max-w-6xl';
            case '7xl': return 'max-w-7xl';
            case 'full': return 'max-w-full';
            default: return 'max-w-3xl';
        }
    };
    // Create a ref for the scroll container
    const scrollContainerRef = useRef<HTMLDivElement>(null);
    const [isScrolling, setIsScrolling] = useState(false);
    const [isHovering, setIsHovering] = useState(false);
    const scrollTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    
    // Get stick-to-bottom setting from appearance store
    const chatSpaceWidth = useAppearanceStore(state => state.chatSpaceWidth);
    
    
    // Optimize store subscription - only get what we need
    const activeConvId = useConvStore(state => state.activeConvId);
    const slctdMsgId = useConvStore(state => state.slctdMsgId);
    const convs = useConvStore(state => state.convs);

    // Get search results from search store
    const { searchResults } = useSearchStore();
    
    // Import WebSocket store to check streaming status
    const { hasActiveStreaming, isMessageStreaming } = useWebSocketStore();
    
    // Determine which message is the streaming target
    const lastMessage = useMemo(() => {
        const conv = activeConvId ? convs[activeConvId] : null;
        if (!conv || conv.messages.length === 0) return null;
        return conv.messages[conv.messages.length - 1];
    }, [activeConvId, convs]);

    // Calculate message chain based on selected message or latest message
    const messageChain = useMemo(() => {
        if (!activeConvId) return [];

        const conv = convs[activeConvId];
        if (!conv || !conv.messages.length) return [];

        // Get the starting message ID
        const startingMessageId = slctdMsgId || conv.messages[conv.messages.length - 1].id;

        const graph = new MessageGraph(conv.messages);
        const path = graph.getMessagePath(startingMessageId);

        return path;
    }, [activeConvId, slctdMsgId, convs]);

    // No need to update visibleCount when conversation changes, always show all messages


    // Removed stream:clear event handling - no longer needed with new streaming system    // No need for handleLoadMore, always show all messages

    // Handle scroll events for auto-hide scrollbar
    const handleScroll = useCallback(() => {
        setIsScrolling(true);
        
        // Clear existing timeout
        if (scrollTimeoutRef.current) {
            clearTimeout(scrollTimeoutRef.current);
        }
        
        // Set timeout to hide scrollbar after scrolling stops
        scrollTimeoutRef.current = setTimeout(() => {
            setIsScrolling(false);
        }, 1000);
    }, []);

    // Add scroll event listener
    useEffect(() => {
        const scrollContainer = scrollContainerRef.current;
        if (scrollContainer) {
            scrollContainer.addEventListener('scroll', handleScroll, { passive: true });
            return () => {
                scrollContainer.removeEventListener('scroll', handleScroll);
                if (scrollTimeoutRef.current) {
                    clearTimeout(scrollTimeoutRef.current);
                }
            };
        }
    }, [handleScroll]);

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

    const containerClassName = `ConvView ConvViewMain h-full relative overflow-y-auto w-full ${isScrolling ? 'scrolling' : ''}`;
    const containerStyle = {
        backgroundColor: 'var(--global-background-color, transparent)',
        color: 'var(--global-text-color, #ffffff)',
        borderColor: 'var(--global-border-color, rgba(55, 65, 81, 0.3))',
        fontFamily: 'var(--global-font-family, inherit)',
        fontSize: 'var(--global-font-size, inherit)',
        borderRadius: 'var(--global-border-radius, 0)',
        boxShadow: 'var(--global-box-shadow, none)',
        width: '100%',
        minWidth: '100%',
        scrollbarColor: (isScrolling || isHovering) ? '#6b7280 transparent' : 'transparent transparent',
        transition: 'scrollbar-color 0.3s ease',
        ...(window?.theme?.ConvView?.style || {})
    };

    const messageContent = (
        <>
            {/* ConversationControls removed: always show all messages */}
            {visibleMessages.map((message) => {
                // Skip system messages
                if (message.source === 'system') return null;

                // Determine if this message item is the target for the current stream
                const isStreamingTarget = isMessageStreaming(message.id) && (message.source === 'ai' || message.source === 'assistant');
                
                return (
                    <MessageItem
                        key={message.id}
                        message={message}
                        activeConvId={activeConvId}
                        isStreamingTarget={isStreamingTarget}
                    />
                );
            })}
        </>
    );

        return (
            <StickToBottom 
                className={containerClassName}
                ref={scrollContainerRef}
                style={containerStyle}
                onMouseEnter={() => setIsHovering(true)}
                onMouseLeave={() => setIsHovering(false)}
            >
                <StickToBottom.Content>
                    <div className={`ConvView flex flex-col gap-4 w-full ${getWidthClass(chatSpaceWidth)} mx-auto px-4`}>
                        {messageContent}
                    </div>
                </StickToBottom.Content>
                
                {/* Add scroll to bottom button */}
                <ScrollToBottomButton 
                    scrollContainerRef={scrollContainerRef}
                    chatSpaceWidth={chatSpaceWidth}
                />
            </StickToBottom>
        );
  
};