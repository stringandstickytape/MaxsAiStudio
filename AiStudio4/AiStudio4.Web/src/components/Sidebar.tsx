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
  const [currentConvId, setCurrentConvId] = useState<string | null>(null);

  // Update currentConvId when activeConvId changes
  useEffect(() => {
    if (activeConvId) {
      setCurrentConvId(activeConvId);
    }
  }, [activeConvId]);

  return (
    <div className="Sidebar flex flex-col h-full border-r" 
      style={{
        backgroundColor: 'var(--sidebar-bg, #111827)',
        borderColor: 'var(--sidebar-border-color, #1f2937)',
        color: 'var(--sidebar-text-color, #e5e7eb)',
        fontFamily: 'var(--sidebar-font-family, inherit)',
        fontSize: 'var(--sidebar-font-size, 0.875rem)',
        ...(window?.theme?.Sidebar?.style || {})
      }}
    >
      {/* Split container - top half */}
      <div className="Sidebar h-[calc(50%-24px)] border-b" 
        style={{
          borderColor: 'var(--sidebar-border-color, #1f2937)'
        }}
      >
        <div className="Sidebar p-2 px-3 text-sm font-medium border-b" 
          style={{
            backgroundColor: 'var(--sidebar-bg, rgba(31, 41, 55, 0.7))',
            borderColor: 'var(--sidebar-border-color, rgba(75, 85, 99, 0.5))',
            color: 'var(--sidebar-text-color, #d1d5db)'
          }}
        >
          Conversation History
        </div>
        <div className="Sidebar h-[calc(100%-32px)]" 
          style={{
            backgroundColor: 'var(--sidebar-bg, #111827)'
          }}
        >
          <HistoricalConvTreeList />
        </div>
      </div>

      {/* Split container - bottom half */}
      <div className="Sidebar h-[calc(50%-24px)]">
        <div className="Sidebar p-2 px-3 text-sm font-medium border-b" 
          style={{
            backgroundColor: 'var(--sidebar-bg, rgba(31, 41, 55, 0.7))',
            borderColor: 'var(--sidebar-border-color, rgba(75, 85, 99, 0.5))',
            color: 'var(--sidebar-text-color, #d1d5db)'
          }}
        >
          Message Structure
        </div>
        <div className="Sidebar h-[calc(100%-32px)] overflow-auto" 
          style={{
            backgroundColor: 'var(--sidebar-bg, #111827)'
          }}
        >
          {currentConvId && convs[currentConvId] ? (
            <ConvTreeView
              key={`sidebar-tree-${currentConvId}`}
              convId={currentConvId}
              messages={convs[currentConvId]?.messages || []}
            />
          ) : (
            <div className="Sidebar text-center p-4" 
              style={{
                color: 'var(--sidebar-text-color, #9ca3af)'
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
          backgroundColor: 'var(--sidebar-bg, #1f2937)',
          borderColor: 'var(--sidebar-border-color, #1f2937)',
          ...((!wsState.isConnected) ? {
            '&:hover': {
              backgroundColor: 'var(--sidebar-bg, rgba(55, 65, 81, 0.5))'
            }
          } : {})
        }}
      >
        <div className="Sidebar flex items-center space-x-2">
          <div
            className="w-2 h-2 rounded-full shadow-glow"
            style={{
              backgroundColor: wsState.isConnected ? 
                'var(--sidebar-accent-color, #10b981)' : 
                '#ef4444',
              boxShadow: wsState.isConnected ?
                '0 0 8px rgba(16, 185, 129, 0.6)' :
                '0 0 8px rgba(239, 68, 68, 0.6)'
            }}
          />
          <span className="Sidebar text-xs" 
            style={{
              color: 'var(--sidebar-text-color, #d1d5db)'
            }}
          >
            WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
        {wsState.clientId && 
          <div className="Sidebar mt-1 text-xs truncate" 
            style={{
              color: 'var(--sidebar-text-color, #9ca3af)'
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
  backgroundColor: {
    cssVar: '--sidebar-bg',
    description: 'Sidebar background color',
    default: '#111827',
  },
  textColor: {
    cssVar: '--sidebar-text-color',
    description: 'Sidebar text color',
    default: '#e5e7eb',
  },
  borderColor: {
    cssVar: '--sidebar-border-color',
    description: 'Sidebar border color',
    default: '#1f2937',
  },
  accentColor: {
    cssVar: '--sidebar-accent-color',
    description: 'Sidebar accent color for highlights and status indicators',
    default: '#10b981',
  },
  style: {
    description: 'Arbitrary CSS style for Sidebar root',
    default: {},
  },
  // Only keeping font-related properties as they're essential for readability
  fontFamily: {
    cssVar: '--sidebar-font-family',
    description: 'Sidebar font family',
    default: 'inherit',
  },
  fontSize: {
    cssVar: '--sidebar-font-size',
    description: 'Sidebar font size',
    default: '0.875rem',
  }
}