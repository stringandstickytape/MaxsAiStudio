﻿// AiStudioClient/src/App.tsx
import { NavigationContainer } from './components/navigation/NavigationContainer';
import { CommandInitializer } from './components/commands/CommandInitializer';
import { ChatSpace } from './components/ChatSpace';
import { FontSizeProvider } from './components/FontSizeProvider'; 
import { CommandInitializationPlugin } from './CommandInitializationPlugin';
// import { SystemPromptDialog } from './components/SystemPrompt/SystemPromptDialog'; // Replaced by ModalManager
// import { UserPromptDialog } from './components/UserPrompt/UserPromptDialog'; // Replaced by ModalManager
import { ToolDialog } from './components/tools/ToolDialog'; // Bridge component for legacy event handling
// import SettingsDialog from './components/modals/SettingsDialog'; // Replaced by ModalManager
// import { ThemeLibrary } from './components/ThemeLibrary'; // Replaced by ModalManager
import { ServerDialog } from './components/servers/ServerDialog'; // Bridge component for legacy event handling
import { ModalManager } from './components/modals/ModalManager'; // Import the new manager
import { useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { v4 as uuidv4 } from 'uuid';
import { useModalStore } from '@/stores/useModalStore';
// Import slash command registry and providers
import { slashItemRegistry } from './services/slashItemRegistry';
import { UserPromptProvider } from './services/providers/userPromptProvider';
import { FileNameProvider } from './services/providers/fileNameProvider';

const PANEL_EVENTS = {
  BEFORE_UNLOAD: 'beforeunload',
  VISIBILITY_CHANGE: 'visibilitychange'
};


import { webSocketService } from '@/services/websocket/WebSocketService';

function App() {
  const { createConv, activeConvId } = useConvStore();
  
  
  useEffect(() => {
    
    webSocketService.connect();
    
    return () => {
      
      webSocketService.disconnect();
    };
  }, []);
  
  // Initialize slash command providers
  useEffect(() => {
    // Register slash command providers
    slashItemRegistry.registerProvider(new UserPromptProvider());
    slashItemRegistry.registerProvider(new FileNameProvider());
    // Register other providers as needed
  }, []);
  
  const checkAndFixPanelData = () => {
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        const savedPanels = JSON.parse(savedLayout);
        
        
        let needsFix = false;
        
        
        Object.keys(savedPanels).forEach(id => {
          
        });
        
        if (needsFix) {
          localStorage.setItem('panel-layout', JSON.stringify(savedPanels));
        }
      }
    } catch (error) {
      console.error('Error checking panel data:', error);
    }
    };

    // Effect to handle opening the system prompt library modal via event
    useEffect(() => {
        const handleOpenSystemPromptLibrary = () => {
            const createAction = window.localStorage.getItem('systemPrompt_action') === 'create';
            useModalStore.getState().openModal('systemPrompt', { createNew: createAction });
            // Clear the action after using it
            if (createAction) {
                window.localStorage.removeItem('systemPrompt_action');
            }
        };
        window.addEventListener('open-system-prompt-library', handleOpenSystemPromptLibrary);
        return () => {
            window.removeEventListener('open-system-prompt-library', handleOpenSystemPromptLibrary);
        };
    }, []);

  
  useEffect(() => {
    
    checkAndFixPanelData();
    
    
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        const { panels } = usePanelStore.getState();
        const savedPanels = JSON.parse(savedLayout);

        
        Object.keys(savedPanels).forEach((panelId) => {
          if (panels[panelId]) {
            usePanelStore.setState((state) => ({
              panels: {
                ...state.panels,
                [panelId]: {
                  ...state.panels[panelId],
                  ...savedPanels[panelId],
                },
              },
            }));
          }
        });
      }
    } catch (error) {
      console.error('Failed to load panel layout:', error);
    }

    

    
    
    const handleBeforeUnload = () => {
      usePanelStore.getState().saveState();
    };
    
    
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') {
        usePanelStore.getState().saveState();
      }
    };
    


    window.addEventListener(PANEL_EVENTS.BEFORE_UNLOAD, handleBeforeUnload);
    document.addEventListener(PANEL_EVENTS.VISIBILITY_CHANGE, handleVisibilityChange);
    
    
    return () => {
      
      window.removeEventListener(PANEL_EVENTS.BEFORE_UNLOAD, handleBeforeUnload);
      document.removeEventListener(PANEL_EVENTS.VISIBILITY_CHANGE, handleVisibilityChange);
      
      
      usePanelStore.getState().saveState();
    };
  }, []);

  
  useEffect(() => {
    if (!activeConvId) {
      createConv({
        id: `conv_${Date.now()}`,
        rootMessage: {
          id: `msg_${uuidv4()}`,
          content: '',
          source: 'system',
          timestamp: Date.now(),
        },
      });
    }
  }, [activeConvId, createConv]);

  // Destructure modal state from useModalStore
  const { openModalId, closeModal } = useModalStore();

  // Assuming userPrompts is available in this scope; if not, it should be imported or passed as prop
  const userPrompts = []; // Placeholder, replace with actual user prompts source

  // Set up event listeners
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setCommandBarOpen(true);
      }
    };
    
    // Add listener for the load-associated-user-prompt event
    const handleLoadUserPrompt = (e: CustomEvent) => {
      const { userPromptId } = e.detail;
      if (userPromptId) {
        const userPrompt = userPrompts.find(up => up.guid === userPromptId);
        if (userPrompt && window.setPrompt) {
          window.setPrompt(userPrompt.content);
        }
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('load-associated-user-prompt', handleLoadUserPrompt as EventListener);
    
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('load-associated-user-prompt', handleLoadUserPrompt as EventListener);
    };
  }, [userPrompts]);

  return (
    <FontSizeProvider>
      
      <CommandInitializer />
      
      
      <CommandInitializationPlugin />

      
      <NavigationContainer>
        
        <ChatSpace />
      </NavigationContainer>

      {/* Centralized Modal Management */}
      <ModalManager />

      {/* Bridge components for legacy event handling */}
      <ToolDialog />
      <ServerDialog />

      {/* Old direct modal rendering (commented out/removed) */}
      {/* <SystemPromptDialog /> */}
      {/* <UserPromptDialog /> */}
      {/* <SettingsDialog /> */}
      {/* <ServerDialog /> */}
      {/* {openModalId === 'theme' && <ThemeLibrary open={true} onOpenChange={(open) => { */}
      {/*   if (!open) closeModal(); */}
      {/* }} />} */}
    </FontSizeProvider>
  );
}

export default App;