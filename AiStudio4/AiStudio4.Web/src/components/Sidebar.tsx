// src/components/Sidebar.tsx
import { WebSocketState } from '@/types/websocket';
import { HistoricalConvTreeList } from './HistoricalConvTreeList';
import { cn } from '@/lib/utils';
import { ScrollArea } from '@/components/ui/scroll-area';

interface SidebarProps {
  wsState: WebSocketState;
}

export function Sidebar({ wsState }: SidebarProps) {

  return (
    <div className="flex-col-full h-full bg-gray-900 border-r border-gray-800">
      <ScrollArea className="flex-1 bg-gray-900">
        <HistoricalConvTreeList />
      </ScrollArea>

      <div className="p-3 border-t border-gray-800 bg-[#1f2937]">
        <div className="flex items-center space-x-2">
          <div
            className={cn('w-2 h-2 rounded-full shadow-glow', wsState.isConnected ? 'bg-green-500' : 'bg-red-500')}
          />
          <span className="text-xs text-gray-300">WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}</span>
        </div>
        {wsState.clientId && <div className="mt-1 text-gray-400 text-xs truncate">ID: {wsState.clientId}</div>}
      </div>
    </div>
  );
}

