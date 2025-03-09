// src/components/PanelManager.tsx
import React, { useEffect, useRef } from 'react';
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
  const initialRegistrationDone = useRef(false);

  // Register all panels on mount, only once
  useEffect(() => {
    if (!initialRegistrationDone.current) {
      // Only register panels that don't already exist in the store
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
      
      initialRegistrationDone.current = true;
    }
  }, [panels, registerPanel]);

  
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
          >
            {panel.render(state.isOpen || state.isPinned)}
          </Panel>
        );
      })}
    </div>
  );
}