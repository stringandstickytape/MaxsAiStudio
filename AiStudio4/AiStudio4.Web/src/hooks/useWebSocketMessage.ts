import { useEffect, useState } from 'react';
import { messageService } from '@/services/messaging/WebSocketMessageService';

export interface WebSocketHookResult {
    isConnected: boolean;
}

export const useWebSocketMessage = (
    messageType: string,
    handler: (data: any) => void
): WebSocketHookResult => {
    const [isConnected, setIsConnected] = useState(messageService.isConnected());

    useEffect(() => {
        const handleConnectionChange = (connected: boolean) => {
            setIsConnected(connected);
        };

        messageService.onConnectionChange(handleConnectionChange);
        messageService.subscribe(messageType, handler);

        return () => {
            messageService.offConnectionChange(handleConnectionChange);
            messageService.unsubscribe(messageType, handler);
        };
    }, [messageType, handler]);

    return { isConnected };
};