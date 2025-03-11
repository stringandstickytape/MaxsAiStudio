// src/components/PanelContainerLayout.tsx
import React, { useEffect } from 'react';
import { usePanelStore } from '@/stores/usePanelStore';
import { cn } from '@/lib/utils';

interface PanelContainerLayoutProps {
  children: React.ReactNode;
}

export function PanelContainerLayout({ children }: PanelContainerLayoutProps) {
  const { panels } = usePanelStore();

  // Determine if panels are visible/pinned
  const hasLeftPanel = panels.sidebar?.isPinned || false;
  const hasRightPanel =
    panels.convTree?.isPinned || panels.settings?.isPinned || panels.systemPrompts?.isPinned || false;

  // Update CSS variables for panel spacing
  useEffect(() => {
    const updatePanelVariables = () => {
      const root = document.documentElement;

      // Base sizing factors based on responsive breakpoints
      const baseLeftWidth = hasLeftPanel ? 320 : 0;
      const baseRightWidth = hasRightPanel ? 320 : 0;

      root.style.setProperty('--panel-left-width', `${baseLeftWidth}px`);
      root.style.setProperty('--panel-right-width', `${baseRightWidth}px`);
    };

    updatePanelVariables();
    window.addEventListener('resize', updatePanelVariables);

    return () => window.removeEventListener('resize', updatePanelVariables);
  }, [hasLeftPanel, hasRightPanel]);

  return (
    <div
      className={cn(
        'panel-adjusted-container h-screen ',
        hasLeftPanel && 'has-left-panel',
        hasRightPanel && 'has-right-panel',
      )}
    >
      {children}
    </div>
  );
}
