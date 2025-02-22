import { useEffect } from 'react';
import { messageService } from '@/services/messaging/WebSocketMessageService';

export const useWebSocketMessage = (
    messageType: string,
    handler: (data: any) => void
) => {
    useEffect(() => {
        messageService.subscribe(messageType, handler);

        // Cleanup subscription when component unmounts
        return () => {
            messageService.unsubscribe(messageType, handler);
        };
    }, [messageType, handler]);
};