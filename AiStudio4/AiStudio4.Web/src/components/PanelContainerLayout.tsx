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
    panels.convTree?.isPinned || panels.settings?.isPinned || panels.systemPrompts?.isPinned || panels.toolPanel?.isPinned || false;

  // Update CSS variables for panel spacing
  useEffect(() => {
    const updatePanelVariables = () => {
      const root = document.documentElement;
      
      // Fixed width for panels - ensure consistent sizing
      const PANEL_WIDTH = 320; // Fixed panel width in pixels
      
      // Set panel widths based on their pinned state
      const baseLeftWidth = hasLeftPanel ? PANEL_WIDTH : 0;
      const baseRightWidth = hasRightPanel ? PANEL_WIDTH : 0;
      
      // Set CSS variables for panel widths
      root.style.setProperty('--panel-width', `${PANEL_WIDTH}px`);
      root.style.setProperty('--panel-left-width', `${baseLeftWidth}px`);
      root.style.setProperty('--panel-right-width', `${baseRightWidth}px`);
      
      // Set content margins to match panel widths
      root.style.setProperty('--content-margin-left', hasLeftPanel ? `${PANEL_WIDTH}px` : '0px');
      root.style.setProperty('--content-margin-right', hasRightPanel ? `${PANEL_WIDTH}px` : '0px');
    };
    
    updatePanelVariables();
    window.addEventListener('resize', updatePanelVariables);
    
    return () => window.removeEventListener('resize', updatePanelVariables);
  }, [hasLeftPanel, hasRightPanel]);

  return (
    <div
      className={cn(
        'panel-adjusted-container h-screen transition-all duration-300',
        hasLeftPanel && 'has-left-panel',
        hasRightPanel && 'has-right-panel',
      )}
      style={{
        marginLeft: 'var(--content-margin-left, 0px)',
        marginRight: 'var(--content-margin-right, 0px)',
        width: 'auto', // Let the margins control the width
        maxWidth: 'calc(100% - var(--content-margin-left, 0px) - var(--content-margin-right, 0px))'
      }}
    >
      {children}
    </div>
  );
}