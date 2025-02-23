import { useState, useEffect, useCallback } from 'react';
import { messageService } from '@/services/messaging/WebSocketMessageService';

export function useLiveStream() {
    const [streamTokens, setStreamTokens] = useState<string[]>([]); // Array of strings

    const handleNewStreamToken = useCallback((token: string) => {
        setStreamTokens(prevTokens => [...prevTokens, token]); // Add the new token to the array
    }, []);

    const handleEndStream = useCallback(() => {
        setStreamTokens([]); // Clear the array on endstream
    }, []);

    useEffect(() => {
        messageService.subscribe('cfrag', handleNewStreamToken);
        messageService.subscribe('endstream', handleEndStream);

        return () => {
            messageService.unsubscribe('cfrag', handleNewStreamToken);
            messageService.unsubscribe('endstream', handleEndStream);
        };
    }, [handleNewStreamToken, handleEndStream]);

    return { streamTokens }; // Return the array of tokens
}