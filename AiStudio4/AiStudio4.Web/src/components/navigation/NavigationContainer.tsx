
import { useState, useEffect, useMemo, useCallback, ReactNode } from 'react';
import { PanelManager, type PanelConfig } from '@/components/PanelManager';
import { PanelContainerLayout } from '@/components/PanelContainerLayout';
import { Sidebar } from '../Sidebar';
import { ConvTreeView } from '@/components/ConvTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { UserPromptLibrary } from '@/components/UserPrompt/UserPromptLibrary';
import { useWebSocket } from '@/hooks/useWebSocket';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';

import { useUserPromptStore } from '@/stores/useUserPromptStore';

interface NavigationContainerProps {
  children: ReactNode;
}

export function NavigationContainer({ children }: NavigationContainerProps) {
  const [selectedConvId, setSelectedConvId] = useState<string | null>(null);
  const { isConnected, clientId } = useWebSocket();
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

          if (panels.convTree?.isOpen) {
            setSelectedConvId(null);
            setTimeout(() => {
              setSelectedConvId(activeId);
            }, 50);
          }

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
        render: (isOpen) => (isOpen ? <Sidebar wsState={wsState} /> : null),
      },
      {
        id: 'convTree',
        position: 'right',
        size: '320px',
        minWidth: '320px',
        maxWidth: '450px',
        width: '320px',
        zIndex: 30,
        title: 'Conv Tree',
        render: (isOpen) =>
          isOpen && selectedConvId ? (
            <div className="h-full overflow-auto" style={{ height: '100%' }}>
              <ConvTreeView
                key={`tree-${selectedConvId}-${Date.now()}`}
                convId={selectedConvId}
                messages={(selectedConvId && convs[selectedConvId]?.messages) || []}
              />
            </div>
          ) : null,
      },
      /* Note: convTree panel still exists on right side for users who prefer it there */
      {
        id: 'settings',
        position: 'right',
        size: '320px',
        minWidth: '320px',
        maxWidth: '450px',
        width: '320px',
        zIndex: 40,
        title: 'Settings',
        render: (isOpen) => (isOpen ? <SettingsPanel /> : null),
      },
      
    ],
    [activeConvId, convs, selectedConvId, setConvPrompt, setConvSystemPrompt, wsState],
  );

  return (
    <>
      <PanelContainerLayout>
        <div className="h-full flex flex-col">{children}</div>
      </PanelContainerLayout>

      <PanelManager panels={panelConfigs} />
    </>
  );
}

