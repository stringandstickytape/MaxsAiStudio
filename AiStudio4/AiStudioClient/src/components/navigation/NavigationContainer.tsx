// AiStudioClient\src\components\navigation\NavigationContainer.tsx
import { useState, useEffect, useMemo, useCallback, ReactNode } from 'react';
import { PanelManager, type PanelConfig } from '@/components/PanelManager';
import { PanelContainerLayout } from '@/components/PanelContainerLayout';
import { Sidebar } from '../Sidebar';
import { useWebSocket } from '@/hooks/useWebSocket';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';

import { useUserPromptStore } from '@/stores/useUserPromptStore';

/**
 * Themeable properties for NavigationContainer
 */
export const themeableProps = {};

interface NavigationContainerProps {
  children: ReactNode;
}

export function NavigationContainer({ children }: NavigationContainerProps) {
  const [selectedConvId, setSelectedConvId] = useState<string | null>(null);
  const { isConnected, clientId, connect } = useWebSocket(); // Get connect function
  const wsState = { isConnected, clientId, messages: [] };

  const { togglePanel, panels } = usePanelStore();

  const { activeConvId, convs } = useConvStore();

  const { setConvSystemPrompt } = useSystemPromptManagement();

  const { setConvPrompt } = useSystemPromptStore();

  const openPanel = useCallback(
    (panelId: string) => {
      const panel = panels[panelId];

      if (panel && !panel.isOpen) {
        togglePanel(panelId);
      }
    },
    [panels, togglePanel],
  );

  useEffect(() => {
    const handleOpenToolPanel = () => {
      openPanel('toolPanel');
    };

    window.addEventListener('openToolPanel', handleOpenToolPanel);
    return () => {
      window.removeEventListener('openToolPanel', handleOpenToolPanel);
    };
  }, [openPanel]);

  useEffect(() => {
    if (activeConvId && activeConvId !== selectedConvId) {
      setSelectedConvId(activeConvId);

    }

    let lastMessagesLength = 0;

    const unsubscribe = useConvStore.subscribe(
      (state) => ({
        activeId: state.activeConvId,
        convs: state.convs,
      }),
      ({ activeId, convs }) => {
        if (!activeId) return;

        if (activeId !== selectedConvId) {
          setSelectedConvId(activeId);
        }

        const conv = convs[activeId];
        if (!conv) return;

        const currentMessagesLength = conv.messages.length;

        if (currentMessagesLength !== lastMessagesLength) {

          lastMessagesLength = currentMessagesLength;
        }
      },
    );

    return () => unsubscribe();
  }, [panels.convTree?.isOpen, activeConvId, selectedConvId]);

  const panelConfigs: PanelConfig[] = useMemo(
    () => [
      {
        id: 'sidebar',
        position: 'left',
        size: '320px',
        minWidth: '320px',
        maxWidth: '450px',
        width: '320px',
        zIndex: 40,
        render: (isOpen) => (isOpen ? <Sidebar wsState={wsState} onReconnectClick={connect} isCollapsed={panels.sidebar?.isCollapsed} /> : null),
      },
    ],
    [wsState, connect], // Add connect to dependencies
  );

  return (
    <>
      <div 
        className="NavigationContainer"
        style={{
          backgroundColor: 'var(--global-background-color)',
          color: 'var(--global-text-color)',
          borderColor: 'var(--global-border-color)',
          fontFamily: 'var(--global-font-family)',
          fontSize: 'var(--global-font-size)',
          borderRadius: 'var(--global-border-radius)',
          boxShadow: 'var(--global-box-shadow)',
          width: '95%',
          marginLeft: 'auto',
          marginRight: 'auto',
          ...(window?.theme?.NavigationContainer?.style || {})
        }}
      >
        <PanelContainerLayout>
          <div 
            className="NavigationContainer h-full flex flex-col"
            style={{
              backgroundColor: 'var(--global-background-color)',
              color: 'var(--global-text-color)',
              borderColor: 'var(--global-border-color)',
              fontFamily: 'var(--global-font-family)',
              fontSize: 'var(--global-font-size)',
              borderRadius: 'var(--global-border-radius)',
              boxShadow: 'var(--global-box-shadow)',
              ...(window?.theme?.NavigationContainer?.style || {})
            }}
          >
            {children}
          </div>
        </PanelContainerLayout>

        <PanelManager panels={panelConfigs} />
      </div>
    </>
  );
}
