// src/App.tsx
import { useState, useEffect } from "react";
import { Provider } from 'react-redux';
import { store } from './store/store';
import { useMediaQuery } from '@/hooks/use-media-query';
import { Button } from '@/components/ui/button';
import { AppHeader } from './components/AppHeader';
import { ChatContainer } from './components/ChatContainer';
import { InputBar } from './components/InputBar';
import { Sidebar } from './components/Sidebar';
import { useWebSocket } from './hooks/useWebSocket';
import { useStreamTokens } from '@/hooks/useStreamTokens';
import { cn } from '@/lib/utils';
import { ConversationTreeView } from '@/components/ConversationTreeView';
import { SettingsPanel } from '@/components/SettingsPanel';
import { commandRegistry } from './commands/commandRegistry';
import { initializeCoreCommands } from './commands/coreCommands';
import { initializeModelCommands } from '@/plugins/modelCommands';
import { CommandBar } from './components/CommandBar';
import { VoiceInputOverlay } from './components/VoiceInputOverlay';
import { useVoiceInputState, setupVoiceInputKeyboardShortcut } from '@/commands/voiceInputCommand';
import { initializeVoiceCommands } from '@/plugins/voiceCommands';
import { ToolPanel } from './components/tools/ToolPanel';
import { useToolCommands } from '@/hooks/useToolCommands';
import { useToolStore } from '@/stores/useToolStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useConversationStore } from '@/stores/useConversationStore';
import { useModelStore } from '@/stores/useModelStore';
import { initializeSystemPromptCommands } from './commands/systemPromptCommands';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { useGetConfigQuery } from '@/services/api/chatApi';
import { useGetToolsQuery, useGetToolCategoriesQuery } from '@/services/api/toolsApi';
import { useGetSystemPromptsQuery, useSetConversationSystemPromptMutation } from '@/services/api/systemPromptApi';
import { ModelType } from '@/types/modelTypes';
import { Panel } from '@/components/panel';
import { usePanelStore } from '@/stores/usePanelStore';
import { v4 as uuidv4 } from 'uuid';

// Create an inner component that uses Redux hooks
function AppContent() {
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { isConnected, clientId } = useWebSocket();
    const wsState = { isConnected, clientId, messages: [] };
    const { streamTokens } = useStreamTokens();
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
    const [inputValue, setInputValue] = useState(''); // Add this state for voice input
    const [isToolPanelOpen, setIsToolPanelOpen] = useState(false);
    const [promptToEdit, setPromptToEdit] = useState<string | null>(null);

    // Zustand stores
    const { 
        setTools, 
        setCategories, 
        activeTools 
    } = useToolStore();
    
    const { 
        prompts, 
        defaultPromptId, 
        conversationPrompts, 
        setPrompts,
        setConversationPrompt 
    } = useSystemPromptStore();
    
    const { 
        createConversation, 
        activeConversationId,
        conversations
    } = useConversationStore();
    
    const {
        models,
        setModels,
        selectedPrimaryModel,
        selectedSecondaryModel,
        selectPrimaryModel,
        selectSecondaryModel
    } = useModelStore();

    // RTK Query hooks
    const { data: configData, isLoading: isConfigLoading } = useGetConfigQuery();
    const { data: tools } = useGetToolsQuery();
    const { data: toolCategories } = useGetToolCategoriesQuery();
    const { data: systemPrompts, isLoading: isSystemPromptsLoading } = useGetSystemPromptsQuery();
    const [setConversationSystemPrompt] = useSetConversationSystemPromptMutation();

    // Zustand panel state
    const { registerPanel, togglePanel, panels } = usePanelStore();

    // Register panels
    useEffect(() => {
        registerPanel({
            id: 'sidebar',
            position: 'left',
            size: '80',
            zIndex: 40,
            title: 'Conversations',
            isOpen: false,
            isPinned: false
        });

        registerPanel({
            id: 'conversationTree',
            position: 'right',
            size: '80',
            zIndex: 30,
            title: 'Conversation Tree',
            isOpen: false,
            isPinned: false
        });

        registerPanel({
            id: 'settings',
            position: 'right',
            size: '80',
            zIndex: 40,
            title: 'Settings',
            isOpen: false,
            isPinned: false
        });

        registerPanel({
            id: 'systemPrompts',
            position: 'right',
            size: '80',
            zIndex: 50,
            title: 'System Prompts',
            isOpen: false,
            isPinned: false
        });
    }, [registerPanel]);

    // Get panel states
    const sidebarPanel = panels.sidebar || { isOpen: false, isPinned: false };
    const conversationTreePanel = panels.conversationTree || { isOpen: false, isPinned: false };
    const settingsPanel = panels.settings || { isOpen: false, isPinned: false };
    const systemPromptsPanel = panels.systemPrompts || { isOpen: false, isPinned: false };

    // Sync tools and categories with Zustand store
    useEffect(() => {
        if (tools) {
            setTools(tools);
        }
    }, [tools, setTools]);

    useEffect(() => {
        if (toolCategories) {
            setCategories(toolCategories);
        }
    }, [toolCategories, setCategories]);

    // Sync system prompts with Zustand store
    useEffect(() => {
        if (systemPrompts) {
            setPrompts(systemPrompts);
        }
    }, [systemPrompts, setPrompts]);

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
            toggleSidebar: () => togglePanel('sidebar'),
            toggleConversationTree: handleToggleConversationTree,
            toggleSettings: () => togglePanel('settings'),
            openNewWindow: handleOpenNewWindow
        });

        // Initialize system prompt commands
        initializeSystemPromptCommands({
            toggleLibrary: () => togglePanel('systemPrompts'),
            createNewPrompt: () => {
                setPromptToEdit(null);
                togglePanel('systemPrompts');
            },
            editPrompt: (promptId) => {
                setPromptToEdit(promptId);
                togglePanel('systemPrompts');
            }
        });

        // Initialize model commands
        initializeModelCommands({
            getAvailableModels: () => models.map(m => m.modelName)
        });

        // Initialize voice commands
        initializeVoiceCommands();

        // Register all system prompts as commands
        const systemPromptsUpdated = () => {
            registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
        };

        // Initial registration
        systemPromptsUpdated();

        // Set up subscription to system prompts changes
        const unsubscribe = useSystemPromptStore.subscribe(
            (state) => state.prompts,
            () => systemPromptsUpdated()
        );

        // Set up voice input keyboard shortcut
        const cleanupKeyboardShortcut = setupVoiceInputKeyboardShortcut();

        return () => {
            cleanupKeyboardShortcut();
            unsubscribe();
        };
    }, [models, togglePanel]);

    // Load models from config
    useEffect(() => {
        if (configData) {
            // Set available models
            if (configData.models && configData.models.length > 0) {
                // Create model objects from config data
                const modelObjects = configData.models.map(modelName => ({
                    guid: uuidv4(),
                    modelName,
                    friendlyName: modelName,
                    providerGuid: '', // Default value, will be updated later
                    userNotes: '',
                    additionalParams: '',
                    input1MTokenPrice: 0,
                    output1MTokenPrice: 0,
                    color: '#4f46e5',
                    starred: false,
                    supportsPrefill: false
                }));
                
                setModels(modelObjects);
            }

            // Set primary model if available
            if (configData.defaultModel && configData.defaultModel.length > 0) {
                selectPrimaryModel(configData.defaultModel);
            }
            
            // Set secondary model if available
            if (configData.secondaryModel && configData.secondaryModel.length > 0) {
                selectSecondaryModel(configData.secondaryModel);
            }

            // Create initial conversation if needed
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
        }
    }, [configData, activeConversationId, createConversation, setModels, selectPrimaryModel, selectSecondaryModel]);

    const handleModelSelect = (modelType: ModelType, modelName: string) => {
        if (modelType === 'primary') {
            selectPrimaryModel(modelName);
        } else {
            selectSecondaryModel(modelName);
        }

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
                    if (modelType === 'primary') {
                        selectPrimaryModel(modelName);
                    } else {
                        selectSecondaryModel(modelName);
                    }
                }
            });
        }
    };

    const handleToggleConversationTree = () => {
        // When opening the tree, use the active conversation ID
        setSelectedConversationId(activeConversationId);
        console.log('Opening conversation tree with conversation ID:', activeConversationId);
        togglePanel('conversationTree');
    };

    // Subscribe to Zustand store to update the conversation tree when messages change
    useEffect(() => {
        let lastMessagesLength = 0;
        let lastActiveConversation = '';

        // Set up subscription to conversation messages changes
        const unsubscribe = useConversationStore.subscribe(
            (state) => ({ 
                activeId: state.activeConversationId, 
                conversations: state.conversations 
            }),
            ({ activeId, conversations }) => {
                if (!activeId || !selectedConversationId) return;
                
                // Get current conversation messages
                const conversation = conversations[activeId];
                if (!conversation) return;
                
                const currentMessagesLength = conversation.messages.length;
                
                // Only refresh when message count changes or active conversation changes
                if (currentMessagesLength !== lastMessagesLength ||
                    activeId !== lastActiveConversation) {
                    
                    console.log('Conversation store updated - conversation messages changed:', {
                        oldCount: lastMessagesLength,
                        newCount: currentMessagesLength,
                        activeConversationId: activeId
                    });
                    
                    // Force a refresh of the tree view by briefly setting to null and back
                    if (conversationTreePanel.isOpen) {
                        setSelectedConversationId(null);
                        setTimeout(() => {
                            setSelectedConversationId(activeId);
                        }, 50);
                    }
                    
                    // Update tracking variables
                    lastMessagesLength = currentMessagesLength;
                    lastActiveConversation = activeId;
                }
            }
        );

        return () => unsubscribe();
    }, [conversationTreePanel.isOpen, selectedConversationId]);

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
                sidebarPanel.isPinned && "pl-80",
                (conversationTreePanel.isPinned || settingsPanel.isPinned || systemPromptsPanel.isPinned) && "pr-80"
            )}>
                {/* Top header - fixed height */}
                <div className="flex-none h-[140px] bg-background">
                    <AppHeader
                        isMobile={isMobile}
                        selectedModel={selectedPrimaryModel}
                        secondaryModel={selectedSecondaryModel}
                        models={models.map(m => m.modelName)}
                        onToggleSidebar={() => togglePanel('sidebar')}
                        onModelSelect={(model) => handleModelSelect('primary', model)}
                        onSecondaryModelSelect={(model) => handleModelSelect('secondary', model)}
                        onToggleConversationTree={handleToggleConversationTree}
                        onToggleSettings={() => togglePanel('settings')}
                        onToggleSystemPrompts={() => togglePanel('systemPrompts')}
                        onToggleToolPanel={() => setIsToolPanelOpen(true)}
                        isCommandBarOpen={isCommandBarOpen}
                        setIsCommandBarOpen={setIsCommandBarOpen}
                        CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
                        sidebarPinned={sidebarPanel.isPinned}
                        rightSidebarPinned={conversationTreePanel.isPinned || settingsPanel.isPinned || systemPromptsPanel.isPinned}
                        activeConversationId={activeConversationId}
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
                        selectedModel={selectedPrimaryModel}
                        onVoiceInputClick={() => setVoiceInputOpen(true)}
                        inputValue={inputValue}
                        onInputChange={setInputValue}
                        activeTools={activeTools} // Pass activeTools from Zustand
                    />
                </div>
            </div>

            {/* Sidebar Panel */}
            <Panel
                id="sidebar"
                position="left"
                size="80"
                minWidth="300px"
                maxWidth="360px"
                width="320px"  // Fixed width
                zIndex={40}
                title="Conversations"
                isOpen={sidebarPanel.isOpen}
                isPinned={sidebarPanel.isPinned}
            // No need for onClose or onTogglePinned - uses store functions by default
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
                    isOpen={conversationTreePanel.isOpen}
                    isPinned={conversationTreePanel.isPinned}
                >
                    <ConversationTreeView
                        key={`tree-${selectedConversationId}-${Date.now()}`}
                        conversationId={selectedConversationId}
                        messages={selectedConversationId && conversations[selectedConversationId]?.messages || []}
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
                isOpen={settingsPanel.isOpen}
                isPinned={settingsPanel.isPinned}
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
                isOpen={systemPromptsPanel.isOpen}
                isPinned={systemPromptsPanel.isPinned}
            >
                <SystemPromptLibrary
                    isOpen={true}
                    conversationId={activeConversationId || undefined}
                    onApplyPrompt={(prompt) => {
                        console.log("App.tsx - Applying prompt:", prompt);
                        const conversationId = activeConversationId;

                        // Check for guid in either camelCase or PascalCase
                        const promptId = prompt?.guid || prompt?.Guid;

                        if (conversationId && promptId) {
                            console.log(`Setting conversation system prompt with conversationId=${conversationId}, promptId=${promptId}`);
                            // Using RTK Query mutation
                            setConversationSystemPrompt({ conversationId, promptId });
                            // Also update the Zustand store
                            setConversationPrompt(conversationId, promptId);
                        } else {
                            console.error("Cannot apply prompt - missing required data:", {
                                conversationId,
                                promptId,
                                prompt
                            });
                        }

                        togglePanel('systemPrompts');
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
                                <span className="h-5 w-5">×</span>
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