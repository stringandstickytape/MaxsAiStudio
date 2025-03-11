// src/components/navigation/NavigationContainer.tsx
import { useState, useEffect, useMemo, useCallback, ReactNode } from 'react';
import { PanelManager, type PanelConfig } from '@/components/PanelManager';
import { PanelContainerLayout } from '@/components/PanelContainerLayout';
import { cn } from '@/lib/utils';
import { Sidebar } from '../Sidebar';
import { ConvTreeView } from '@/components/ConvTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { useWebSocket } from '@/hooks/useWebSocket';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useSystemPromptManagement';
import { ToolPanel } from '@/components/tools/ToolPanel';

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



    const openPanel = useCallback((panelId: string) => {
        const panel = panels[panelId];

        if (panel && !panel.isOpen) {
            togglePanel(panelId);
        }
    }, [panels, togglePanel]);

    useEffect(() => {
        const handleOpenToolPanel = () => {
            openPanel('toolPanel');
        };

        window.addEventListener('openToolPanel', handleOpenToolPanel);
        return () => {
            window.removeEventListener('openToolPanel', handleOpenToolPanel);
        };
    }, [openPanel]);

    const handleToggleConvTree = () => {
        setSelectedConvId(activeConvId);

        console.log('Opening conv tree with conv ID:', activeConvId);

        openPanel('convTree');
    };

    useEffect(() => {
        if (activeConvId && activeConvId !== selectedConvId) {
            console.log('Active conv changed, updating selected conv ID', {
                old: selectedConvId,
                new: activeConvId
            });
            setSelectedConvId(activeConvId);
        }

        let lastMessagesLength = 0;

        const unsubscribe = useConvStore.subscribe(
            (state) => ({
                activeId: state.activeConvId,
                convs: state.convs
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
                    console.log('Conv store updated - conv messages changed:', {
                        oldCount: lastMessagesLength,
                        newCount: currentMessagesLength,
                        activeConvId: activeId
                    });

                    if (panels.convTree?.isOpen) {
                        setSelectedConvId(null);
                        setTimeout(() => {
                            setSelectedConvId(activeId);
                        }, 50);
                    }

                    lastMessagesLength = currentMessagesLength;
                }
            }
        );

        return () => unsubscribe();
    }, [panels.convTree?.isOpen, activeConvId, selectedConvId]);

    const hasLeftPanel = panels.sidebar?.isPinned || false;
    const hasRightPanel = panels.convTree?.isPinned ||
        panels.settings?.isPinned ||
        panels.systemPrompts?.isPinned ||
        panels.toolPanel?.isPinned || false;

    const panelConfigs: PanelConfig[] = useMemo(() => [
        {
            id: 'sidebar',
            position: 'left',
            size: '320px',
            minWidth: '320px',
            maxWidth: '450px',
            width: '320px',
            zIndex: 40,
            title: 'Convs',
            render: (isOpen) => isOpen ? (
                <Sidebar wsState={wsState} />
            ) : null
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
            render: (isOpen) => isOpen && selectedConvId ? (
                <ConvTreeView
                    key={`tree-${selectedConvId}-${Date.now()}`}
                    convId={selectedConvId}
                    messages={selectedConvId && convs[selectedConvId]?.messages || []}
                />
            ) : null
        },
        {
            id: 'settings',
            position: 'right',
            size: '320px',
            minWidth: '320px',
            maxWidth: '450px',
            width: '320px',
            zIndex: 40,
            title: 'Settings',
            render: (isOpen) => isOpen ? (
                <SettingsPanel />
            ) : null
        },
        {
            id: 'systemPrompts',
            position: 'right',
            size: '320px',
            minWidth: '320px',
            maxWidth: '450px',
            width: '320px',
            zIndex: 50,
            title: 'System Prompts',
            render: (isOpen) => isOpen ? (
                <SystemPromptLibrary
                    convId={activeConvId || undefined}
                    onApplyPrompt={(prompt) => {
                        console.log("Applying prompt:", prompt);
                        const convId = activeConvId;
                        const promptId = prompt?.guid || prompt?.Guid;

                        if (convId && promptId) {
                            console.log(`Setting conv system prompt with convId=${convId}, promptId=${promptId}`);
                            setConvSystemPrompt({ convId, promptId });
                            setConvPrompt(convId, promptId);
                        } else {
                            console.error("Cannot apply prompt - missing required data:", {
                                convId, promptId, prompt
                            });
                        }

                        togglePanel('systemPrompts');
                    }}
                />
            ) : null
        },
        {
            id: 'toolPanel',
            position: 'right',
            size: '320px',
            minWidth: '320px',
            maxWidth: '450px',
            width: '320px',
            zIndex: 60,
            title: 'Tool Library',
            render: (isOpen) => isOpen ? (
                <ToolPanel isOpen={isOpen} onClose={() => togglePanel('toolPanel')} />
            ) : null
        }
    ], [activeConvId, convs, selectedConvId, setConvPrompt, setConvSystemPrompt, wsState]);

    return (
        <>
            <PanelContainerLayout>
                <div className="h-full flex flex-col">
                    {children}
                </div>
            </PanelContainerLayout>

            <PanelManager panels={panelConfigs} />
        </>
    );
}