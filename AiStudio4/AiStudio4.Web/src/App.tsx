// src/App.tsx
import { NavigationContainer } from './components/navigation/NavigationContainer';
import { CommandInitializer } from './components/commands/CommandInitializer';
import { ChatSpace } from './components/ChatSpace';
import { FontSizeProvider } from './components/FontSizeProvider';
import { useEffect } from 'react';
import { useConvStore } from '@/stores/useConvStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { v4 as uuidv4 } from 'uuid';

// Event name constants for better consistency
const PANEL_EVENTS = {
  BEFORE_UNLOAD: 'beforeunload',
  VISIBILITY_CHANGE: 'visibilitychange'
};

function App() {
  // Get the conv store functions
  const { createConv, activeConvId } = useConvStore();
  
  // Function to check for stale panel data
  const checkAndFixPanelData = () => {
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        const savedPanels = JSON.parse(savedLayout);
        const { panels } = usePanelStore.getState();
        
        // Look for panels that should be closed but aren't
        let needsFix = false;
        
        Object.keys(savedPanels).forEach(id => {
          // If a panel is not pinned but is marked as open in saved state, fix it
          if (savedPanels[id]?.isOpen === true && savedPanels[id]?.isPinned === false) {
            console.warn(`Found stale panel state for ${id} - fixing...`);
            savedPanels[id].isOpen = false;
            needsFix = true;
          }
        });
        
        if (needsFix) {
          localStorage.setItem('panel-layout', JSON.stringify(savedPanels));
          console.log('Fixed and saved corrected panel state');
        }
      }
    } catch (error) {
      console.error('Error checking panel data:', error);
    }
  };

  // Initialize panel persistence
  useEffect(() => {
    // First check and fix any stale panel data
    checkAndFixPanelData();
    
    // Load panel layout from local storage directly
    try {
      const savedLayout = localStorage.getItem('panel-layout');
      if (savedLayout) {
        console.log('%c📋 Loading saved panel layout from localStorage', 'color: #8b5cf6; font-weight: bold');
        const { panels } = usePanelStore.getState();
        const savedPanels = JSON.parse(savedLayout);
        
        console.log('Saved panels state:', savedPanels);
        console.log('Current panels state before merge:', panels);

        // Merge saved state into current panels
        Object.keys(savedPanels).forEach((panelId) => {
          if (panels[panelId]) {
            console.log(`Merging saved state for panel ${panelId}:`, {
              current: { isOpen: panels[panelId].isOpen, isPinned: panels[panelId].isPinned },
              saved: { isOpen: savedPanels[panelId].isOpen, isPinned: savedPanels[panelId].isPinned }
            });
            
            usePanelStore.setState((state) => ({
              panels: {
                ...state.panels,
                [panelId]: {
                  ...state.panels[panelId],
                  ...savedPanels[panelId],
                },
              },
            }));
          } else {
            console.log(`Panel ${panelId} from saved state not found in current panels`);
          }
        });
        
        // Enable debug tools
        usePanelStore.getState().debugPanelState();
      }
    } catch (error) {
      console.error('Failed to load panel layout:', error);
    }

    // We'll save on specific actions instead of using a timer

    // Save panel layout on unmount
    // Setup event listeners for saving panel state when page is about to be closed
    const handleBeforeUnload = () => {
      usePanelStore.getState().saveState();
    };
    
    // Save state when tab visibility changes (user switches tabs/minimizes)
    const handleVisibilityChange = () => {
      if (document.visibilityState === 'hidden') {
        usePanelStore.getState().saveState();
      }
    };
    
    // Add event listeners
    window.addEventListener(PANEL_EVENTS.BEFORE_UNLOAD, handleBeforeUnload);
    document.addEventListener(PANEL_EVENTS.VISIBILITY_CHANGE, handleVisibilityChange);
    
    // Cleanup function
    return () => {
      // Remove event listeners
      window.removeEventListener(PANEL_EVENTS.BEFORE_UNLOAD, handleBeforeUnload);
      document.removeEventListener(PANEL_EVENTS.VISIBILITY_CHANGE, handleVisibilityChange);
      
      // Final save on unmount
      usePanelStore.getState().saveState();
    };
  }, []);

  // Create initial conv if needed
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
      {/* Command initializer component to set up all commands */}
      <CommandInitializer />

      {/* Navigation container handles panels and routing */}
      <NavigationContainer>
        {/* Chat workspace manages the chat interface */}
        <ChatSpace />
      </NavigationContainer>
    </FontSizeProvider>
  );
}

// Export the App component directly, no Redux Provider needed
export default App;