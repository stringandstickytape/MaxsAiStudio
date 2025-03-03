import { WebSocketState } from '@/types/websocket';
import { HistoricalConversationTreeList } from './HistoricalConversationTreeList';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Plus } from 'lucide-react';
import { store } from '@/store/store';
import { v4 as uuidv4 } from 'uuid';
import { createConversation } from '@/store/conversationSlice';

interface SidebarProps {
    wsState: WebSocketState;
    isPinned?: boolean;
    onTogglePin?: () => void;
    onClose?: () => void;
}

export function Sidebar({ wsState, isPinned = false, onTogglePin, onClose }: SidebarProps) {
    return (
        <div className="flex flex-col h-full">
            <SidebarContent wsState={wsState} isPinned={isPinned} />
        </div>
    );
}

function SidebarContent({ wsState, isPinned }: { wsState: WebSocketState, isPinned?: boolean }) {
    const state = store.getState();
    const conversations = state.conversations.conversations;

    const handleNewChat = () => {
        store.dispatch(createConversation({
            rootMessage: {
                id: `msg_${uuidv4()}`,
                content: '',
                source: 'system',
                timestamp: Date.now()
            }
        }));
    };

    return (
        <div className="flex flex-col h-[calc(100vh-10rem)]">
            <Button
                onClick={handleNewChat}
                className="m-2 flex items-center gap-2 bg-[#374151] hover:bg-[#4B5563] text-gray-100 border-gray-600"
                variant="outline"
            >
                <Plus className="h-4 w-4" />
                New Chat
            </Button>
            <ScrollArea className="flex-1">
                <HistoricalConversationTreeList />
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