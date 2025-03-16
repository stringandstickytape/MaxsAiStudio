
import React, { useEffect } from 'react';
import { usePanelStore } from '@/stores/usePanelStore';
import { cn } from '@/lib/utils';

interface PanelContainerLayoutProps {
  children: React.ReactNode;
}

export function PanelContainerLayout({ children }: PanelContainerLayoutProps) {
  const { panels } = usePanelStore();

  // Check if panels are open in left or right positions
  const hasLeftPanel = panels.sidebar?.isOpen || false;
  const hasRightPanel =
    panels.convTree?.isOpen || panels.settings?.isOpen || panels.systemPrompts?.isOpen || panels.toolPanel?.isOpen || false;

  // Update CSS variables based on panel state
  useEffect(() => {
    const updatePanelVariables = () => {
      const root = document.documentElement;
      
      // Define panel width constant
      const PANEL_WIDTH = 320; 
      
      // Set base widths based on whether panels are open
      const baseLeftWidth = hasLeftPanel ? PANEL_WIDTH : 0;
      const baseRightWidth = hasRightPanel ? PANEL_WIDTH : 0;
      
      // Set CSS variables
      root.style.setProperty('--panel-width', `${PANEL_WIDTH}px`);
      root.style.setProperty('--panel-left-width', `${baseLeftWidth}px`);
      root.style.setProperty('--panel-right-width', `${baseRightWidth}px`);
      
      // Set content margins
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
        'panel-adjusted-container h-screen transition-all duration-300 px-4',
        hasLeftPanel && 'has-left-panel',
        hasRightPanel && 'has-right-panel',
      )}
      style={{
        marginLeft: 'var(--content-margin-left, 0px)',
        marginRight: 'var(--content-margin-right, 0px)',
        width: 'auto', 
        maxWidth: 'calc(100% - var(--content-margin-left, 0px) - var(--content-margin-right, 0px))'
      }}
    >
      {children}
    </div>
  );
}
