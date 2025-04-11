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
          borderColor: 'var(--sidebar-divider-color, #1f2937)',
          ...(window?.theme?.Sidebar?.sectionStyle || {})
        }}
      >
        <div className="Sidebar p-2 px-3 text-sm font-medium border-b" 
          style={{
            backgroundColor: 'var(--sidebar-header-bg, rgba(31, 41, 55, 0.7))',
            borderColor: 'var(--sidebar-header-border-color, rgba(75, 85, 99, 0.5))',
            color: 'var(--sidebar-header-text-color, #d1d5db)',
            ...(window?.theme?.Sidebar?.headerStyle || {})
          }}
        >
          Conversation History
        </div>
        <div className="Sidebar h-[calc(100%-32px)]" 
          style={{
            backgroundColor: 'var(--sidebar-content-bg, #111827)',
            ...(window?.theme?.Sidebar?.contentStyle || {})
          }}
        >
          <HistoricalConvTreeList />
        </div>
      </div>

      {/* Split container - bottom half */}
      <div className="Sidebar h-[calc(50%-24px)]">
        <div className="Sidebar p-2 px-3 text-sm font-medium border-b" 
          style={{
            backgroundColor: 'var(--sidebar-header-bg, rgba(31, 41, 55, 0.7))',
            borderColor: 'var(--sidebar-header-border-color, rgba(75, 85, 99, 0.5))',
            color: 'var(--sidebar-header-text-color, #d1d5db)',
            ...(window?.theme?.Sidebar?.headerStyle || {})
          }}
        >
          Message Structure
        </div>
        <div className="Sidebar h-[calc(100%-32px)] overflow-auto" 
          style={{
            backgroundColor: 'var(--sidebar-content-bg, #111827)',
            ...(window?.theme?.Sidebar?.contentStyle || {})
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
                color: 'var(--sidebar-empty-text-color, #9ca3af)',
                ...(window?.theme?.Sidebar?.emptyStateStyle || {})
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
          backgroundColor: 'var(--sidebar-footer-bg, #1f2937)',
          borderColor: 'var(--sidebar-border-color, #1f2937)',
          ...(window?.theme?.Sidebar?.footerStyle || {}),
          ...((!wsState.isConnected) ? {
            '&:hover': {
              backgroundColor: 'var(--sidebar-footer-hover-bg, rgba(55, 65, 81, 0.5))'
            }
          } : {})
        }}
      >
        <div className="Sidebar flex items-center space-x-2">
          <div
            className="w-2 h-2 rounded-full shadow-glow"
            style={{
              backgroundColor: wsState.isConnected ? 
                'var(--sidebar-status-connected, #10b981)' : 
                'var(--sidebar-status-disconnected, #ef4444)',
              boxShadow: wsState.isConnected ?
                'var(--sidebar-status-connected-glow, 0 0 8px rgba(16, 185, 129, 0.6))' :
                'var(--sidebar-status-disconnected-glow, 0 0 8px rgba(239, 68, 68, 0.6))',
              ...(window?.theme?.Sidebar?.statusDotStyle || {})
            }}
          />
          <span className="Sidebar text-xs" 
            style={{
              color: 'var(--sidebar-footer-text-color, #d1d5db)',
              ...(window?.theme?.Sidebar?.footerTextStyle || {})
            }}
          >
            WebSocket: {wsState.isConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>
        {wsState.clientId && 
          <div className="Sidebar mt-1 text-xs truncate" 
            style={{
              color: 'var(--sidebar-footer-id-color, #9ca3af)',
              ...(window?.theme?.Sidebar?.footerIdStyle || {})
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
  borderColor: {
    cssVar: '--sidebar-border-color',
    description: 'Sidebar border color',
    default: '#1f2937',
  },
  textColor: {
    cssVar: '--sidebar-text-color',
    description: 'Sidebar text color',
    default: '#e5e7eb',
  },
  fontFamily: {
    cssVar: '--sidebar-font-family',
    description: 'Sidebar font family',
    default: 'inherit',
  },
  fontSize: {
    cssVar: '--sidebar-font-size',
    description: 'Sidebar font size',
    default: '0.875rem',
  },
  dividerColor: {
    cssVar: '--sidebar-divider-color',
    description: 'Sidebar divider color',
    default: '#1f2937',
  },
  headerBackground: {
    cssVar: '--sidebar-header-bg',
    description: 'Sidebar header background color',
    default: 'rgba(31, 41, 55, 0.7)',
  },
  headerBorderColor: {
    cssVar: '--sidebar-header-border-color',
    description: 'Sidebar header border color',
    default: 'rgba(75, 85, 99, 0.5)',
  },
  headerTextColor: {
    cssVar: '--sidebar-header-text-color',
    description: 'Sidebar header text color',
    default: '#d1d5db',
  },
  contentBackground: {
    cssVar: '--sidebar-content-bg',
    description: 'Sidebar content background color',
    default: '#111827',
  },
  emptyTextColor: {
    cssVar: '--sidebar-empty-text-color',
    description: 'Sidebar empty state text color',
    default: '#9ca3af',
  },
  footerBackground: {
    cssVar: '--sidebar-footer-bg',
    description: 'Sidebar footer background color',
    default: '#1f2937',
  },
  footerHoverBackground: {
    cssVar: '--sidebar-footer-hover-bg',
    description: 'Sidebar footer hover background color',
    default: 'rgba(55, 65, 81, 0.5)',
  },
  footerTextColor: {
    cssVar: '--sidebar-footer-text-color',
    description: 'Sidebar footer text color',
    default: '#d1d5db',
  },
  footerIdColor: {
    cssVar: '--sidebar-footer-id-color',
    description: 'Sidebar footer ID text color',
    default: '#9ca3af',
  },
  statusConnected: {
    cssVar: '--sidebar-status-connected',
    description: 'Sidebar connected status color',
    default: '#10b981',
  },
  statusDisconnected: {
    cssVar: '--sidebar-status-disconnected',
    description: 'Sidebar disconnected status color',
    default: '#ef4444',
  },
  statusConnectedGlow: {
    cssVar: '--sidebar-status-connected-glow',
    description: 'Sidebar connected status glow effect',
    default: '0 0 8px rgba(16, 185, 129, 0.6)',
  },
  statusDisconnectedGlow: {
    cssVar: '--sidebar-status-disconnected-glow',
    description: 'Sidebar disconnected status glow effect',
    default: '0 0 8px rgba(239, 68, 68, 0.6)',
  },
  
  // Style overrides
  style: {
    description: 'Arbitrary CSS style for Sidebar root',
    default: {},
  },
  sectionStyle: {
    description: 'Arbitrary CSS style for sidebar sections',
    default: {},
  },
  headerStyle: {
    description: 'Arbitrary CSS style for sidebar headers',
    default: {},
  },
  contentStyle: {
    description: 'Arbitrary CSS style for sidebar content areas',
    default: {},
  },
  emptyStateStyle: {
    description: 'Arbitrary CSS style for empty state messages',
    default: {},
  },
  footerStyle: {
    description: 'Arbitrary CSS style for sidebar footer',
    default: {},
  },
  footerTextStyle: {
    description: 'Arbitrary CSS style for footer text',
    default: {},
  },
  footerIdStyle: {
    description: 'Arbitrary CSS style for footer ID text',
    default: {},
  },
  statusDotStyle: {
    description: 'Arbitrary CSS style for status indicator dot',
    default: {},
  },
}