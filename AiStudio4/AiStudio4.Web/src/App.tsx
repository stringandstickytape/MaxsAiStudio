﻿import { useState, useEffect } from "react";
import { useDispatch } from 'react-redux';
import { useMediaQuery } from '@/hooks/use-media-query';
import { Provider } from 'react-redux';
import { store } from './store/store';
import { MessageSquare } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { createConversation } from './store/conversationSlice';
import { AppHeader } from './components/AppHeader';
import { ChatContainer } from './components/ChatContainer';
import { InputBar } from './components/input-bar';
import { Sidebar } from './components/Sidebar';
import { useWebSocket } from './hooks/useWebSocket';
import { useLiveStream } from '@/hooks/useLiveStream';
import { cn } from '@/lib/utils';
import { ConversationTreeView } from '@/components/ConversationTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { buildMessageTree } from '@/utils/treeUtils';
import { commandRegistry } from './commands/commandRegistry';
import { initializeCoreCommands } from './commands/coreCommands';
import { initializeModelCommands } from '@/plugins/modelCommands';
import { CommandBar } from './components/CommandBar';
import { VoiceInputOverlay } from '@/components/VoiceInputOverlay';
import { useVoiceInputState, setupVoiceInputKeyboardShortcut } from '@/commands/voiceInputCommand';
import { initializeVoiceCommands } from '@/plugins/voiceCommands';
import { ToolPanel } from '@/components/tools/ToolPanel';
import { useToolCommands } from '@/hooks/useToolCommands';
import { setTools } from '@/store/toolSlice';
import { initializeSystemPromptCommands } from './commands/systemPromptCommands';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { setPrompts, setConversationPrompt } from './store/systemPromptSlice';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { useGetConfigQuery } from '@/services/api/chatApi';
import { useGetToolsQuery, useGetToolCategoriesQuery } from '@/services/api/toolsApi';
import { useGetSystemPromptsQuery, useSetConversationSystemPromptMutation } from '@/services/api/systemPromptApi';
import { ModelType } from '@/types/modelTypes';
import { PanelProvider } from '@/contexts/PanelContext';
import { usePanelManager } from '@/hooks/usePanelManager';
import { Panel } from '@/components/ui/panel';

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
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
    const [inputValue, setInputValue] = useState(''); // Add this state for voice input
    const [isToolPanelOpen, setIsToolPanelOpen] = useState(false);
    const [promptToEdit, setPromptToEdit] = useState<string | null>(null);

    // RTK Query hooks
    const { data: configData, isLoading: isConfigLoading } = useGetConfigQuery();
    const { data: tools, isLoading: isToolsLoading } = useGetToolsQuery();
    const { data: toolCategories } = useGetToolCategoriesQuery();
    const { data: systemPrompts, isLoading: isSystemPromptsLoading } = useGetSystemPromptsQuery();
    const [setConversationSystemPrompt] = useSetConversationSystemPromptMutation();

    // Use the panel manager hooks for each panel
    const sidebar = usePanelManager({
        id: 'sidebar',
        position: 'left',
        size: '80',
        zIndex: 40,
        title: 'Conversations'
    });

    const conversationTree = usePanelManager({
        id: 'conversationTree',
        position: 'right',
        size: '80',
        zIndex: 30,
        title: 'Conversation Tree'
    });

    const settings = usePanelManager({
        id: 'settings',
        position: 'right',
        size: '80',
        zIndex: 40,
        title: 'Settings'
    });

    const systemPromptPanel = usePanelManager({
        id: 'systemPrompts',
        position: 'right',
        size: '80',
        zIndex: 50,
        title: 'System Prompts'
    });

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
            toggleSidebar: sidebar.toggle,
            toggleConversationTree: handleToggleConversationTree,
            toggleSettings: settings.toggle,
            openNewWindow: handleOpenNewWindow
        });

        // Initialize system prompt commands
        initializeSystemPromptCommands({
            toggleLibrary: systemPromptPanel.toggle,
            createNewPrompt: () => {
                setPromptToEdit(null);
                systemPromptPanel.open();
            },
            editPrompt: (promptId) => {
                setPromptToEdit(promptId);
                systemPromptPanel.open();
            }
        });

        // Initialize model commands
        initializeModelCommands({
            onModelSelect: handleModelSelect,
            getAvailableModels: () => models
        });

        // Initialize voice commands
        initializeVoiceCommands();

        // Register all system prompts as commands
        const systemPromptsUpdated = () => {
            registerSystemPromptsAsCommands(systemPromptPanel.toggle);
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

    // Update tools and categories in Redux state when they load from RTK Query
    useEffect(() => {
        if (tools) {
            dispatch(setTools(tools));
        }
    }, [tools, dispatch]);

    // Update system prompts in Redux state when they load from RTK Query
    useEffect(() => {
        if (systemPrompts) {
            dispatch(setPrompts(systemPrompts));
        }
    }, [systemPrompts, dispatch]);

    // Load models from config
    useEffect(() => {
        if (configData) {
            setModels(configData.models || []);

            // Set both models at once using the new state structure
            setModelSettings({
                primary: configData.defaultModel && configData.defaultModel.length > 0 ? configData.defaultModel : "Select Model",
                secondary: configData.secondaryModel && configData.secondaryModel.length > 0 ? configData.secondaryModel : "Select Model"
            });

            // Create initial conversation if needed
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
        }
    }, [configData, dispatch]);

    const handleModelSelect = (modelType: ModelType, modelName: string) => {
        setModelSettings(prev => ({
            ...prev,
            [modelType]: modelName
        }));

        // When model changes, create command for quick access
        const commandId = `select-${modelType}-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`;
        if (!commandRegistry.getCommandById(commandId)) {
            commandRegistry.registerCommand({
                id: commandId,
                name: `Set ${modelType} model to ${modelName}`,
                description: `Change the ${modelType} model to ${modelName}`,
                keywords: ['model', 'select', modelType, modelName],
                section: 'model',
                execute: () => {
                    setModelSettings(prev => ({
                        ...prev,
                        [modelType]: modelName
                    }));
                }
            });
        }
    };

    const handleToggleConversationTree = () => {
        // When opening the tree, use the active conversation ID
        const state = store.getState();
        const activeConversationId = state.conversations.activeConversationId;
        setSelectedConversationId(activeConversationId);
        console.log('Opening conversation tree with conversation ID:', activeConversationId);
        conversationTree.toggle();
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
                if (conversationTree.isOpen) {
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
    }, [conversationTree.isOpen, selectedConversationId]);

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
                sidebar.isPinned && "pl-80",
                conversationTree.isPinned && "pr-80",
                settings.isPinned && "pr-80",
                systemPromptPanel.isPinned && "pr-80"
            )}>
                {/* Top header - fixed height */}
                <div className="flex-none h-[140px] bg-background">
                    <AppHeader
                        isMobile={isMobile}
                        selectedModel={modelSettings.primary}
                        secondaryModel={modelSettings.secondary}
                        models={models}
                        onToggleSidebar={sidebar.toggle}
                        onModelSelect={(model) => handleModelSelect('primary', model)}
                        onSecondaryModelSelect={(model) => handleModelSelect('secondary', model)}
                        onToggleConversationTree={handleToggleConversationTree}
                        onToggleSettings={settings.toggle}
                        onToggleSystemPrompts={systemPromptPanel.toggle}
                        onToggleToolPanel={() => setIsToolPanelOpen(true)}
                        isCommandBarOpen={isCommandBarOpen}
                        setIsCommandBarOpen={setIsCommandBarOpen}
                        CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
                        sidebarPinned={sidebar.isPinned}
                        rightSidebarPinned={conversationTree.isPinned || settings.isPinned || systemPromptPanel.isPinned}
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

            {/* Sidebar Panel */}
            <Panel 
                id="sidebar"
                position="left"
                size="80"
                zIndex={40}
                title="Conversations"
            >
                <Sidebar
                    wsState={wsState}
                />
            </Panel>

            {/* Conversation Tree Panel */}
            {selectedConversationId && (
                <Panel
                    id="conversationTree"
                    position="right"
                    size="80"
                    zIndex={30}
                    title="Conversation Tree"
                >
                    <ConversationTreeView
                        key={`tree-${selectedConversationId}-${Date.now()}`} // Force re-render when id changes or is refreshed
                        conversationId={selectedConversationId}
                        messages={{
                            id: selectedConversationId,
                            text: "Root",
                            children: buildMessageTree(store.getState().conversations.conversations[selectedConversationId]?.messages || [], false)
                        }}
                    />
                </Panel>
            )}

            {/* Settings Panel */}
            <Panel
                id="settings"
                position="right"
                size="80"
                zIndex={40}
                title="Settings"
            >
                <SettingsPanel
                    isOpen={true}
                />
            </Panel>

            {/* System Prompts Panel */}
            <Panel
                id="systemPrompts"
                position="right"
                size="80"
                zIndex={50}
                title="System Prompts"
            >
                <SystemPromptLibrary
                    isOpen={true}
                    conversationId={store.getState().conversations.activeConversationId || undefined}
                    onApplyPrompt={(prompt) => {
                        console.log("App.tsx - Applying prompt:", prompt);
                        const conversationId = store.getState().conversations.activeConversationId;

                        // Check for guid in either camelCase or PascalCase
                        const promptId = prompt?.guid || prompt?.Guid;

                        if (conversationId && promptId) {
                            console.log(`Dispatching setConversationSystemPrompt with conversationId=${conversationId}, promptId=${promptId}`);
                            // Using RTK Query mutation
                            setConversationSystemPrompt({ conversationId, promptId });
                            // Also update the local Redux state
                            dispatch(setConversationPrompt({ conversationId, promptId }));
                        } else {
                            console.error("Cannot apply prompt - missing required data:", {
                                conversationId,
                                promptId,
                                prompt
                            });
                        }

                        systemPromptPanel.close();
                    }}
                />
            </Panel>

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
                                <span className="h-5 w-5">�</span>
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

// Wrapper component that provides the Redux store and panel context
function App() {
    return (
        <Provider store={store}>
            <PanelProvider>
                <AppContent />
            </PanelProvider>
        </Provider>
    );
}

export default App;