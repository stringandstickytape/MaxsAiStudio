// src/App.tsx
import { NavigationContainer } from './components/navigation/NavigationContainer';
import { CommandInitializer } from './components/commands/CommandInitializer';
import { ChatSpace } from './components/ChatSpace';
import { FontSizeProvider } from './components/FontSizeProvider';
import { useEffect } from 'react';
import { useConversationStore } from '@/stores/useConversationStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { v4 as uuidv4 } from 'uuid';

function App() {
    // Get the conversation store functions
    const { createConversation, activeConversationId } = useConversationStore();
    
    // Initialize panel persistence
    useEffect(() => {
        // Load panel layout from local storage directly
        try {
            const savedLayout = localStorage.getItem('panel-layout');
            if (savedLayout) {
                const { panels } = usePanelStore.getState();
                const savedPanels = JSON.parse(savedLayout);
                
                // Merge saved state into current panels
                Object.keys(savedPanels).forEach(panelId => {
                    if (panels[panelId]) {
                        usePanelStore.setState(state => ({
                            panels: {
                                ...state.panels,
                                [panelId]: {
                                    ...state.panels[panelId],
                                    ...savedPanels[panelId]
                                }
                            }
                        }));
                    }
                });
            }
        } catch (error) {
            console.error('Failed to load panel layout:', error);
        }
        
        // Save panel layout on unmount
        return () => {
            try {
                const { panels } = usePanelStore.getState();
                localStorage.setItem('panel-layout', JSON.stringify(panels));
            } catch (error) {
                console.error('Failed to save panel layout:', error);
            }
        };
    }, []);
    
    // Create initial conversation if needed
    useEffect(() => {
        if (!activeConversationId) {
            createConversation({
                id: `conv_${Date.now()}`,
                rootMessage: {
                    id: `msg_${uuidv4()}`,
                    content: '',
                    source: 'system',
                    timestamp: Date.now()
                }
            });
        }
    }, [activeConversationId, createConversation]);

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