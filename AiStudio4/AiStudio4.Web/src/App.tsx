import { useState, useEffect } from "react";
import "./App.css";
import { useMediaQuery } from '@/hooks/use-media-query';
import { Provider } from 'react-redux';
import { store } from './store/store';
import { X, Pin, PinOff } from 'lucide-react';
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
    const [secondaryModel, setSecondaryModel] = useState<string>("Select Model");
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { wsState } = useWebSocketState(); // Destructure only wsState
    const { streamTokens } = useLiveStream();   // Use the new hook
    const [showConversationTree, setShowConversationTree] = useState(false);
    const [showSettings, setShowSettings] = useState(false);
    const [showSidebar, setShowSidebar] = useState(false); // Hidden by default
    const [sidebarPinned, setSidebarPinned] = useState(false); // Controls if sidebar is pinned
    const [conversationTreePinned, setConversationTreePinned] = useState(false); // Controls if conversation tree is pinned
    const [settingsPanelPinned, setSettingsPanelPinned] = useState(false); // Controls if settings panel is pinned
    
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    // We'll get the conversations directly from the store instead of using useSelector

    useEffect(() => {
       const initialize = async () => {
            try {
                const { models: availableModels, defaultModel, secondaryModel } = await ChatService.fetchModels();
                setModels(availableModels);
                
                // Set the default model if available
                if (defaultModel && defaultModel.length > 0) {
                    setSelectedModel(defaultModel);
                }
                
                // Set the secondary model if available
                if (secondaryModel && secondaryModel.length > 0) {
                    setSecondaryModel(secondaryModel);
                }

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
            console.log('Opening conversation tree with conversation ID:', activeConversationId);
        }
        setShowConversationTree(!showConversationTree);
        // Close settings if opening conversation tree and it's not pinned
        if (!showConversationTree && !settingsPanelPinned) setShowSettings(false);
    };
    
    // Subscribe to Redux store to update the conversation tree when messages change
    useEffect(() => {
        let lastMessagesLength = 0;
        let lastActiveConversation = '';
        
        const unsubscribe = store.subscribe(() => {
            const state = store.getState();
            const activeConversationId = state.conversations.activeConversationId;
            
            if (!activeConversationId || !selectedConversationId) return;
            
            // Get current conversation messages
            const conversation = state.conversations.conversations[activeConversationId];
            if (!conversation) return;
            
            const currentMessagesLength = conversation.messages.length;
            
            // Only refresh when message count changes or active conversation changes
            if (currentMessagesLength !== lastMessagesLength || 
                activeConversationId !== lastActiveConversation) {
                
                console.log('Redux store updated - conversation messages changed:', {
                    oldCount: lastMessagesLength,
                    newCount: currentMessagesLength,
                    activeConversationId
                });
                
                // Force a refresh of the tree view by briefly setting to null and back
                if (showConversationTree || conversationTreePinned) {
                    setSelectedConversationId(null);
                    setTimeout(() => {
                        setSelectedConversationId(activeConversationId);
                    }, 50);
                }
                
                // Update tracking variables
                lastMessagesLength = currentMessagesLength;
                lastActiveConversation = activeConversationId;
            }
        });
        
        return () => unsubscribe();
    }, [showConversationTree, conversationTreePinned, selectedConversationId]);
    
    const handleToggleSettings = () => {
        setShowSettings(!showSettings);
        // Close conversation tree if opening settings and it's not pinned
        if (!showSettings && !conversationTreePinned) setShowConversationTree(false);
    };

    const handleToggleSidebar = () => {
        setShowSidebar(!showSidebar);
    };

    const handleOpenNewWindow = () => {
        window.open(window.location.href, '_blank');
    };

    return (
        <Provider store={store}>
            <div className={cn(
                "h-screen flex flex-col overflow-hidden",
                sidebarPinned && "pl-80", // Add left padding when sidebar is pinned
                conversationTreePinned && "pr-80", // Add right padding when conversation tree is pinned
                settingsPanelPinned && "pr-80" // Add right padding when settings panel is pinned
            )}>
                {/* Left sidebar with slide-in/out animation */}
                <div className={cn(
                    "fixed top-0 left-0 bottom-0 w-80 bg-gray-900 border-r border-gray-700/50 shadow-xl z-40 transition-all duration-300",
                    (showSidebar || sidebarPinned) ? "translate-x-0" : "-translate-x-full"
                )}>
                    {/* Close button moved to Sidebar component */}
                    <div className="mt-2">
                        <Sidebar 
                            wsState={wsState} 
                            isPinned={sidebarPinned} 
                            onTogglePin={() => setSidebarPinned(!sidebarPinned)}
                            onClose={handleToggleSidebar} 
                        />
                    </div>
                </div>

                {/* Top fixed header pane */}
                <div className="flex-none w-full bg-background border-b">
                    <AppHeader
                        isMobile={isMobile}
                        selectedModel={selectedModel}
                        secondaryModel={secondaryModel}
                        models={models}
                        onToggleSidebar={handleToggleSidebar}
                        onModelSelect={setSelectedModel}
                        onSecondaryModelSelect={setSecondaryModel}
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
                (showConversationTree && selectedConversationId) || conversationTreePinned ? "translate-x-0" : "translate-x-full"
            )}>
                {(showConversationTree && selectedConversationId || conversationTreePinned) && (
                    <>
                        <div className="flex justify-between p-3 border-b border-gray-700 bg-[#1f2937]">
                            <div className="flex space-x-2">
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => setConversationTreePinned(!conversationTreePinned)}
                                    className="text-gray-400 hover:text-gray-100"
                                >
                                    {conversationTreePinned ? (
                                        <PinOff className="h-4 w-4" />
                                    ) : (
                                        <Pin className="h-4 w-4" />
                                    )}
                                </Button>
                                {!conversationTreePinned && (
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={handleToggleConversationTree}
                                        className="text-gray-400 hover:text-gray-100"
                                    >
                                        <X className="h-4 w-4" />
                                    </Button>
                                )}
                            </div>
                            <h2 className="text-gray-100 text-lg font-semibold flex items-center">Conversation Tree</h2>
                        </div>
                        <ConversationTreeView
                            key={`tree-${selectedConversationId}-${Date.now()}`} // Force re-render when id changes or is refreshed
                            conversationId={selectedConversationId}
                            isPinned={conversationTreePinned}
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
                showSettings || settingsPanelPinned ? "translate-x-0" : "translate-x-full"
            )}>
                {(showSettings || settingsPanelPinned) && (
                    <>
                        <div className="flex justify-between p-3 border-b border-gray-700 bg-[#1f2937]">
                            <div className="flex space-x-2">
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => setSettingsPanelPinned(!settingsPanelPinned)}
                                    className="text-gray-400 hover:text-gray-100"
                                >
                                    {settingsPanelPinned ? (
                                        <PinOff className="h-4 w-4" />
                                    ) : (
                                        <Pin className="h-4 w-4" />
                                    )}
                                </Button>
                                {!settingsPanelPinned && (
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={handleToggleSettings}
                                        className="text-gray-400 hover:text-gray-100"
                                    >
                                        <X className="h-4 w-4" />
                                    </Button>
                                )}
                            </div>
                            <h2 className="text-gray-100 text-lg font-semibold flex items-center">Settings</h2>
                        </div>
                        <SettingsPanel 
                            isOpen={showSettings || settingsPanelPinned}
                            isPinned={settingsPanelPinned}
                            onClose={!settingsPanelPinned ? handleToggleSettings : undefined}
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