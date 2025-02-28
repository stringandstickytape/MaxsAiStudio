import { useState, useEffect } from "react";
import "./App.css";
import { useMediaQuery } from '@/hooks/use-media-query';
import { Provider } from 'react-redux';
import { store } from './store/store';
import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { createConversation } from './store/conversationSlice';
import { AppHeader } from './components/AppHeader';
import { ChatContainer } from './components/ChatContainer';
import { InputBar } from './components/input-bar';
import { Sidebar } from './components/Sidebar';
import { useWebSocketState } from './hooks/useWebSocketState';
import { useLiveStream } from '@/hooks/useLiveStream'; // Import useLiveStream
import { ChatService } from '@/services/ChatService';
import { cn } from '@/lib/utils';
import { ConversationTreeView } from '@/components/ConversationTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { buildMessageTree } from '@/utils/treeUtils';

// Create an inner component that uses Redux hooks

function AppContent() {
    const [models, setModels] = useState<string[]>([]);
    const [selectedModel, setSelectedModel] = useState<string>("Select Model");
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { wsState } = useWebSocketState(); // Destructure only wsState
    const { streamTokens } = useLiveStream();   // Use the new hook
    const [showConversationTree, setShowConversationTree] = useState(false);
    const [showSettings, setShowSettings] = useState(false);
    const [showSidebar, setShowSidebar] = useState(false); // Hidden by default
    
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    // We'll get the conversations directly from the store instead of using useSelector

    useEffect(() => {
       const initialize = async () => {
            try {
                const availableModels = await ChatService.fetchModels();
                setModels(availableModels);

                const conversationId = `conv_${Date.now()}`;
                store.dispatch(createConversation({
                    id: conversationId,
                    rootMessage: {
                        id: `msg_${Date.now()}`,
                        content: '',
                        source: 'system',
                        timestamp: Date.now()
                    }
                }));
            } catch (error) {
                console.error("Error during initialization:", error);
                setModels([]);
            }
        };
        initialize();
    }, []);
    
    const handleToggleConversationTree = () => {
        if (!showConversationTree) {
            // When opening the tree, use the active conversation ID
            const state = store.getState();
            const activeConversationId = state.conversations.activeConversationId;
            setSelectedConversationId(activeConversationId);
        }
        setShowConversationTree(!showConversationTree);
        // Close settings if opening conversation tree
        if (!showConversationTree) setShowSettings(false);
    };
    
    const handleToggleSettings = () => {
        setShowSettings(!showSettings);
        // Close conversation tree if opening settings
        if (!showSettings) setShowConversationTree(false);
    };

    const handleToggleSidebar = () => {
        setShowSidebar(!showSidebar);
    };

    const handleOpenNewWindow = () => {
        window.open(window.location.href, '_blank');
    };

    return (
        <Provider store={store}>
            <div className="h-screen flex flex-col overflow-hidden">
                {/* Left sidebar with slide-in/out animation */}
                <div className={cn(
                    "fixed top-0 left-0 bottom-0 w-80 bg-gray-900 border-r border-gray-700/50 shadow-xl z-40 transition-transform duration-300",
                    showSidebar ? "translate-x-0" : "-translate-x-full"
                )}>
                    <div className="flex justify-end p-3">
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={handleToggleSidebar}
                            className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg z-50"
                        >
                            <X className="h-5 w-5" />
                        </Button>
                    </div>
                    <div className="mt-2">
                        <Sidebar wsState={wsState} />
                    </div>
                </div>

                {/* Top fixed header pane */}
                <div className="flex-none w-full bg-background border-b">
                    <AppHeader
                        isMobile={isMobile}
                        selectedModel={selectedModel}
                        models={models}
                        onToggleSidebar={handleToggleSidebar}
                        onModelSelect={setSelectedModel}
                        onToggleConversationTree={handleToggleConversationTree}
                        onToggleSettings={handleToggleSettings}
                        onOpenNewWindow={handleOpenNewWindow}
                    />
                </div>

                {/* Middle dynamic height pane */}
                <div className="flex-1 overflow-auto">
                    <ChatContainer
                        streamTokens={streamTokens} 
                        isMobile={isMobile}
                    />
                </div>

                {/* Bottom fixed pane */}
                <div className="flex-none w-full bg-background border-t">
                    <InputBar selectedModel={selectedModel} />
                </div>
            </div>
            
            {/* Right-side slideovers with smooth transitions */}
            <div className={cn(
                "fixed top-0 right-0 bottom-0 w-80 bg-gray-900 border-l border-gray-700/50 shadow-xl z-30 transition-transform duration-300",
                showConversationTree && selectedConversationId ? "translate-x-0" : "translate-x-full"
            )}>
                {showConversationTree && selectedConversationId && (
                    <>
                        <div className="flex justify-end p-3">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={handleToggleConversationTree}
                                className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg z-50"
                            >
                                <X className="h-5 w-5" />
                            </Button>
                        </div>
                        <ConversationTreeView
                            conversationId={selectedConversationId}
                            messages={{
                            id: selectedConversationId,
                            text: "Root",
                            children: buildMessageTree(store.getState().conversations.conversations[selectedConversationId]?.messages || [], false)
                        }}
                        />
                    </>
                )}
            </div>
            
            {/* Settings panel with higher z-index so it appears on top */}
            <div className={cn(
                "fixed top-0 right-0 bottom-0 w-80 bg-gray-900 border-l border-gray-700/50 shadow-xl z-40 transition-transform duration-300",
                showSettings ? "translate-x-0" : "translate-x-full"
            )}>
                {showSettings && (
                    <>
                        <div className="flex justify-end p-3">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={handleToggleSettings}
                                className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg z-50"
                            >
                                <X className="h-5 w-5" />
                            </Button>
                        </div>
                        <SettingsPanel 
                            isOpen={showSettings}
                            onClose={handleToggleSettings}
                        />
                    </>
                )}
            </div>
        </Provider>
    );
}

// Wrapper component that provides the Redux store
function App() {
  return (
    <Provider store={store}>
      <AppContent />
    </Provider>
  );
}

export default App;