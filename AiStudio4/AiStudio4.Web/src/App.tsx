// src/App.tsx
import { NavigationContainer } from './components/navigation/NavigationContainer';
import { CommandInitializer } from './components/commands/CommandInitializer';
import { ChatWorkspace } from './components/chat/ChatWorkspace';
import { FontSizeProvider } from './components/FontSizeProvider';
import { useEffect } from 'react';
import { useConversationStore } from '@/stores/useConversationStore';
import { v4 as uuidv4 } from 'uuid';

function App() {
    // Get the conversation store functions
    const { createConversation, activeConversationId } = useConversationStore();
    
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
                <ChatWorkspace />
            </NavigationContainer>
        </FontSizeProvider>
    );
}

// Export the App component directly, no Redux Provider needed
export default App;