
import { useStreamableWebSocketData } from '@/utils/webSocketUtils';

import { useCallback, useRef, useEffect } from 'react';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

export function useStreamTokens() {
    const { data: streamTokens, reset } = useStreamableWebSocketData<string>('stream:token', [], { resetOnEnd: true });
    const { isCancelling } = useWebSocketStore();
    const wasCancellingRef = useRef(false);

    // Reset tokens when cancellation is complete
    useEffect(() => {
        // Only trigger when transitioning from cancelling to not cancelling
        if (wasCancellingRef.current && !isCancelling && streamTokens.length > 0) {
            const timeout = setTimeout(() => {
                const event = new CustomEvent('request:cancelled', { detail: { cancelled: true } });
                window.dispatchEvent(event);
            }, 500);
            return () => clearTimeout(timeout);
        }

        // Update the ref for the next render
        wasCancellingRef.current = isCancelling;
    }, [isCancelling, streamTokens.length]);

    useEffect(() => {
        const handleCancelled = () => reset();
        window.addEventListener('request:cancelled', handleCancelled);
        return () => window.removeEventListener('request:cancelled', handleCancelled);
    }, [reset]);

    return { streamTokens, resetStreamTokens: reset };
}

