import { WebSocketState } from '@/types/websocket';
import { CachedConversationList } from './CachedConversationList';

interface SidebarProps {
    isOpen: boolean;
    wsState: WebSocketState;
}

export function Sidebar({ isOpen, wsState }: SidebarProps) {
    return (
        <div className={`fixed left-0 top-0 h-full w-80 bg-[#1f2937] transform transition-transform duration-300 ease-in-out ${isOpen ? 'translate-x-0' : '-translate-x-full'} z-50`}>
            <div className="flex flex-col h-full">
                <div className="p-4 flex-grow overflow-hidden flex flex-col">
                    <h2 className="text-white text-xl font-bold mb-4">Sidebar</h2>
                    <div className="overflow-y-auto flex-1">
                        <CachedConversationList />
                    </div>
                </div>
                {/* WebSocket Status Panel */}
                <div className="p-3 border-t border-gray-700 text-sm">
                    <div className="flex items-center space-x-2">
                        <div className={`w-2 h-2 rounded-full ${wsState.isConnected ? 'bg-green-500' : 'bg-red-500'}`} />
                        <span className="text-white text-xs">WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}</span>
                    </div>
                    {wsState.clientId && (
                        <div className="mt-1 text-gray-400 text-xs truncate">
                            ID: {wsState.clientId}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}