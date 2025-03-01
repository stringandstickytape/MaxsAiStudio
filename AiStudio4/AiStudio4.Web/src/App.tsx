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
import { useLiveStream } from '@/hooks/useLiveStream';
import { ChatService, ModelType } from '@/services/ChatService';  // Import the updated type
import { cn } from '@/lib/utils';
import { ConversationTreeView } from '@/components/ConversationTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { buildMessageTree } from '@/utils/treeUtils';
import { commandRegistry } from './commands/commandRegistry';
import { initializeCoreCommands } from './commands/coreCommands';
import { initializeModelCommands } from '@/plugins/modelCommands';
import { CommandBar } from './components/CommandBar';

// Define a type for model settings
interface ModelSettings {
    primary: string;
    secondary: string;
}

// Create an inner component that uses Redux hooks
function AppContent() {
    const [models, setModels] = useState<string[]>([]);
    const [modelSettings, setModelSettings] = useState<ModelSettings>({
        primary: "Select Model",
        secondary: "Select Model"
    });
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { wsState } = useWebSocketState();
    const { streamTokens } = useLiveStream();
    const [showConversationTree, setShowConversationTree] = useState(false);
    const [showSettings, setShowSettings] = useState(false);
    const [showSidebar, setShowSidebar] = useState(false);
    const [sidebarPinned, setSidebarPinned] = useState(false);
    const [conversationTreePinned, setConversationTreePinned] = useState(false);
    const [settingsPanelPinned, setSettingsPanelPinned] = useState(false);
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);

    // In your AppContent component, add this near the beginning:
    useEffect(() => {
        // Initialize core commands with handlers
        initializeCoreCommands({
            toggleSidebar: handleToggleSidebar,
            toggleConversationTree: handleToggleConversationTree,
            toggleSettings: handleToggleSettings,
            openNewWindow: handleOpenNewWindow
        });
        // Initialize model commands
        initializeModelCommands({
            onModelSelect: handleModelSelect,
            getAvailableModels: () => models
        });

        // You can register additional commands here
    }, [models]);

    useEffect(() => {
        const initialize = async () => {
            try {
                const { models: availableModels, defaultModel, secondaryModel } = await ChatService.fetchModels();
                setModels(availableModels);

                // Set both models at once using the new state structure
                setModelSettings({
                    primary: defaultModel && defaultModel.length > 0 ? defaultModel : "Select Model",
                    secondary: secondaryModel && secondaryModel.length > 0 ? secondaryModel : "Select Model"
                });

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

    // Unified handler for model selection
    const handleModelSelect = (modelType: ModelType, modelName: string) => {
        setModelSettings(prev => ({
            ...prev,
            [modelType]: modelName
        }));
        // Note: The model persistence is now handled in the ModelSelector component
    };

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

    // Add global keyboard shortcut listener for Command+K or Ctrl+K
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            // Check for Ctrl+K or Command+K (Mac)
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault(); // Prevent default browser behavior
                setIsCommandBarOpen(prev => !prev);
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, []);

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
                        selectedModel={modelSettings.primary}
                        secondaryModel={modelSettings.secondary}
                        models={models}
                        onToggleSidebar={handleToggleSidebar}
                        onModelSelect={(model) => handleModelSelect('primary', model)}
                        onSecondaryModelSelect={(model) => handleModelSelect('secondary', model)}
                        onToggleConversationTree={handleToggleConversationTree}
                        onToggleSettings={handleToggleSettings}
                        onOpenNewWindow={handleOpenNewWindow}
                        isCommandBarOpen={isCommandBarOpen}
                        setIsCommandBarOpen={setIsCommandBarOpen}
                        CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
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
                    <InputBar selectedModel={modelSettings.primary} />
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