// AiStudioClient/src/App.tsx
import { NavigationContainer } from './components/navigation/NavigationContainer';
import { CommandInitializer } from './components/commands/CommandInitializer';
import { ChatSpace } from './components/ChatSpace';
import { FontSizeProvider } from './components/FontSizeProvider'; 
import { windowEventService, WindowEvents } from '@/services/windowEvents';
import { CommandInitializationPlugin } from './CommandInitializationPlugin';
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
import { useInputBarStore } from '@/stores/useInputBarStore';
import { useTipOfTheDayStore } from '@/stores/useTipOfTheDayStore';
import { useUserPromptStore } from '@/stores/useUserPromptStore';

const PANEL_EVENTS = {
  BEFORE_UNLOAD: 'beforeunload',
  VISIBILITY_CHANGE: 'visibilitychange'
};


import { webSocketService } from '@/services/websocket/WebSocketService';

function App() {
  const { createConv, activeConvId } = useConvStore();
  const { prompts: userPrompts } = useUserPromptStore();
  
  
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
  
  // Initialize tip of the day
  useEffect(() => {
    const initializeTipOfTheDay = async () => {
      const { fetchNextTip, showTip } = useTipOfTheDayStore.getState();
      
      try {
        await fetchNextTip();
        
        // Show tip on startup with a small delay to ensure app is fully loaded
        setTimeout(() => {
          showTip();
        }, 100);
      } catch (error) {
        console.error('Failed to initialize tip of the day:', error);
      }
    };
    
    initializeTipOfTheDay();
  }, []);
  
  // Remove placeholder local array and use centralized windowEventService
  // Listen for a standardized event that loads a user prompt into the input bar
  useEffect(() => {
    const unsubscribe = windowEventService.on(WindowEvents.USER_PROMPTS_UPDATED, () => {
      // No action needed here for now; kept to show how to subscribe if future logic is added
    });

    // Support explicit loading by id via a dedicated event name
    const unsubscribeLoad = windowEventService.on('load-associated-user-prompt', (detail: { userPromptId?: string }) => {
      const userPromptId = detail?.userPromptId;
      if (!userPromptId) return;
      const prompt = userPrompts.find(up => up.guid === userPromptId);
      if (prompt && prompt.content) {
        useInputBarStore.getState().setInputText(prompt.content);
      } else {
        console.warn('User prompt not found for id:', userPromptId);
      }
    });

    return () => {
      unsubscribe();
      unsubscribeLoad();
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

    </FontSizeProvider>
  );
}

export default App;