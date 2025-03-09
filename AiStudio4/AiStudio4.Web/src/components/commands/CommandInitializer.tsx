// src/components/commands/CommandInitializer.tsx
import { useEffect } from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { initializeCoreCommands } from '@/commands/coreCommands';
import { initializeModelCommands } from '@/plugins/modelCommands';
import { initializeVoiceCommands } from '@/plugins/voiceCommands';
import { initializeSystemPromptCommands, registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { initializeSettingsCommands, registerModelCommands, registerProviderCommands } from '@/commands/settingsCommands';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useModelStore } from '@/stores/useModelStore';
import { useModelManagement } from '@/hooks/useModelManagement';
import { usePanelStore } from '@/stores/usePanelStore';
import { useToolCommands } from '@/hooks/useToolCommands';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { setupVoiceInputKeyboardShortcut } from '@/commands/voiceInputCommand';
import { useToolsManagement } from '@/hooks/useToolsManagement';

export function CommandInitializer() {
  const { togglePanel } = usePanelStore();
  const { models, handleModelSelect } = useModelManagement();
  const { fetchPinnedCommands } = usePinnedCommandsStore();
  const { fetchTools, fetchToolCategories } = useToolsManagement();
  
  // Handle opening a new window
  const handleOpenNewWindow = () => {
    window.open(window.location.href, '_blank');
  };
  
  // Handle conversation tree toggle
  const handleToggleConversationTree = () => {
    togglePanel('conversationTree');
  };
  
  // Define tool panel functions for commands
  const openToolPanel = () => {
    window.localStorage.setItem('toolPanel_action', 'open');
    window.dispatchEvent(new Event('openToolPanel'));
  };
  
  // Use the tool commands hook
  useToolCommands({
    openToolPanel,
    createNewTool: () => {
      window.localStorage.setItem('toolPanel_action', 'create');
      window.dispatchEvent(new Event('openToolPanel'));
    },
    importTools: () => {
      window.localStorage.setItem('toolPanel_action', 'import');
      window.dispatchEvent(new Event('openToolPanel'));
    },
    exportTools: () => {
      window.localStorage.setItem('toolPanel_action', 'export');
      window.dispatchEvent(new Event('openToolPanel'));
    }
  });

  // Fetch pinned commands and tools on component mount
  useEffect(() => {
    fetchPinnedCommands();
    fetchTools();
    fetchToolCategories();
  }, [fetchPinnedCommands, fetchTools, fetchToolCategories]);

  // Initialize all commands
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
        togglePanel('systemPrompts');
        window.localStorage.setItem('systemPrompt_action', 'create');
      },
      editPrompt: (promptId) => {
        togglePanel('systemPrompts');
        window.localStorage.setItem('systemPrompt_edit', promptId);
      }
    });

    // Initialize settings commands
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
    const unsubscribePrompts = useSystemPromptStore.subscribe(
      (state) => state.prompts,
      () => systemPromptsUpdated()
    );

    // Register individual model and provider commands whenever they change
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
      unsubscribePrompts();
      unsubscribeModels();
      unsubscribeProviders();
    };
  }, [models, togglePanel, handleModelSelect]);

  // No UI to render - this is a logic-only component
  return null;
}