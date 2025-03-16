
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';
import { useState, useRef, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';

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
    }, [streamTokens, isStreaming]);
    
    
    useEffect(() => {
        
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
                
                setTimeout(() => {
                    setIsStreaming(false);
                    reset();
                    lastMessageIdRef.current = newestMessage.id;
                }, 100);
            }
        }
    }, [activeConvId, convs, isStreaming, reset]);

    
    useEffect(() => {
        const handleStreamEnd = () => {
            
            
        };
        
        const handleCancelled = () => {
            
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

