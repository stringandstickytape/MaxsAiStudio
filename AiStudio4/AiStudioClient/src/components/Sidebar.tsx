import { WebSocketState } from '@/types/websocket';
import { HistoricalConvTreeList } from './HistoricalConvTreeList';
import { cn } from '@/lib/utils';
import { ScrollArea } from '@/components/ui/scroll-area';
import { ConvTreeView } from '@/components/ConvTreeView';
import { useConvStore } from '@/stores/useConvStore';
import { useSearchStore } from '@/stores/useSearchStore';
import { Separator } from '@/components/ui/separator';
import { useEffect, useState, useMemo } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Search, X } from 'lucide-react';

interface SidebarProps {
  wsState: WebSocketState;
  onReconnectClick: () => void;
}

export interface SidebarThemeableProps {
  background: string;
  border: string;
  text: string;
  highlight: string;
}

export function Sidebar({ wsState, onReconnectClick }: SidebarProps) {
  const { activeConvId, convs } = useConvStore();
  const { 
    searchTerm, 
    setSearchTerm, 
    startSearch, 
    clearSearch, 
    isSearching, 
    searchResults,
    searchError 
  } = useSearchStore();
  const [currentConvId, setCurrentConvId] = useState<string | null>(null);

  // Update currentConvId when activeConvId changes
  useEffect(() => {
    if (activeConvId) {
      setCurrentConvId(activeConvId);
    }
  }, [activeConvId]);
  
  // Memoize the messages array to prevent unnecessary re-renders
  const currentMessages = useMemo(() => {
    return currentConvId && convs[currentConvId] ? convs[currentConvId].messages : [];
  }, [currentConvId, convs[currentConvId]?.messages]);

  // Handle search input changes
  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
  };
  
  // Handle search submission
  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    startSearch();
  };
  
  // Handle clearing search
  const handleClearSearch = () => {
    clearSearch();
  };

  return (
    <div className="Sidebar flex flex-col h-full border-r" 
      style={{
        backgroundColor: 'var(--global-backgroundColor, #111827)',
        borderColor: 'var(--global-borderColor, #1f2937)',
        color: 'var(--global-textColor, #e5e7eb)',
        fontFamily: 'var(--global-fontFamily, inherit)',
        fontSize: 'var(--global-fontSize, 0.875rem)',
        borderRadius: 'var(--global-borderRadius, 0)',
        boxShadow: 'var(--global-boxShadow, none)',
        ...(window?.theme?.Sidebar?.style || {})
      }}
    >
      {/* Search input */}
      <div className="p-2 border-b" style={{ borderColor: 'var(--global-borderColor, #1f2937)' }}>
        <form onSubmit={handleSearchSubmit} className="flex items-center gap-1">
          <Input
            type="text"
            placeholder="Search conversations..."
            value={searchTerm}
            onChange={handleSearchChange}
            className="flex-1 h-8 text-sm bg-transparent border-gray-700"
            style={{
              color: 'var(--global-textColor, #e5e7eb)',
              borderColor: 'var(--global-borderColor, rgba(75, 85, 99, 0.5))'
            }}
          />
          {searchTerm && (
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="h-8 w-8 p-0"
              onClick={handleClearSearch}
              title="Clear search"
            >
              <X className="h-4 w-4" />
            </Button>
          )}
          <Button
            type="submit"
            variant="ghost"
            size="icon"
            className="h-8 w-8 p-0"
            disabled={isSearching}
            title="Search"
          >
            <Search className="h-4 w-4" />
          </Button>
        </form>
        
        {searchError && (
          <div className="mt-1 text-xs text-red-500">
            {searchError}
          </div>
        )}
      </div>

      {/* Split container - top half */}
      <div className="Sidebar h-[calc(50%-24px)] border-b" 
        style={{
          borderColor: 'var(--global-borderColor, #1f2937)'
        }}
      >
        <div className="Sidebar p-2 px-3 text-sm font-medium border-b" 
          style={{
            backgroundColor: 'var(--global-backgroundColor, rgba(31, 41, 55, 0.7))',
            borderColor: 'var(--global-borderColor, rgba(75, 85, 99, 0.5))',
            color: 'var(--global-textColor, #d1d5db)'
          }}
        >
          {searchResults ? `Search Results (${searchResults.length})` : null}
          {searchResults && (
            <Button
              size="sm"
              className="ml-2 p-0 h-auto text-xs"
              onClick={handleClearSearch}
            >
              Clear
            </Button>
          )}
        </div>
        <div className="Sidebar h-[calc(100%-32px)]" 
          style={{
            backgroundColor: 'var(--global-backgroundColor, #111827)'
          }}
        >
          <HistoricalConvTreeList searchResults={searchResults} />
        </div>
      </div>

      {/* Split container - bottom half */}
      <div className="Sidebar h-[calc(50%-24px)]">
        <div className="Sidebar p-2 px-3 text-sm font-medium border-b" 
          style={{
            backgroundColor: 'var(--global-backgroundColor, rgba(31, 41, 55, 0.7))',
            borderColor: 'var(--global-borderColor, rgba(75, 85, 99, 0.5))',
            color: 'var(--global-textColor, #d1d5db)'
          }}
        >

        </div>
        <div className="Sidebar h-[calc(100%-32px)] overflow-auto" 
          style={{
            backgroundColor: 'var(--global-backgroundColor, #111827)'
          }}
        >
          {currentConvId && convs[currentConvId] ? (
            <ConvTreeView
              key={`sidebar-tree-${currentConvId}`}
              convId={currentConvId}
              messages={currentMessages}
            />
          ) : (
            <div className="Sidebar text-center p-4" 
              style={{
                color: 'var(--global-textColor, #9ca3af)'
              }}
            >
              <p>Select a conversation to view its structure</p>
            </div>
          )}
        </div>
      </div>

      {/* Connection status footer */} 
      <div 
        className={cn(
          'Sidebar p-3 border-t rounded-b-md',
          !wsState.isConnected && 'cursor-pointer'
        )}
        onClick={!wsState.isConnected ? onReconnectClick : undefined}
        title={!wsState.isConnected ? 'Click to reconnect' : undefined}
        style={{
          backgroundColor: 'var(--global-backgroundColor, #1f2937)',
          borderColor: 'var(--global-borderColor, #1f2937)',
          ...((!wsState.isConnected) ? {
            '&:hover': {
              backgroundColor: 'var(--global-backgroundColor, rgba(55, 65, 81, 0.5))'
            }
          } : {})
        }}
      >
        <div className="Sidebar flex items-center space-x-2">
          <div
            className="w-2 h-2 rounded-full shadow-glow"
            style={{
              backgroundColor: wsState.isConnected ? 
                'var(--global-primaryColor, #10b981)' : 
                '#ef4444',
              boxShadow: wsState.isConnected ?
                '0 0 8px rgba(16, 185, 129, 0.6)' :
                '0 0 8px rgba(239, 68, 68, 0.6)'
            }}
          />
          <span className="Sidebar text-xs" 
            style={{
              color: 'var(--global-textColor, #d1d5db)'
            }}
          >
            WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
        {wsState.clientId && 
          <div className="Sidebar mt-1 text-xs truncate" 
            style={{
              color: 'var(--global-textColor, #9ca3af)'
            }}
          >
            ID: {wsState.clientId}
          </div>
        }
      </div>
    </div>
  );
}

// Export themeable properties for ThemeManager
export const themeableProps = {
}