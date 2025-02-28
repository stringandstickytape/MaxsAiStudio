import { WebSocketState } from '@/types/websocket';
import { HistoricalConversationTreeList } from './HistoricalConversationTreeList';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { MessageSquare, Menu, FolderOpen, Plus, GitBranch, Pin, PinOff, X } from 'lucide-react';
import { ConversationTreeView } from './ConversationTreeView';
import { useState } from 'react';
import { useMediaQuery } from '@/hooks/use-media-query';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { ChevronDown } from "lucide-react"
import { store } from '@/store/store';
import { v4 as uuidv4 } from 'uuid';
import { createConversation } from '@/store/conversationSlice';
import { Message } from '@/types/message';
import { buildMessageTree } from '@/utils/treeUtils';

interface SidebarProps {
    wsState: WebSocketState;
    isPinned?: boolean;
    onTogglePin?: () => void;
    onClose?: () => void;
}


export function Sidebar({ wsState, isPinned = false, onTogglePin, onClose }: SidebarProps) {
    return (
        <aside className="fixed left-0 top-0 z-30 flex h-screen w-80 flex-col bg-gradient-to-b from-gray-900 to-gray-800 border-r border-gray-700/50 shadow-xl backdrop-blur-sm">
            <div className="flex justify-between p-3 border-b border-gray-700 bg-[#1f2937]">
                <div className="flex space-x-2">
                    {onTogglePin && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={onTogglePin}
                            className="text-gray-400 hover:text-gray-100"
                        >
                            {isPinned ? (
                                <PinOff className="h-4 w-4" />
                            ) : (
                                <Pin className="h-4 w-4" />
                            )}
                        </Button>
                    )}
                    {!isPinned && onClose && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={onClose}
                            className="text-gray-400 hover:text-gray-100"
                        >
                            <X className="h-4 w-4" />
                        </Button>
                    )}
                </div>
                <h2 className="text-gray-100 text-lg font-semibold flex items-center">Conversations</h2>
            </div>
            <SidebarContent wsState={wsState} isPinned={isPinned} />
        </aside>
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