// AiStudio4.Web\src\components\navigation\NavigationContainer.tsx
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
export const themeableProps = {
  backgroundColor: {
    cssVar: '--navigationcontainer-bg',
    description: 'Background color of the navigation container',
    default: 'transparent',
  },
  textColor: {
    cssVar: '--navigationcontainer-text-color',
    description: 'Text color for the navigation container',
    default: 'inherit',
  },
  borderColor: {
    cssVar: '--navigationcontainer-border-color',
    description: 'Border color for the navigation container',
    default: 'transparent',
  },
  accentColor: {
    cssVar: '--navigationcontainer-accent-color',
    description: 'Accent color for the navigation container highlights',
    default: '#3b82f6',
  },
  style: {
    description: 'Arbitrary CSS style for the NavigationContainer root',
    default: {},
  },
};

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
        render: (isOpen) => (isOpen ? <Sidebar wsState={wsState} onReconnectClick={connect} /> : null),
      },
    ],
    [wsState, connect], // Add connect to dependencies
  );

  return (
    <>
      <div 
        className="NavigationContainer"
        style={{
          backgroundColor: 'var(--navigationcontainer-bg, transparent)',
          color: 'var(--navigationcontainer-text-color, inherit)',
          borderColor: 'var(--navigationcontainer-border-color, transparent)',
          ...(window?.theme?.NavigationContainer?.style || {})
        }}
      >
        <PanelContainerLayout>
          <div 
            className="NavigationContainer h-full flex flex-col"
            style={{
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
