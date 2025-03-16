
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';
import { useState, useRef, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
export function useStreamTokens() {
    
    const { data: streamTokens, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: false });
    const { isCancelling } = useWebSocketStore();
    const wasCancellingRef = useRef(false);
    
    
    const [isStreaming, setIsStreaming] = useState(false);
    const [lastStreamedContent, setLastStreamedContent] = useState('');
    
    
    const { activeConvId, convs } = useConvStore();
    const lastMessageIdRef = useRef<string | null>(null);
    
    // Consolidated useEffect for token stream handling and content updates
    useEffect(() => {
        // Update streaming status if we have tokens
        if (streamTokens.length > 0 && !isStreaming) {
            setIsStreaming(true);
        }
        
        // Update last streamed content whenever tokens change
        if (streamTokens.length > 0) {
            setLastStreamedContent(streamTokens.join(''));
        }
        
        // Check if cancellation just completed
        if (wasCancellingRef.current && !isCancelling && streamTokens.length > 0) {
            const event = new CustomEvent('stream:finalized', { 
                detail: { 
                    content: streamTokens.join('') 
                }
            });
            window.dispatchEvent(event);
        }
        
        // Track cancellation state changes
        wasCancellingRef.current = isCancelling;
    }, [streamTokens, isStreaming, isCancelling]);

    // Handle active conversation message tracking
    useEffect(() => {
        if (!activeConvId || !isStreaming) return;
        
        const conv = convs[activeConvId];
        if (!conv) return;
        
        // Find the newest AI message
        const aiMessages = conv.messages
            .filter(msg => msg.source === 'ai')
            .sort((a, b) => b.timestamp - a.timestamp);
        
        if (aiMessages.length > 0) {
            const newestMessage = aiMessages[0];
            
            // If we have a new message and we're still streaming, reset the stream
            if (newestMessage.id !== lastMessageIdRef.current && isStreaming) {
                lastMessageIdRef.current = newestMessage.id;
                setIsStreaming(false);
                reset();
            }
        }
    }, [activeConvId, convs, isStreaming, reset]);

    // Setup event listeners for stream end and request cancellation
    useEffect(() => {
        const handleStreamEnd = () => {
            // Stream has ended but content is preserved in lastStreamedContent
            setIsStreaming(false);
        };
        
        const handleCancelled = () => {
            setIsStreaming(false);
            reset();
        };
        
        // Use the centralized WebSocket event system
        const unsubscribeEnd = listenToWebSocketEvent('stream:end', handleStreamEnd);
        window.addEventListener('request:cancelled', handleCancelled);
        
        return () => {
            unsubscribeEnd();
            window.removeEventListener('request:cancelled', handleCancelled);
        };
    }, [reset]);

    return { 
        streamTokens, 
        resetStreamTokens: reset,
        isStreaming,
        lastStreamedContent
    };
}

