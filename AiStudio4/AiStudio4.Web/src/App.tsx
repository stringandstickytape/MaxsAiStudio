// src/App.tsx
import { useState, useEffect, useCallback } from "react";
import { PanelManager, type PanelConfig } from '@/components/PanelManager';
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
import { useCommandStore } from '@/stores/useCommandStore';
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
import { useModelManagement } from '@/hooks/useModelManagement';
import { useSystemPromptManagement } from '@/hooks/useSystemPromptManagement';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { initializeSystemPromptCommands } from './commands/systemPromptCommands';
import { initializeSettingsCommands, registerModelCommands, registerProviderCommands } from './commands/settingsCommands';
import { SystemPromptLibrary } from '@/components/SystemPrompt/SystemPromptLibrary';
import { registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { ModelType } from '@/types/modelTypes';
import { Panel } from '@/components/panel';
import { usePanelStore } from '@/stores/usePanelStore';
import { v4 as uuidv4 } from 'uuid';

function App() {
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { isConnected, clientId } = useWebSocket();
    const wsState = { isConnected, clientId, messages: [] };
    const { streamTokens } = useStreamTokens();
    const [selectedConversationId, setSelectedConversationId] = useState<string | null>(null);
    const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
    const [inputValue, setInputValue] = useState('');
    const [isToolPanelOpen, setIsToolPanelOpen] = useState(false);
    const [promptToEdit, setPromptToEdit] = useState<string | null>(null);

    // Zustand stores
    const { 
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
        conversations,
        setActiveConversation
    } = useConversationStore();
    
    // Use the centralized management hooks
    const {
        models,
        selectedPrimaryModel,
        selectedSecondaryModel,
        handleModelSelect
    } = useModelManagement();

    const {
        isLoading: isSystemPromptsLoading,
        prompts: systemPrompts,
        setConversationSystemPrompt
    } = useSystemPromptManagement();

    const {
        fetchTools,
        fetchToolCategories
    } = useToolsManagement();

    // Initialize pinned commands to use Zustand's store
    const { fetchPinnedCommands } = usePinnedCommandsStore();
    
    // Fetch pinned commands on component mount
    useEffect(() => {
        fetchPinnedCommands();
    }, [fetchPinnedCommands]);

    // Fetch tools and categories
    useEffect(() => {
        fetchTools();
        fetchToolCategories();
    }, [fetchTools, fetchToolCategories]);

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

    // Sync system prompts with Zustand store
    useEffect(() => {
        if (systemPrompts) {
            setPrompts(systemPrompts);
        }
    }, [systemPrompts, setPrompts]);

    // Define openToolPanel function to be used both directly and via commands
    const openToolPanel = useCallback(() => {
        setIsToolPanelOpen(true);
    }, []);
    
    // Use the tool commands hook to set up tool-related commands
    const toolCommands = useToolCommands({
        openToolPanel,
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

        // Initialize settings commands with the simpler approach
        initializeSettingsCommands({
            openSettings: () => togglePanel('settings')
        });

        // Initialize model commands
        initializeModelCommands({
            getAvailableModels: () => models.map(m => m.modelName),
            selectPrimaryModel: (modelName) => handleModelSelect('primary', modelName),
            selectSecondaryModel: (modelName) => handleModelSelect('secondary', modelName)
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

        // Register individual model and provider commands whenever they change
        // Update the model commands subscription
        const unsubscribeModels = useModelStore.subscribe(
            (state) => state.models,
            (models) => {
                if (models.length > 0) {
                    registerModelCommands(models, () => togglePanel('settings'));
                }
            }
        );

        // Update the provider commands subscription
        const unsubscribeProviders = useModelStore.subscribe(
            (state) => state.providers,
            (providers) => {
                if (providers.length > 0) {
                    registerProviderCommands(providers, () => togglePanel('settings'));
                }
            }
        );

        // Set up voice input keyboard shortcut
        const cleanupKeyboardShortcut = setupVoiceInputKeyboardShortcut();

        return () => {
            cleanupKeyboardShortcut();
            unsubscribe();
            unsubscribeModels();
            unsubscribeProviders();
        };
    }, [models, togglePanel, handleModelSelect]);

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

    // Handle model selection with command registration
    const handleLocalModelSelect = (modelType: ModelType, modelName: string) => {
        // Call the hook's handleModelSelect function
        handleModelSelect(modelType, modelName);

        // When model changes, create command for quick access
        const commandId = `select-${modelType}-model-${modelName.toLowerCase().replace(/\s+/g, '-')}`;
        const commandStore = useCommandStore.getState();
        if (!commandStore.getCommandById(commandId)) {
            commandStore.registerCommand({
                id: commandId,
                name: `Set ${modelType} model to ${modelName}`,
                description: `Change the ${modelType} model to ${modelName}`,
                keywords: ['model', 'select', modelType, modelName],
                section: 'model',
                execute: () => handleModelSelect(modelType, modelName)
            });
        }
    };

    const handleToggleConversationTree = () => {
        // Always use the activeConversationId when toggling the tree
        setSelectedConversationId(activeConversationId);
        
        console.log('Opening conversation tree with conversation ID:', activeConversationId);
        togglePanel('conversationTree');
    };

    // Subscribe to Zustand store to update the conversation tree when messages change
    useEffect(() => {
        // Keep selectedConversationId synchronized with activeConversationId
        // This is critical for the tree view to update when historical conversations are loaded
        if (activeConversationId && activeConversationId !== selectedConversationId) {
            // When active conversation changes, update the selected conversation ID
            console.log('Active conversation changed, updating selected conversation ID', {
                old: selectedConversationId,
                new: activeConversationId
            });
            setSelectedConversationId(activeConversationId);
        }
        
        // Track message changes to refresh the tree when needed
        let lastMessagesLength = 0;
        
        // Set up subscription to conversation messages changes
        const unsubscribe = useConversationStore.subscribe(
            (state) => ({ 
                activeId: state.activeConversationId, 
                conversations: state.conversations 
            }),
            ({ activeId, conversations }) => {
                if (!activeId) return;
                
                // Update selectedConversationId to match activeConversationId if they differ
                if (activeId !== selectedConversationId) {
                    setSelectedConversationId(activeId);
                }
                
                // Get current conversation messages
                const conversation = conversations[activeId];
                if (!conversation) return;
                
                const currentMessagesLength = conversation.messages.length;
                
                // Only refresh when message count changes 
                if (currentMessagesLength !== lastMessagesLength) {
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
                    
                    // Update tracking variable
                    lastMessagesLength = currentMessagesLength;
                }
            }
        );

        return () => unsubscribe();
    }, [conversationTreePanel.isOpen, activeConversationId, selectedConversationId]);

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
    
    // Define panel configurations
    const panelConfigs: PanelConfig[] = [
        {
            id: 'sidebar',
            position: 'left',
            size: '80',
            minWidth: '320px',
            maxWidth: '320px',
            width: '320px',
            zIndex: 40,
            title: 'Conversations',
            render: (isOpen) => isOpen ? <Sidebar wsState={wsState} /> : null
        },
        {
            id: 'conversationTree',
            position: 'right',
            size: '80',
            minWidth: '320px',
            maxWidth: '320px',
            width: '320px',
            zIndex: 30,
            title: 'Conversation Tree',
            render: (isOpen) => isOpen && selectedConversationId ? (
                <ConversationTreeView
                    key={`tree-${selectedConversationId}-${Date.now()}`}
                    conversationId={selectedConversationId}
                    messages={selectedConversationId && conversations[selectedConversationId]?.messages || []}
                />
            ) : null
        },
        {
            id: 'settings',
            position: 'right',
            size: '80',
            minWidth: '320px',
            maxWidth: '320px',
            width: '320px',
            zIndex: 40,
            title: 'Settings',
            render: (isOpen) => isOpen ? <SettingsPanel isOpen={true} /> : null
        },
        {
            id: 'systemPrompts',
            position: 'right',
            size: '80',
            minWidth: '320px',
            maxWidth: '320px',
            width: '320px',
            zIndex: 50,
            title: 'System Prompts',
            render: (isOpen) => isOpen ? (
                <SystemPromptLibrary
                    isOpen={true}
                    conversationId={activeConversationId || undefined}
                    onApplyPrompt={(prompt) => {
                        console.log("App.tsx - Applying prompt:", prompt);
                        const conversationId = activeConversationId;
                        const promptId = prompt?.guid || prompt?.Guid;

                        if (conversationId && promptId) {
                            console.log(`Setting conversation system prompt with conversationId=${conversationId}, promptId=${promptId}`);
                            setConversationSystemPrompt({ conversationId, promptId });
                            setConversationPrompt(conversationId, promptId);
                        } else {
                            console.error("Cannot apply prompt - missing required data:", {
                                conversationId, promptId, prompt
                            });
                        }

                        togglePanel('systemPrompts');
                    }}
                />
            ) : null
        }
    ];
    
    // Get panel states for layout calculations
    const hasLeftPanel = panels.sidebar?.isPinned || false;
    const hasRightPanel = panels.conversationTree?.isPinned || 
                           panels.settings?.isPinned || 
                           panels.systemPrompts?.isPinned || false;

    return (
        <>
            <div className={cn(
                "h-screen flex flex-col",
                hasLeftPanel && "pl-80",
                hasRightPanel && "pr-80"
            )}>
                {/* Top header - fixed height */}
                <div className="flex-none h-[155px] bg-background">
                    <AppHeader
                        isMobile={isMobile}
                        selectedModel={selectedPrimaryModel}
                        secondaryModel={selectedSecondaryModel}
                        models={models.map(m => m.modelName)}
                        onToggleSidebar={() => togglePanel('sidebar')}
                        onModelSelect={(model) => handleLocalModelSelect('primary', model)}
                        onSecondaryModelSelect={(model) => handleLocalModelSelect('secondary', model)}
                        onToggleConversationTree={handleToggleConversationTree}
                        onToggleSettings={() => togglePanel('settings')}
                        onToggleSystemPrompts={() => togglePanel('systemPrompts')}
                        onToggleToolPanel={() => setIsToolPanelOpen(true)}
                        isCommandBarOpen={isCommandBarOpen}
                        setIsCommandBarOpen={setIsCommandBarOpen}
                        CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
                        sidebarPinned={hasLeftPanel}
                        rightSidebarPinned={hasRightPanel}
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
                        onManageTools={openToolPanel} // Pass the function to InputBar
                    />
                </div>
            </div>

            {/* Add the new panel manager */}
            <PanelManager panels={panelConfigs} />

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

// Export the App component directly, no Redux Provider needed
export default App;