// src/components/navigation/NavigationContainer.tsx
import { useState, useEffect, ReactNode } from 'react';
import { PanelManager, type PanelConfig } from '@/components/PanelManager';
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
  
  // Zustand panel state
  const { togglePanel, panels } = usePanelStore();
  
  // Conversation store
  const { activeConversationId, conversations } = useConversationStore();
  
  // System prompt management
  const { setConversationSystemPrompt } = useSystemPromptManagement();
  
  // System prompt store
  const { setConversationPrompt } = useSystemPromptStore();



  const openPanel = (panelId: string) => {
    
    // Get the current panel state
    const panel = panels[panelId];
    
    // If panel exists and is not open, toggle it
    if (panel && !panel.isOpen) {
      togglePanel(panelId);
    } 
  };

  // Listen for tool panel open events
  useEffect(() => {
    const handleOpenToolPanel = () => {
      setIsToolPanelOpen(true);
    };

    window.addEventListener('openToolPanel', handleOpenToolPanel);
    return () => {
      window.removeEventListener('openToolPanel', handleOpenToolPanel);
    };
  }, []);

  // Handle toggle conversation tree
  const handleToggleConversationTree = () => {
    // Always use the activeConversationId when toggling the tree
    setSelectedConversationId(activeConversationId);
    
    console.log('Opening conversation tree with conversation ID:', activeConversationId);

    openPanel('conversationTree');
  };

  // Subscribe to Zustand store to update the conversation tree when messages change
  useEffect(() => {
    // Keep selectedConversationId synchronized with activeConversationId
    if (activeConversationId && activeConversationId !== selectedConversationId) {
      console.log('Active conversation changed, updating selected conversation ID', {
        old: selectedConversationId,
        new: activeConversationId
      });
      setSelectedConversationId(activeConversationId);
    }
    
    // Track message changes to refresh the tree when needed
    let lastMessagesLength = 0;
    
    // Set up subscription to conversation messages changes
    const unsubscribe = useConversationStore.subscribe(
      (state) => ({ 
        activeId: state.activeConversationId, 
        conversations: state.conversations 
      }),
      ({ activeId, conversations }) => {
        if (!activeId) return;
        
        // Update selectedConversationId to match activeConversationId if they differ
        if (activeId !== selectedConversationId) {
          setSelectedConversationId(activeId);
        }
        
        // Get current conversation messages
        const conversation = conversations[activeId];
        if (!conversation) return;
        
        const currentMessagesLength = conversation.messages.length;
        
        // Only refresh when message count changes 
        if (currentMessagesLength !== lastMessagesLength) {
          console.log('Conversation store updated - conversation messages changed:', {
            oldCount: lastMessagesLength,
            newCount: currentMessagesLength,
            activeConversationId: activeId
          });
          
          // Force a refresh of the tree view by briefly setting to null and back
          if (panels.conversationTree?.isOpen) {
            setSelectedConversationId(null);
            setTimeout(() => {
              setSelectedConversationId(activeId);
            }, 50);
          }
          
          // Update tracking variable
          lastMessagesLength = currentMessagesLength;
        }
      }
    );

    return () => unsubscribe();
  }, [panels.conversationTree?.isOpen, activeConversationId, selectedConversationId]);

  // Get panel states for layout calculations
  const hasLeftPanel = panels.sidebar?.isPinned || false;
  const hasRightPanel = panels.conversationTree?.isPinned || 
                       panels.settings?.isPinned || 
                       panels.systemPrompts?.isPinned || false;

  // Define panel configurations for the PanelManager
  const panelConfigs: PanelConfig[] = [
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
  ];

  return (
    <>
      <div className={cn(
        "h-screen flex flex-col",
        hasLeftPanel && "pl-80",
        hasRightPanel && "pr-80"
      )}>
        {children}
      </div>
      
      {/* Panel Manager to handle all panels */}
      <PanelManager panels={panelConfigs} />

      {/* ToolPanel does NOT go here, it's a dialog not a panel :/ */}
    </>
  );
}