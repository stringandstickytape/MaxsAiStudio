
import { NavigationContainer } from './components/navigation/NavigationContainer';
import { CommandInitializer } from './components/commands/CommandInitializer';
import { ChatSpace } from './components/ChatSpace';
import { FontSizeProvider } from './components/FontSizeProvider';
import { CommandInitializationPlugin } from './CommandInitializationPlugin';
import { SystemPromptDialog } from './components/SystemPrompt/SystemPromptDialog';
import { UserPromptDialog } from './components/UserPrompt/UserPromptDialog';
import { ToolDialog } from './components/tools/ToolDialog';
import { useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { v4 as uuidv4 } from 'uuid';

const PANEL_EVENTS = {
  BEFORE_UNLOAD: 'beforeunload',
  VISIBILITY_CHANGE: 'visibilitychange'
};

function App() {
  
  const { createConv, activeConvId } = useConvStore();
  
  
  const checkAndFixPanelData = () => {
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        const savedPanels = JSON.parse(savedLayout);
        
        
        let needsFix = false;
        
        Object.keys(savedPanels).forEach(id => {
          
          if (savedPanels[id]?.isOpen === true && savedPanels[id]?.isPinned === false) {
            savedPanels[id].isOpen = false;
            needsFix = true;
          }
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

  return (
    <FontSizeProvider>
      
      <CommandInitializer />
      
      
      <CommandInitializationPlugin />

      
      <NavigationContainer>
        
        <ChatSpace />
      </NavigationContainer>

      
      <SystemPromptDialog />
      <UserPromptDialog />
      <ToolDialog />
    </FontSizeProvider>
  );
}

export default App;

