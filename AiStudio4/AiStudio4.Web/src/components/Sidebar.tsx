import { WebSocketState } from '@/types/websocket';
import { CachedConversationList } from './CachedConversationList';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { MessageSquare, Menu, FolderOpen, Plus, GitBranch } from 'lucide-react';
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
    isOpen: boolean;
    wsState: WebSocketState;
    onToggle: () => void;
}


export function Sidebar({ isOpen, wsState, onToggle }: SidebarProps) {
    const isMobile = useMediaQuery("(max-width: 768px)");

    const toggleButton = (
        <button
            onClick={onToggle}
            className="p-4 w-full flex items-center hover:bg-[#374151] transition-colors duration-200"
        >
            <Menu className="w-6 h-6 text-gray-100" />
        </button>
    );

    if (isMobile) {
        return (
            <>
                <Sheet open={isOpen} onOpenChange={onToggle}>
                    <SheetContent side="left" className="w-80 p-0 bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900 border-r border-gray-700/50 backdrop-blur-lg shadow-2xl">
                        <MobileContent wsState={wsState} onToggle={onToggle} />
                    </SheetContent>
                </Sheet>
            </>
        );
    }

    return (
        <aside
            className={cn(
                "fixed left-0 top-0 z-30 flex h-screen flex-col bg-gradient-to-b from-gray-900 to-gray-800 border-r border-gray-700/50 transition-all duration-300 shadow-xl backdrop-blur-sm",
                isOpen ? "w-80" : "w-16"
            )}
        >
            {toggleButton}
            <DesktopContent wsState={wsState} isCollapsed={!isOpen} />
        </aside>
    );
}

function MobileContent({ wsState, onToggle }: { wsState: WebSocketState; onToggle: () => void }) {
    return (
        <>
            <button
                onClick={onToggle}
                className="p-4 w-full flex items-center hover:bg-[#374151] transition-colors duration-200"
            >
                <Menu className="w-6 h-6 text-gray-100" />
            </button>
            <SheetHeader className="p-4 border-b border-gray-700 bg-[#1f2937]">
                <SheetTitle className="text-gray-100">Conversations</SheetTitle>
            </SheetHeader>
            <SidebarContent wsState={wsState} />
        </>
    );
}

function DesktopContent({ wsState, isCollapsed }: { wsState: WebSocketState; isCollapsed: boolean }) {
    return (
        <>
            <div className="p-4 border-b border-gray-700 bg-[#1f2937] flex items-center">
                {!isCollapsed ? (
                    <h2 className="text-gray-100 text-lg font-semibold">Conversations</h2>
                ) : (
                    <MessageSquare className="h-6 w-6 text-gray-100" />
                )}
            </div>
            <SidebarContent wsState={wsState} isCollapsed={isCollapsed} />
        </>
    );
}

function SidebarContent({ wsState, isCollapsed }: { wsState: WebSocketState; isCollapsed?: boolean }) {
    const [showTreeView, setShowTreeView] = useState(false);
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const state = store.getState();
    const conversations = state.conversations.conversations;

    const handleShowTree = (conversationId: string) => {
        setSelectedConversationId(conversationId);
        setShowTreeView(true);
    };

    const handleCloseTree = () => {
        setShowTreeView(false);
        setSelectedConversationId(null);
    };

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
                {!isCollapsed && "New Chat"}
            </Button>
            <ScrollArea className="flex-1">
                <CachedConversationList
                    collapsed={isCollapsed}
                    onShowTree={handleShowTree}
                />
                {showTreeView && selectedConversationId && (
                    <ConversationTreeView
                        onClose={handleCloseTree}
                        conversationId={selectedConversationId}
                        messages={{
                            id: selectedConversationId,
                            text: "Root",
                            children: buildMessageTree(conversations[selectedConversationId]?.messages || [], false)
                        }}
                    />
                )}
            </ScrollArea>

            <div className="p-3 border-t border-gray-700 bg-[#2d3748]">
                <div className="flex items-center space-x-2">
                    <div className={cn(
                        'w-2 h-2 rounded-full shadow-glow',
                        wsState.isConnected ? 'bg-green-500' : 'bg-red-500'
                    )} />
                    {!isCollapsed && (
                        <span className="text-xs text-gray-300">
                            WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}
                        </span>
                    )}
                </div>
                {!isCollapsed && wsState.clientId && (
                    <div className="mt-1 text-gray-400 text-xs truncate">
                        ID: {wsState.clientId}
                    </div>
                )}
            </div>
        </div>
    );
}