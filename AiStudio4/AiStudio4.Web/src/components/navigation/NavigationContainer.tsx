// src/components/navigation/NavigationContainer.tsx
import { useState, useEffect, useMemo, ReactNode } from 'react';
import { PanelManager, type PanelConfig } from '@/components/PanelManager';
import { PanelContainerLayout } from '@/components/PanelContainerLayout';
import { cn } from '@/lib/utils';
import { Sidebar } from '../Sidebar';
import { ConversationTreeView } from '@/components/ConversationTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { useWebSocket } from '@/hooks/useWebSocket';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConversationStore } from '@/stores/useConversationStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useSystemPromptManagement';
import { ToolPanel } from '@/components/tools/ToolPanel';

interface NavigationContainerProps {
    children: ReactNode;
}

export function NavigationContainer({ children }: NavigationContainerProps) {
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const [isToolPanelOpen, setIsToolPanelOpen] = useState(false);
    const { isConnected, clientId } = useWebSocket();
    const wsState = { isConnected, clientId, messages: [] };

    const { togglePanel, panels } = usePanelStore();

    const { activeConversationId, conversations } = useConversationStore();

    const { setConversationSystemPrompt } = useSystemPromptManagement();

    const { setConversationPrompt } = useSystemPromptStore();



    const openPanel = (panelId: string) => {

        const panel = panels[panelId];

        if (panel && !panel.isOpen) {
            togglePanel(panelId);
        }
    };

    useEffect(() => {
        const handleOpenToolPanel = () => {
            setIsToolPanelOpen(true);
        };

        window.addEventListener('openToolPanel', handleOpenToolPanel);
        return () => {
            window.removeEventListener('openToolPanel', handleOpenToolPanel);
        };
    }, []);

    const handleToggleConversationTree = () => {
        setSelectedConversationId(activeConversationId);

        console.log('Opening conversation tree with conversation ID:', activeConversationId);

        openPanel('conversationTree');
    };

    useEffect(() => {
        if (activeConversationId && activeConversationId !== selectedConversationId) {
            console.log('Active conversation changed, updating selected conversation ID', {
                old: selectedConversationId,
                new: activeConversationId
            });
            setSelectedConversationId(activeConversationId);
        }

        let lastMessagesLength = 0;

        const unsubscribe = useConversationStore.subscribe(
            (state) => ({
                activeId: state.activeConversationId,
                conversations: state.conversations
            }),
            ({ activeId, conversations }) => {
                if (!activeId) return;

                if (activeId !== selectedConversationId) {
                    setSelectedConversationId(activeId);
                }

                const conversation = conversations[activeId];
                if (!conversation) return;

                const currentMessagesLength = conversation.messages.length;

                if (currentMessagesLength !== lastMessagesLength) {
                    console.log('Conversation store updated - conversation messages changed:', {
                        oldCount: lastMessagesLength,
                        newCount: currentMessagesLength,
                        activeConversationId: activeId
                    });

                    if (panels.conversationTree?.isOpen) {
                        setSelectedConversationId(null);
                        setTimeout(() => {
                            setSelectedConversationId(activeId);
                        }, 50);
                    }

                    lastMessagesLength = currentMessagesLength;
                }
            }
        );

        return () => unsubscribe();
    }, [panels.conversationTree?.isOpen, activeConversationId, selectedConversationId]);

    const hasLeftPanel = panels.sidebar?.isPinned || false;
    const hasRightPanel = panels.conversationTree?.isPinned ||
        panels.settings?.isPinned ||
        panels.systemPrompts?.isPinned || false;

    const panelConfigs: PanelConfig[] = useMemo(() => [
        {
            id: 'sidebar',
            position: 'left',
            size: '320px',
            minWidth: '320px',
            maxWidth: '450px',
            width: '320px',
            zIndex: 40,
            title: 'Conversations',
            render: (isOpen) => isOpen ? (
                <Sidebar wsState={wsState} />
            ) : null
        },
        {
            id: 'conversationTree',
            position: 'right',
            size: '320px',
            minWidth: '320px',
            maxWidth: '450px',
            width: '320px',
            zIndex: 30,
            title: 'Conversation Tree',
            render: (isOpen) => isOpen && selectedConversationId ? (
                <ConversationTreeView
                    key={`tree-${selectedConversationId}-${Date.now()}`}
                    conversationId={selectedConversationId}
                    messages={selectedConversationId && conversations[selectedConversationId]?.messages || []}
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
                    conversationId={activeConversationId || undefined}
                    onApplyPrompt={(prompt) => {
                        console.log("Applying prompt:", prompt);
                        const conversationId = activeConversationId;
                        const promptId = prompt?.guid || prompt?.Guid;

                        if (conversationId && promptId) {
                            console.log(`Setting conversation system prompt with conversationId=${conversationId}, promptId=${promptId}`);
                            setConversationSystemPrompt({ conversationId, promptId });
                            setConversationPrompt(conversationId, promptId);
                        } else {
                            console.error("Cannot apply prompt - missing required data:", {
                                conversationId, promptId, prompt
                            });
                        }

                        togglePanel('systemPrompts');
                    }}
                />
            ) : null
        }
    ], [activeConversationId, conversations, selectedConversationId, setConversationPrompt, setConversationSystemPrompt, wsState]);

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