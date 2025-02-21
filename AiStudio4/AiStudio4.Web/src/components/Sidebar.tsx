import { WebSocketState } from '@/types/websocket';
import { CachedConversationList } from './CachedConversationList';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { MessageSquare, Menu, FolderOpen } from 'lucide-react';
import { useMediaQuery } from '@/hooks/use-media-query';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible"
import { ChevronDown } from "lucide-react" // Add this to imports

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
                <SheetContent side="left" className="w-80 p-0 bg-[#1f2937] border-r border-gray-700">
                    <MobileContent wsState={wsState} onToggle={onToggle} />
                </SheetContent>
            </Sheet>
            </>
        );
    }

    return (
        <aside
            className={cn(
                "fixed left-0 top-0 z-30 flex h-screen flex-col bg-[#1f2937] border-r border-gray-700 transition-all duration-300",
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
    return (
        <div className="flex flex-col h-[calc(100vh-10rem)]">
            <ScrollArea className="flex-1">
                <CachedConversationList collapsed={isCollapsed} />

                <Collapsible className="mt-4">
                    <CollapsibleTrigger className="flex w-full items-center justify-between py-2 text-gray-100 hover:bg-[#374151] transition-colors">
                        <div className="flex items-center gap-2">
                            <FolderOpen className="h-4 w-4" />
                            {!isCollapsed && <span>Dummy Section</span>}
                        </div>
                        <ChevronDown className="h-4 w-4" />
                    </CollapsibleTrigger>
                    <CollapsibleContent>
                        <div className="pl-4 py-2 space-y-2">
                            {Array.from({ length: 50 }, (_, i) => (
                                <div
                                    key={i}
                                    className="text-sm text-gray-300 hover:text-gray-100 cursor-pointer transition-colors"
                                >
                                    Subitem {i + 1}
                                </div>
                            ))}
                        </div>
                    </CollapsibleContent>
                </Collapsible>
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