
import React, { useEffect, useRef } from 'react';
import { cn } from '@/lib/utils';
import { Panel, PanelGroup, PanelResizeHandle, ImperativePanelHandle } from 'react-resizable-panels';
import { usePanelStore } from '@/stores/usePanelStore';
import { PanelState } from '@/types/ui';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';

export interface PanelConfig extends Omit<PanelState, 'isOpen'> {
  id: string;
  render: (isOpen: boolean) => React.ReactNode;
  minSize?: number;
  defaultSize?: number;
}

interface PanelManagerProps {
  panels: PanelConfig[];
  className?: string;
}

interface ResizeHandleProps {
  className?: string;
  direction?: 'horizontal' | 'vertical';
}

const ResizeHandle = ({ className, direction = 'horizontal' }: ResizeHandleProps) => (
  <PanelResizeHandle
    className={cn(
      direction === 'horizontal'
        ? 'w-1.5 hover:w-2 animate-hover relative cursor-col-resize'
        : 'h-1.5 hover:h-2 animate-hover relative cursor-row-resize',
      'before:absolute before:bg-gray-700/50 hover:before:bg-gray-600/80 active:before:bg-blue-500/50',
      direction === 'horizontal'
        ? 'before:top-0 before:bottom-0 before:left-1/2 before:-ml-px before:w-0.5'
        : 'before:left-0 before:right-0 before:top-1/2 before:-mt-px before:h-0.5',
      'before:rounded-full',
      className,
    )}
  />
);

export function PanelManager({ panels, className }: PanelManagerProps) {
  const { registerPanel, panels: panelStates, togglePanel } = usePanelStore();
  const initialRegistrationDone = useRef(false);
  const panelRefs = useRef<Record<string, ImperativePanelHandle | null>>({});

  
  useEffect(() => {
    panels.forEach((panel) => {
      if (!panelRefs.current[panel.id]) {
        panelRefs.current[panel.id] = null;
      }
    });
  }, [panels]);

  
  useEffect(() => {
    if (!initialRegistrationDone.current) {
      
      panels.forEach((panel) => {
        registerPanel({
          id: panel.id,
          position: panel.position,
          size: panel.size,
          zIndex: panel.zIndex,
          title: panel.title,
          isOpen: false
        });
      });

      initialRegistrationDone.current = true;
    }
  }, [panels, registerPanel]);

  
  const leftPanels = panels.filter((p) => p.position === 'left');
  const rightPanels = panels.filter((p) => p.position === 'right');
  const topPanels = panels.filter((p) => p.position === 'top');
  const bottomPanels = panels.filter((p) => p.position === 'bottom');

  
  const renderPanel = (panel: PanelConfig) => {
    const state = panelStates[panel.id] || { isOpen: false };
    const isVisible = state.isOpen;
    const showTitleBar = panel.title && panel.title.trim() !== '';

    if (!isVisible) return null;

    const handleClosePanel = () => {
      usePanelStore.setState(state => ({
        panels: {
          ...state.panels,
          [panel.id]: {
            ...state.panels[panel.id],
            isOpen: false
          }
        }
      }));
      
      requestAnimationFrame(() => {
        usePanelStore.getState().saveState();
      });
    };

    const closeButton = (
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8 text-gray-400 hover:text-gray-100"
        onClick={handleClosePanel}
        title="Close panel"
      >
        <X className="h-4 w-4" />
      </Button>
    );

    return (
      <div className="flex flex-col h-full overflow-hidden relative" style={{ width: 'var(--panel-width, 320px)' }}>
        {showTitleBar ? (
          /* Panel with title bar */
          <>
            <div className="flex-between p-3 border-b border-gray-700 bg-gray-800">
              <h3 className="font-medium text-gray-100 flex-1 truncate">{panel.title}</h3>
              <div className="flex gap-1">
                {closeButton}
              </div>
            </div>
            <div className="flex-1 overflow-auto">{panel.render(isVisible)}</div>
          </>
        ) : (
          /* Panel without title bar, floating close button */
          <>
            <div className="absolute top-2 right-2 z-10">
              {closeButton}
            </div>
            <div className="flex-1 overflow-hidden h-full">{panel.render(isVisible)}</div>
          </>
        )}
      </div>
    );
  };

  
  const renderPanelGroup = (positionPanels: PanelConfig[], direction: 'horizontal' | 'vertical') => {
    
    const visiblePanels = positionPanels.filter((panel) => {
      const state = panelStates[panel.id];
      return state && state.isOpen;
    });

    if (visiblePanels.length === 0) return null;

    return (
      <PanelGroup direction={direction} className="h-full">
        {visiblePanels.map((panel, index) => (
          <React.Fragment key={panel.id}>
            <Panel
              ref={(ref) => (panelRefs.current[panel.id] = ref)}
              id={panel.id}
              minSize={panel.minSize || 10}
              defaultSize={panel.defaultSize || 20}
              className="h-full"
              style={{ width: 'var(--panel-width, 320px)' }}
            >
              {renderPanel(panel)}
            </Panel>
            {index < visiblePanels.length - 1 && <ResizeHandle direction={direction} />}
          </React.Fragment>
        ))}
      </PanelGroup>
    );
  };

  
  return (
    <div className={cn('fixed inset-0 z-40 pointer-events-none', className)}>
      {/* Left panels */}
      <div className="absolute top-0 left-0 bottom-0 pointer-events-auto">
        {renderPanelGroup(leftPanels, 'horizontal')}
      </div>

      {/* Right panels */}
      <div className="absolute top-0 right-0 bottom-0 pointer-events-auto">
        {renderPanelGroup(rightPanels, 'horizontal')}
      </div>

      {/* Top panels */}
      <div className="absolute top-0 left-0 right-0 pointer-events-auto" style={{ left: 'var(--content-margin-left, 0px)', right: 'var(--content-margin-right, 0px)' }}>
        {renderPanelGroup(topPanels, 'vertical')}
      </div>

      {/* Bottom panels */}
      <div className="absolute bottom-0 left-0 right-0 pointer-events-auto" style={{ left: 'var(--content-margin-left, 0px)', right: 'var(--content-margin-right, 0px)' }}>
        {renderPanelGroup(bottomPanels, 'vertical')}
      </div>
    </div>
  );
}
