import { useState, useEffect } from "react";
import { useDispatch } from 'react-redux';

import { useMediaQuery } from '@/hooks/use-media-query';
import { Provider } from 'react-redux';
import { store } from './store/store';
import { X, Pin, PinOff, Tool as ToolIcon, MessageSquare } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { createConversation } from './store/conversationSlice';
import { AppHeader } from './components/AppHeader';
import { ChatContainer } from './components/ChatContainer';
import { InputBar } from './components/input-bar';
import { Sidebar } from './components/Sidebar';
import { useWebSocket } from './hooks/useWebSocket';
import { useLiveStream } from '@/hooks/useLiveStream';
import { ChatService, ModelType } from '@/services/ChatService';
import { cn } from '@/lib/utils';
import { ConversationTreeView } from '@/components/ConversationTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { buildMessageTree } from '@/utils/treeUtils';
import { commandRegistry } from './commands/commandRegistry';
import { initializeCoreCommands } from './commands/coreCommands';
import { initializeModelCommands } from '@/plugins/modelCommands';
import { CommandBar } from './components/CommandBar';
import { VoiceInputOverlay } from '@/components/VoiceInputOverlay';
import { useVoiceInputState, initializeVoiceInputCommand, setupVoiceInputKeyboardShortcut } from '@/commands/voiceInputCommand';
import { initializeVoiceCommands } from '@/plugins/voiceCommands';
import { initializeToolCommands } from './commands/toolCommands';
import { ToolPanel } from '@/components/tools/ToolPanel';
import { useToolCommands } from '@/hooks/useToolCommands';
import { fetchTools } from '@/store/toolSlice';
import { initializeSystemPromptCommands } from './commands/systemPromptCommands';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { fetchSystemPrompts, setConversationSystemPrompt } from './store/systemPromptSlice';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';

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
    const { isConnected, clientId } = useWebSocket();
    const wsState = { isConnected, clientId, messages: [] };
    const { streamTokens } = useLiveStream();
    const [showConversationTree, setShowConversationTree] = useState(false);
    const [showSettings, setShowSettings] = useState(false);
    const [showSidebar, setShowSidebar] = useState(false);
    const [showSystemPrompts, setShowSystemPrompts] = useState(false);
    const [sidebarPinned, setSidebarPinned] = useState(false);
    const [conversationTreePinned, setConversationTreePinned] = useState(false);
    const [settingsPanelPinned, setSettingsPanelPinned] = useState(false);
    const [systemPromptsPinned, setSystemPromptsPinned] = useState(false);
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
    const [inputValue, setInputValue] = useState(''); // Add this state for voice input
    const [isToolPanelOpen, setIsToolPanelOpen] = useState(false);
    const [promptToEdit, setPromptToEdit] = useState<string | null>(null);
    
    // Use the tool commands hook to set up tool-related commands
    const toolCommands = useToolCommands({
        openToolPanel: () => setIsToolPanelOpen(true),
        createNewTool: () => {
            setIsToolPanelOpen(true);
            window.localStorage.setItem('toolPanel_action', 'create');
        },
        importTools: () => {
            setIsToolPanelOpen(true);
            window.localStorage.setItem('toolPanel_action', 'import');
        },
        exportTools: () => {
            setIsToolPanelOpen(true);
            window.localStorage.setItem('toolPanel_action', 'export');
        }
    });

    const dispatch = useDispatch();

    // Voice input integration
    const { isVoiceInputOpen, setVoiceInputOpen, handleTranscript } = useVoiceInputState(
        (text) => {
            // This function handles the transcript when voice input is done
            setInputValue(text);
            // If you want to automatically send the message:
            // handleSendMessage(text);
        }
    );

    // Initialize commands including voice commands
    useEffect(() => {
        // Initialize core commands with handlers
        initializeCoreCommands({
            toggleSidebar: handleToggleSidebar,
            toggleConversationTree: handleToggleConversationTree,
            toggleSettings: handleToggleSettings,
            openNewWindow: handleOpenNewWindow
        });
        
        // Initialize system prompt commands
        initializeSystemPromptCommands({
            toggleLibrary: handleToggleSystemPrompts,
            createNewPrompt: () => {
                setPromptToEdit(null);
                setShowSystemPrompts(true);
            },
            editPrompt: (promptId) => {
                setPromptToEdit(promptId);
                setShowSystemPrompts(true);
            }
        });

        // Initialize model commands
        initializeModelCommands({
            onModelSelect: handleModelSelect,
            getAvailableModels: () => models
        });

        // Initialize voice commands
        initializeVoiceCommands();
        
        // Initialize tool commands - this is now moved to useToolCommands hook
        /*initializeToolCommands({
            openToolPanel: () => setIsToolPanelOpen(true),
            createNewTool: () => {
                setIsToolPanelOpen(true);
                // We'll trigger the "new tool" mode in the tool panel
                window.localStorage.setItem('toolPanel_action', 'create');
            },
            importTools: () => {
                setIsToolPanelOpen(true);
                window.localStorage.setItem('toolPanel_action', 'import');
            },
            exportTools: () => {
                setIsToolPanelOpen(true);
                window.localStorage.setItem('toolPanel_action', 'export');
            }
        });*/

        // Register all system prompts as commands
        const systemPromptsUpdated = () => {
            registerSystemPromptsAsCommands(handleToggleSystemPrompts);
        };
        
        // Initial registration
        systemPromptsUpdated();
        
        // Set up subscription to system prompts changes
        const unsubscribeFromStore = store.subscribe(() => {
            const prevPrompts = store.getState().systemPrompts.prompts;
            const currentPrompts = store.getState().systemPrompts.prompts;
            
            if (prevPrompts !== currentPrompts) {
                systemPromptsUpdated();
            }
        });
        
        // Set up voice input keyboard shortcut
        const cleanupKeyboardShortcut = setupVoiceInputKeyboardShortcut();

        return () => {
            cleanupKeyboardShortcut();
            unsubscribeFromStore();
        };
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

                // Fetch tools and system prompts during initialization
                dispatch(fetchTools());
                dispatch(fetchSystemPrompts());

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
    }, [dispatch]); // Make sure to include dispatch in the dependency array

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
        // Close other panels if they're not pinned
        if (!showConversationTree) {
            if (!settingsPanelPinned) setShowSettings(false);
            if (!systemPromptsPinned) setShowSystemPrompts(false);
        }
    };
    
    const handleToggleSystemPrompts = () => {
        setShowSystemPrompts(!showSystemPrompts);
        // Close other panels if they're not pinned
        if (!showSystemPrompts) {
            if (!settingsPanelPinned) setShowSettings(false);
            if (!conversationTreePinned) setShowConversationTree(false);
        }
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
        // Close other panels if they're not pinned
        if (!showSettings) {
            if (!conversationTreePinned) setShowConversationTree(false);
            if (!systemPromptsPinned) setShowSystemPrompts(false);
        }
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
        <>
            <div className={cn(
                "h-screen flex flex-col",
                sidebarPinned && "pl-80",
                conversationTreePinned && "pr-80",
                settingsPanelPinned && "pr-80",
                systemPromptsPinned && "pr-80"
            )}>
                {/* Left sidebar with slide-in/out animation */}
                <div className={cn(
                    "fixed top-0 left-0 bottom-0 w-80 bg-gray-900 border-r border-gray-700/50 shadow-xl z-40 transition-all duration-300",
                    (showSidebar || sidebarPinned) ? "translate-x-0" : "-translate-x-full"
                )}>
                    {/* Sidebar content remains the same */}
                    <div className="mt-2">
                        <Sidebar
                            wsState={wsState}
                            isPinned={sidebarPinned}
                            onTogglePin={() => setSidebarPinned(!sidebarPinned)}
                            onClose={handleToggleSidebar}
                        />
                    </div>
                </div>

                {/* Top header - fixed height */}
                <div className="flex-none h-[140px] bg-background border-b">
                    <AppHeader
                        isMobile={isMobile}
                        selectedModel={modelSettings.primary}
                        secondaryModel={modelSettings.secondary}
                        models={models}
                        onToggleSidebar={handleToggleSidebar}
                        onModelSelect={(model) => handleModelSelect('primary', model)}
                        onSecondaryModelSelect={(model) => handleModelSelect('secondary', model)}
                        onToggleConversationTree={(showConversationTree || conversationTreePinned || showSettings || settingsPanelPinned || showSystemPrompts || systemPromptsPinned) ? null : handleToggleConversationTree}
                        onToggleSettings={(showSettings || settingsPanelPinned || showConversationTree || conversationTreePinned || showSystemPrompts || systemPromptsPinned) ? null : handleToggleSettings}
                        onToggleSystemPrompts={(showSettings || settingsPanelPinned || showConversationTree || conversationTreePinned || showSystemPrompts || systemPromptsPinned) ? null : handleToggleSystemPrompts}
                        onToggleToolPanel={() => setIsToolPanelOpen(true)}
                        isCommandBarOpen={isCommandBarOpen}
                        setIsCommandBarOpen={setIsCommandBarOpen}
                        CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
                        sidebarPinned={sidebarPinned || showSidebar}
                        rightSidebarPinned={conversationTreePinned || settingsPanelPinned || systemPromptsPinned}
                        activeConversationId={store.getState().conversations.activeConversationId}
                    />
                </div>

                {/* Middle chat container - flexible height */}
                <div className="flex-1 overflow-auto">
                    <ChatContainer
                        streamTokens={streamTokens}
                        isMobile={isMobile}
                    />
                </div>

                {/* Bottom input bar - fixed height */}
                <div className="flex-none h-[30vh] bg-background border-t">
                    <InputBar
                        selectedModel={modelSettings.primary}
                        onVoiceInputClick={() => setVoiceInputOpen(true)}
                        inputValue={inputValue}
                        onInputChange={setInputValue}
                    />
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
                            onClose={handleToggleConversationTree}
                            messages={{
                                id: selectedConversationId,
                                text: "Root",
                                children: buildMessageTree(store.getState().conversations.conversations[selectedConversationId]?.messages || [], false)
                            }}
                        />
                    </>
                )}
            </div>

            {/* System prompts panel with the highest z-index */}
            <div className={cn(
                "fixed top-0 right-0 bottom-0 w-80 bg-gray-900 border-l border-gray-700/50 shadow-xl z-50 transition-transform duration-300",
                showSystemPrompts || systemPromptsPinned ? "translate-x-0" : "translate-x-full"
            )}>
                {(showSystemPrompts || systemPromptsPinned) && (
                    <>
                        <div className="flex justify-between p-3 border-b border-gray-700 bg-[#1f2937]">
                            <div className="flex space-x-2">
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    onClick={() => setSystemPromptsPinned(!systemPromptsPinned)}
                                    className="text-gray-400 hover:text-gray-100"
                                >
                                    {systemPromptsPinned ? (
                                        <PinOff className="h-4 w-4" />
                                    ) : (
                                        <Pin className="h-4 w-4" />
                                    )}
                                </Button>
                                {!systemPromptsPinned && (
                                    <Button
                                        variant="ghost"
                                        size="icon"
                                        onClick={handleToggleSystemPrompts}
                                        className="text-gray-400 hover:text-gray-100"
                                    >
                                        <X className="h-4 w-4" />
                                    </Button>
                                )}
                            </div>
                            <h2 className="text-gray-100 text-lg font-semibold flex items-center gap-2">
                                <MessageSquare className="h-5 w-5" />
                                System Prompts
                            </h2>
                        </div>
                        <SystemPromptLibrary 
                            isOpen={true}
                            isPinned={systemPromptsPinned}
                            onClose={handleToggleSystemPrompts}
                            conversationId={store.getState().conversations.activeConversationId || undefined}
                            onApplyPrompt={(prompt) => {
                                console.log("App.tsx - Applying prompt:", prompt);
                                const conversationId = store.getState().conversations.activeConversationId;

                                // Check for guid in either camelCase or PascalCase
                                const promptId = prompt?.guid || prompt?.Guid;

                                if (conversationId && promptId) {
                                    console.log(`Dispatching setConversationSystemPrompt with conversationId=${conversationId}, promptId=${promptId}`);
                                    dispatch(setConversationSystemPrompt({
                                        conversationId,
                                        promptId
                                    }));
                                } else {
                                    console.error("Cannot apply prompt - missing required data:", {
                                        conversationId,
                                        promptId,
                                        prompt
                                    });
                                }

                                if (!systemPromptsPinned) {
                                    setShowSystemPrompts(false);
                                }
                            }}
                        />
                    </>
                )}
            </div>
            
            {/* Settings panel with high z-index */}
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

            {/* Voice Input Overlay */}
            <VoiceInputOverlay
                isOpen={isVoiceInputOpen}
                onClose={() => setVoiceInputOpen(false)}
                onTranscript={handleTranscript}
            />

            {/* Tool Panel Dialog */}
            {isToolPanelOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
                    <div className="bg-gray-900 border border-gray-700 rounded-lg w-5/6 h-5/6 max-w-6xl overflow-hidden">
                        <div className="flex justify-between items-center p-4 border-b border-gray-700">
                            <h2 className="text-xl font-semibold text-gray-100">Tool Management</h2>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => setIsToolPanelOpen(false)}
                                className="text-gray-400 hover:text-gray-100"
                            >
                                <X className="h-5 w-5" />
                            </Button>
                        </div>
                        <div className="h-full overflow-y-auto">
                            <ToolPanel isOpen={true} onClose={() => setIsToolPanelOpen(false)} />
                        </div>
                    </div>
                </div>
            )}
        </>
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