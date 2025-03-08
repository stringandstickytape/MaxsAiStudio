// src/components/PanelManager.tsx
import React, { useEffect } from 'react';
import { cn } from '@/lib/utils';
import { Panel } from '@/components/panel';
import { usePanelStore } from '@/stores/usePanelStore';
import { PanelState } from '@/types/ui';

export interface PanelConfig extends Omit<PanelState, 'isOpen' | 'isPinned'> {
  id: string;
  render: (isOpen: boolean) => React.ReactNode;
}

interface PanelManagerProps {
  panels: PanelConfig[];
  className?: string;
}

export function PanelManager({ panels, className }: PanelManagerProps) {
  const { registerPanel, panels: panelStates } = usePanelStore();

  // Register all panels on mount
  useEffect(() => {
    panels.forEach(panel => {
      registerPanel({
        id: panel.id,
        position: panel.position,
        size: panel.size,
        zIndex: panel.zIndex,
        title: panel.title,
        isOpen: false,
        isPinned: false,
      });
    });
  }, []);

  return (
    <div className={cn("relative", className)}>
      {panels.map(panel => {
        const state = panelStates[panel.id] || { isOpen: false, isPinned: false };
        
        return (
          <Panel
            key={panel.id}
            id={panel.id}
            position={panel.position}
            size={panel.size}
            minWidth={panel.minWidth}
            maxWidth={panel.maxWidth}
            width={panel.width}
            zIndex={panel.zIndex}
            title={panel.title}
            isOpen={state.isOpen}
            isPinned={state.isPinned}
          >
            {panel.render(state.isOpen)}
          </Panel>
        );
      })}
    </div>
  );
}