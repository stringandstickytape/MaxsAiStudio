
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
                reset();
            }
        }
    }, [activeConvId, convs, isStreaming, reset]);

    
    useEffect(() => {
        const handleStreamEnd = () => {
            
            setIsStreaming(false);
        };
        
        const handleCancelled = () => {
            setIsStreaming(false);
            reset();
        };
        
        
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

