import { useStreamableWebSocketData } from '@/utils/webSocketUtils';
import { useState, useRef, useEffect, useCallback } from 'react';
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
    
    
    useEffect(() => {
        
        if (streamTokens.length > 0 && !isStreaming) {
            setIsStreaming(true);
        }
        
        
        if (streamTokens.length > 0) {
            setLastStreamedContent(streamTokens.join(''));
        }
        
        
        if (wasCancellingRef.current && !isCancelling && streamTokens.length > 0) {
            const event = new CustomEvent('stream:finalized', { 
                detail: { 
                    content: streamTokens.join('') 
                }
            });
            window.dispatchEvent(event);
        }
        
        
        wasCancellingRef.current = isCancelling;
    }, [streamTokens, isStreaming, isCancelling]);


    // Create a wrapper for reset that also clears the input bar
    const resetStreamTokensAndInput = useCallback(() => {
console.log('resetstreamtokensandinput');
        // Clear the input bar
        if (window.setPrompt) {
console.log('set window prompt');
            window.setPrompt('123');
        }
        
        // Reset the stream tokens
        reset();
        
        // Also reset the streaming state and last streamed content
        setIsStreaming(false);
        setLastStreamedContent('');
    }, [reset]);
    
    useEffect(() => {
        if (!activeConvId || !isStreaming) return;
        
        const conv = convs[activeConvId];
        if (!conv) return;
        
        
        const aiMessages = conv.messages
            .filter(msg => msg.source === 'ai')
            .sort((a, b) => b.timestamp - a.timestamp);
        
        if (aiMessages.length > 0) {
            const newestMessage = aiMessages[0];
            
            
            if (newestMessage.id !== lastMessageIdRef.current && isStreaming) {
                lastMessageIdRef.current = newestMessage.id;
                setIsStreaming(false);
                // Use resetStreamTokensAndInput instead of reset
                resetStreamTokensAndInput();
            }
        }
    }, [activeConvId, convs, isStreaming, resetStreamTokensAndInput]);

    
    useEffect(() => {
        const handleStreamEnd = () => {
            // Use resetStreamTokensAndInput instead of reset
            resetStreamTokensAndInput();
            setIsStreaming(false);
            console.log("sIS = false");
        };
        
        const handleCancelled = () => {
            setIsStreaming(false);
            // Use resetStreamTokensAndInput instead of reset
            resetStreamTokensAndInput();
        };
        
        
        const unsubscribeEnd = listenToWebSocketEvent('stream:end', handleStreamEnd);
        window.addEventListener('request:cancelled', handleCancelled);
        
        return () => {
            unsubscribeEnd();
            window.removeEventListener('request:cancelled', handleCancelled);
        };
    }, [resetStreamTokensAndInput]);



    return { 
        streamTokens, 
        resetStreamTokens: resetStreamTokensAndInput,
        isStreaming,
        lastStreamedContent
    };
}