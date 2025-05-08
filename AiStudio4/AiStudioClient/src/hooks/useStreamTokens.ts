import { useStreamableWebSocketData } from '@/utils/webSocketUtils';
import { useState, useRef, useEffect, useCallback } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { useConvStore } from '@/stores/useConvStore';
import { listenToWebSocketEvent } from '@/services/websocket/websocketEvents';
import { useJumpToEndStore } from '@/stores/useJumpToEndStore';

// AiStudioClient\src\hooks\useStreamTokens.ts
export function useStreamTokens() {
    // --- New: ignoreTokens flag ---
    const ignoreTokensRef = useRef(false);

    const { data: streamTokensRaw, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: false });
    const { isCancelling } = useWebSocketStore();
    const wasCancellingRef = useRef(false);

    const [isStreaming, setIsStreaming] = useState(false);
    const [lastStreamedContent, setLastStreamedContent] = useState('');

    const { activeConvId, convs } = useConvStore();
    const lastMessageIdRef = useRef<string | null>(null);

    // --- New: filtered streamTokens ---
    const [streamTokens, setStreamTokens] = useState<string[]>([]);

    // Listen for ignore/resume events
    useEffect(() => {
        const handleIgnore = () => {
            ignoreTokensRef.current = true;
            setStreamTokens([]); // Immediately clear tokens
            setIsStreaming(false);
            setLastStreamedContent('');
        };
        const handleAllow = () => {
            ignoreTokensRef.current = false;
            setStreamTokens([]); // Clear tokens on new send
            setIsStreaming(false);
            setLastStreamedContent('');
        };
        window.addEventListener('stream:ignore', handleIgnore);
        window.addEventListener('stream:allow', handleAllow);
        return () => {
            window.removeEventListener('stream:ignore', handleIgnore);
            window.removeEventListener('stream:allow', handleAllow);
        };
    }, []);

    // Update streamTokens only if not ignoring
    useEffect(() => {
        if (!ignoreTokensRef.current) {
            setStreamTokens(streamTokensRaw);
        } else {
            setStreamTokens([]);
        }
    }, [streamTokensRaw]);

    useEffect(() => {
        if (streamTokens.length > 0 && !isStreaming) {
            setIsStreaming(true);
        }
        if (streamTokens.length > 0) {
            setLastStreamedContent(streamTokens.join(''));
            
            // Check if jump to end is enabled and dispatch event
            if (useJumpToEndStore.getState().jumpToEndEnabled) {
                window.dispatchEvent(new CustomEvent('jump-to-end'));
            }
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
    const resetStreamTokens = useCallback(() => {
        reset();
        setIsStreaming(false);
        setLastStreamedContent('');
        setStreamTokens([]);
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
                if (window.setPrompt) {
                    window.setPrompt('');
                }
                console.log("Reset 1");
                resetStreamTokens();
            }
        }
    }, [activeConvId, convs, isStreaming, resetStreamTokens]);

    useEffect(() => {
        const handleStreamEnd = () => {
            resetStreamTokens();
            setIsStreaming(false);
            setStreamTokens([]); // Explicitly clear tokens when stream ends
        };

        const handleCancelled = () => {
            console.log("Reset 3");
            if (window.setPrompt) {
                window.setPrompt('');
            }
            setIsStreaming(false);
            resetStreamTokens();
        };
        const unsubscribeEnd = listenToWebSocketEvent('stream:end', handleStreamEnd);
        window.addEventListener('request:cancelled', handleCancelled);
        return () => {
            unsubscribeEnd();
            window.removeEventListener('request:cancelled', handleCancelled);
        };
    }, [resetStreamTokens]);

    return {
        streamTokens,
        resetStreamTokens: resetStreamTokens,
        isStreaming,
        lastStreamedContent
    };
}