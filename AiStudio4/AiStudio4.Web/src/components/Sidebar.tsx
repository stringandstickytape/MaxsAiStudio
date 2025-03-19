import { WebSocketState } from '@/types/websocket';
import { HistoricalConvTreeList } from './HistoricalConvTreeList';
import { cn } from '@/lib/utils';
import { ScrollArea } from '@/components/ui/scroll-area';
import { ConvTreeView } from '@/components/ConvTreeView';
import { useConvStore } from '@/stores/useConvStore';
import { Separator } from '@/components/ui/separator';
import { useEffect, useState } from 'react';

interface SidebarProps {
  wsState: WebSocketState;
}

export function Sidebar({ wsState }: SidebarProps) {
  const { activeConvId, convs } = useConvStore();
  const [currentConvId, setCurrentConvId] = useState<string | null>(null);

  // Update currentConvId when activeConvId changes
  useEffect(() => {
    if (activeConvId) {
      setCurrentConvId(activeConvId);
    }
  }, [activeConvId]);

  return (
    <div className="flex-col h-full bg-gray-900 border-r border-gray-800">
      {/* Split container - top half */}
      <div className="h-1/2 border-b border-gray-800">
        <div className="p-2 px-3 text-sm font-medium text-gray-300 bg-gray-800/70 border-b border-gray-700/50">
          Conversation History
        </div>
        <ScrollArea className="h-[calc(100%-32px)] bg-gray-900">
          <HistoricalConvTreeList />
        </ScrollArea>
      </div>

      {/* Split container - bottom half */}
      <div className="h-1/2">
        <div className="p-2 px-3 text-sm font-medium text-gray-300 bg-gray-800/70 border-b border-gray-700/50">
          Message Structure
        </div>
        <div className="h-[calc(100%-32px)] bg-gray-900 overflow-auto">
          {currentConvId && convs[currentConvId] ? (
            <ConvTreeView
              key={`sidebar-tree-${currentConvId}`}
              convId={currentConvId}
              messages={convs[currentConvId]?.messages || []}
            />
          ) : (
            <div className="text-gray-400 text-center p-4">
              <p>Select a conversation to view its structure</p>
            </div>
          )}
        </div>
      </div>

      {/* Connection status footer */}
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