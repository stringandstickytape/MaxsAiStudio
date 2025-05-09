
import React, { useEffect } from 'react';
import { usePanelStore } from '@/stores/usePanelStore';
import { cn } from '@/lib/utils';

interface PanelContainerLayoutProps {
  children: React.ReactNode;
}

export function PanelContainerLayout({ children }: PanelContainerLayoutProps) {
  const { panels } = usePanelStore();
  
  // Subscribe to panel store to update when sidebar collapse state changes
  useEffect(() => {
    const unsubscribe = usePanelStore.subscribe(
      state => state.panels.sidebar?.isCollapsed,
      () => {
        // This will trigger a re-render when the sidebar collapse state changes
      }
    );
    
    return () => unsubscribe();
  }, []);

  
  const hasLeftPanel = panels.sidebar?.isOpen || false;
  const hasRightPanel =
    panels.settings?.isOpen || panels.systemPrompts?.isOpen || panels.toolPanel?.isOpen || false;

  
  useEffect(() => {
    const updatePanelVariables = () => {
      const root = document.documentElement;
      
      
      const PANEL_WIDTH = 320; 
      const COLLAPSED_WIDTH = 48; // Width when sidebar is collapsed
      
      // Check if sidebar is collapsed
      const isSidebarCollapsed = panels.sidebar?.isCollapsed || false;
      
      // Calculate the actual width based on collapsed state
      const sidebarWidth = hasLeftPanel ? (isSidebarCollapsed ? COLLAPSED_WIDTH : PANEL_WIDTH) : 0;
      const baseRightWidth = hasRightPanel ? PANEL_WIDTH : 0;
      
      
      root.style.setProperty('--panel-width', `${PANEL_WIDTH}px`);
      root.style.setProperty('--panel-left-width', `${sidebarWidth}px`);
      root.style.setProperty('--panel-right-width', `${baseRightWidth}px`);
      
      
      root.style.setProperty('--content-margin-left', hasLeftPanel ? `${sidebarWidth}px` : '0px');
      root.style.setProperty('--content-margin-right', hasRightPanel ? `${PANEL_WIDTH}px` : '0px');
    };
    
    updatePanelVariables();
    window.addEventListener('resize', updatePanelVariables);
    
    return () => window.removeEventListener('resize', updatePanelVariables);
  }, [hasLeftPanel, hasRightPanel, panels.sidebar?.isCollapsed]);

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
        width: 'auto', 
        maxWidth: 'calc(100% - var(--content-margin-left, 0px) - var(--content-margin-right, 0px))'
      }}
    >
      {children}
    </div>
  );
}
