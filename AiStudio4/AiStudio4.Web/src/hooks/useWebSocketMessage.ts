import { useWebSocket } from './useWebSocket';

export interface WebSocketHookResult {
    isConnected: boolean;
}

// This is a compatibility wrapper to maintain backward compatibility
// with code that uses the old useWebSocketMessage hook
export const useWebSocketMessage = (
    messageType: string,
    handler: (data: any) => void
): WebSocketHookResult => {
    const { isConnected } = useWebSocket({
        subscriptions: { [messageType]: handler }
    });

    return { isConnected };
};