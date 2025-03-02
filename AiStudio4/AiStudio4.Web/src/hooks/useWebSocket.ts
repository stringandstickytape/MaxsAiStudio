import { useState, useEffect, useCallback } from 'react';
import { webSocketService, WebSocketConnectionStatus } from '@/services/websocket/WebSocketService';

interface UseWebSocketOptions {
    autoConnect?: boolean;
    subscriptions?: { [key: string]: (data: any) => void };
}

interface UseWebSocketResult {
    isConnected: boolean;
    clientId: string | null;
    connect: () => void;
    disconnect: () => void;
    send: (messageType: string, content: any) => void;
}

/**
 * Hook for working with WebSocket connections
 */
export function useWebSocket(options: UseWebSocketOptions = {}): UseWebSocketResult {
    const { autoConnect = true, subscriptions = {} } = options;
    
    const [connectionStatus, setConnectionStatus] = useState<WebSocketConnectionStatus>({
        isConnected: webSocketService.isConnected(),
        clientId: webSocketService.getClientId()
    });

    // Handle connection status changes
    useEffect(() => {
        const handleConnectionStatus = (status: WebSocketConnectionStatus) => {
            setConnectionStatus(status);
        };
        
        webSocketService.onConnectionStatusChange(handleConnectionStatus);
        
        return () => {
            webSocketService.offConnectionStatusChange(handleConnectionStatus);
        };
    }, []);
    
    // Set up subscriptions
    useEffect(() => {
        // Subscribe to all message types in the subscriptions object
        Object.entries(subscriptions).forEach(([messageType, handler]) => {
            webSocketService.subscribe(messageType, handler);
        });
        
        // Clean up subscriptions on unmount
        return () => {
            Object.entries(subscriptions).forEach(([messageType, handler]) => {
                webSocketService.unsubscribe(messageType, handler);
            });
        };
    }, [subscriptions]);
    
    // Auto-connect if enabled
    useEffect(() => {
        if (autoConnect) {
            webSocketService.connect();
        }
        
        // We don't automatically disconnect to avoid disconnecting when a component using 
        // this hook unmounts but other components still need the connection
    }, [autoConnect]);
    
    // Convenience wrapper for sending messages
    const send = useCallback((messageType: string, content: any) => {
        webSocketService.send({ messageType, content });
    }, []);
    
    return {
        isConnected: connectionStatus.isConnected,
        clientId: connectionStatus.clientId,
        connect: webSocketService.connect,
        disconnect: webSocketService.disconnect,
        send
    };
}