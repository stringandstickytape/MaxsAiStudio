// src/components/Sidebar.tsx
import { WebSocketState } from '@/types/websocket';
import { HistoricalConvTreeList } from './HistoricalConvTreeList';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Plus } from 'lucide-react';
import { v4 as uuidv4 } from 'uuid';
import { useConvStore } from '@/stores/useConvStore';

interface SidebarProps {
    wsState: WebSocketState;
}

export function Sidebar({ wsState }: SidebarProps) {
    const { createConv } = useConvStore();

    const handleNewChat = () => {
        createConv({
            id: `conv_${Date.now()}`,
            rootMessage: {
                id: `msg_${uuidv4()}`,
                content: '',
                source: 'system',
                timestamp: Date.now()
            }
        });
    };

    return (
        <div className="flex flex-col h-[calc(100vh-10rem)]">
            <div className="p-2">
                <Button
                    onClick={handleNewChat}
                    className="flex items-center gap-2 bg-[#374151] hover:bg-[#4B5563] text-gray-100 border-gray-600 w-full"
                    variant="outline"
                >
                    <Plus className="h-4 w-4" />
                    New Chat
                </Button>
            </div>
            <ScrollArea className="flex-1">
                <HistoricalConvTreeList />
            </ScrollArea>

            <div className="p-3 border-t border-gray-700 bg-[#2d3748]">
                <div className="flex items-center space-x-2">
                    <div className={cn(
                        'w-2 h-2 rounded-full shadow-glow',
                        wsState.isConnected ? 'bg-green-500' : 'bg-red-500'
                    )} />
                    <span className="text-xs text-gray-300">
                        WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}
                    </span>
                </div>
                {wsState.clientId && (
                    <div className="mt-1 text-gray-400 text-xs truncate">
                        ID: {wsState.clientId}
                    </div>
                )}
            </div>
        </div>
    );
}