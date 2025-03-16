
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';
import { useState, useRef, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';

export function useStreamTokens() {
    // Set resetOnEnd to false to prevent automatic clearing
    const { data: streamTokens, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: false });
    const { isCancelling } = useWebSocketStore();
    const wasCancellingRef = useRef(false);
    
    // Track streaming state for UI coordination
    const [isStreaming, setIsStreaming] = useState(false);
    const [lastStreamedContent, setLastStreamedContent] = useState('');
    
    // Monitor changes to the active conversation for coordination
    const { activeConvId, convs } = useConvStore();
    const lastMessageIdRef = useRef<string | null>(null);
    
    // Track when streaming starts
    useEffect(() => {
        if (streamTokens.length > 0 && !isStreaming) {
            setIsStreaming(true);
        }
        
        // Save complete content for transitions
        if (streamTokens.length > 0) {
            setLastStreamedContent(streamTokens.join(''));
        }
    }, [streamTokens, isStreaming]);
    
    // Reset tokens when cancellation is complete
    useEffect(() => {
        // Only trigger when transitioning from cancelling to not cancelling
        if (wasCancellingRef.current && !isCancelling && streamTokens.length > 0) {
            const timeout = setTimeout(() => {
                const event = new CustomEvent('request:cancelled', { 
                    detail: { 
                        cancelled: true,
                        content: streamTokens.join('') 
                    }
                });
                window.dispatchEvent(event);
            }, 300);
            return () => clearTimeout(timeout);
        }

        wasCancellingRef.current = isCancelling;
    }, [isCancelling, streamTokens]);

    // Monitor conversation for completed message
    useEffect(() => {
        if (!activeConvId || !isStreaming) return;
        
        const conv = convs[activeConvId];
        if (!conv) return;
        
        // Get newest AI message
        const aiMessages = conv.messages
            .filter(msg => msg.source === 'ai')
            .sort((a, b) => b.timestamp - a.timestamp);
        
        if (aiMessages.length > 0) {
            const newestMessage = aiMessages[0];
            
            // If we have a new message and we were streaming, reset stream tokens
            if (newestMessage.id !== lastMessageIdRef.current && isStreaming) {
                // Wait briefly to ensure the new message is rendered
                setTimeout(() => {
                    setIsStreaming(false);
                    reset();
                    lastMessageIdRef.current = newestMessage.id;
                }, 100);
            }
        }
    }, [activeConvId, convs, isStreaming, reset]);

    // Handle stream end event to coordinate with message appearance
    useEffect(() => {
        const handleStreamEnd = () => {
            // Don't reset immediately - wait for the actual AI message to appear
            // The conversation effect above will handle the reset once the message is ready
        };
        
        const handleCancelled = () => {
            // For cancellation, we do want to reset immediately
            setIsStreaming(false);
            reset();
        };
        
        window.addEventListener('stream:end', handleStreamEnd);
        window.addEventListener('request:cancelled', handleCancelled);
        return () => {
            window.removeEventListener('stream:end', handleStreamEnd);
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

