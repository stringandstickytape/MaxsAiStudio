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

interface NavigationContainerProps {
  children: ReactNode;
}

export function NavigationContainer({ children }: NavigationContainerProps) {
  const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
  const { isConnected, clientId } = useWebSocket();
  const wsState = { isConnected, clientId, messages: [] };
  
  // Zustand panel state
  const { registerPanel, togglePanel, panels } = usePanelStore();
  
  // Conversation store
  const { activeConversationId, conversations } = useConversationStore();
  
  // System prompt management
  const { setConversationSystemPrompt } = useSystemPromptManagement();
  
  // System prompt store
  const { setConversationPrompt } = useSystemPromptStore();

  // Register panels
  useEffect(() => {
    registerPanel({
      id: 'sidebar',
      position: 'left',
      size: '80',
      zIndex: 40,
      title: 'Conversations',
      isOpen: false,
      isPinned: false
    });

    registerPanel({
      id: 'conversationTree',
      position: 'right',
      size: '80',
      zIndex: 30,
      title: 'Conversation Tree',
      isOpen: false,
      isPinned: false
    });

    registerPanel({
      id: 'settings',
      position: 'right',
      size: '80',
      zIndex: 40,
      title: 'Settings',
      isOpen: false,
      isPinned: false
    });

    registerPanel({
      id: 'systemPrompts',
      position: 'right',
      size: '80',
      zIndex: 50,
      title: 'System Prompts',
      isOpen: false,
      isPinned: false
    });
  }, [registerPanel]);

  // Get panel states
  const conversationTreePanel = panels.conversationTree || { isOpen: false, isPinned: false };

  // Handle toggle conversation tree
  const handleToggleConversationTree = () => {
    // Always use the activeConversationId when toggling the tree
    setSelectedConversationId(activeConversationId);
    
    console.log('Opening conversation tree with conversation ID:', activeConversationId);
    togglePanel('conversationTree');
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
          if (conversationTreePanel.isOpen) {
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
  }, [conversationTreePanel.isOpen, activeConversationId, selectedConversationId]);

  // Define panel configurations
  const panelConfigs: PanelConfig[] = [
    {
      id: 'sidebar',
      position: 'left',
      size: '80',
      minWidth: '320px',
      maxWidth: '320px',
      width: '320px',
      zIndex: 40,
      title: 'Conversations',
      render: (isOpen) => isOpen ? <Sidebar wsState={wsState} /> : null
    },
    {
      id: 'conversationTree',
      position: 'right',
      size: '80',
      minWidth: '320px',
      maxWidth: '320px',
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
      size: '80',
      minWidth: '320px',
      maxWidth: '320px',
      width: '320px',
      zIndex: 40,
      title: 'Settings',
      render: (isOpen) => isOpen ? <SettingsPanel isOpen={true} /> : null
    },
    {
      id: 'systemPrompts',
      position: 'right',
      size: '80',
      minWidth: '320px',
      maxWidth: '320px',
      width: '320px',
      zIndex: 50,
      title: 'System Prompts',
      render: (isOpen) => isOpen ? (
        <SystemPromptLibrary
          isOpen={true}
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
  
  // Get panel states for layout calculations
  const hasLeftPanel = panels.sidebar?.isPinned || false;
  const hasRightPanel = panels.conversationTree?.isPinned || 
                       panels.settings?.isPinned || 
                       panels.systemPrompts?.isPinned || false;

  // Expose panel toggle handlers for child components
  const navigationContext = {
    toggleSidebar: () => togglePanel('sidebar'),
    toggleConversationTree: handleToggleConversationTree,
    toggleSettings: () => togglePanel('settings'),
    toggleSystemPrompts: () => togglePanel('systemPrompts'),
    hasLeftPanel,
    hasRightPanel
  };

  return (
    <>
      <div className={cn(
        "h-screen flex flex-col",
        hasLeftPanel && "pl-80",
        hasRightPanel && "pr-80"
      )}>
        {children}
      </div>
      
      {/* Panel manager */}
      <PanelManager panels={panelConfigs} />
    </>
  );
}