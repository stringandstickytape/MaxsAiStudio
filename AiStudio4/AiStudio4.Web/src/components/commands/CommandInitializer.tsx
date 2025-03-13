// src/components/commands/CommandInitializer.tsx
import { useEffect } from 'react';
import { registerEditMessageCommands } from '@/components/commands/EditMessageCommands';
import { initializeCoreCommands } from '@/commands/coreCommands';
import { initializeModelCommands } from '@/commands/modelCommands';
import { initializeVoiceInputCommand } from '@/commands/voiceInputCommand';
import { initializeSystemPromptCommands, registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { initializeUserPromptCommands, registerUserPromptsAsCommands } from '@/commands/userPromptCommands';
import {
  initializeSettingsCommands,
  registerModelCommands,
  registerProviderCommands,
} from '@/commands/settingsCommands';
import { initializeAppearanceCommands } from '@/commands/appearanceCommands';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useModelStore } from '@/stores/useModelStore';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { usePanelStore } from '@/stores/usePanelStore';
import { useToolCommands } from '@/hooks/useToolCommands';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { setupVoiceInputKeyboardShortcut } from '@/commands/voiceInputCommand';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
export function CommandInitializer() {
  const { togglePanel } = usePanelStore();
  const { models, handleModelSelect } = useModelManagement();
  const { fetchPinnedCommands } = usePinnedCommandsStore();
  const { fetchUserPrompts } = useUserPromptManagement();
  const { fetchTools, fetchToolCategories } = useToolsManagement();

  const handleOpenNewWindow = () => {
    window.open(window.location.href, '_blank');
  };

  const handleToggleConvTree = () => {
    togglePanel('convTree');
  };

  const openToolPanel = () => {
    
    togglePanel('toolPanel');
  };

  useToolCommands({
    openToolPanel,
    createNewTool: () => {
      window.localStorage.setItem('toolPanel_action', 'create');
      window.dispatchEvent(new Event('openToolPanel'));
    },
    exportTools: () => {
      window.localStorage.setItem('toolPanel_action', 'export');
      window.dispatchEvent(new Event('openToolPanel'));
    },
  });

  
  useEffect(() => {
    const loadInitialData = async () => {
      try {
        await Promise.all([
          fetchPinnedCommands(),
          fetchTools(),
          fetchToolCategories(),
          fetchUserPrompts()
        ]);
        
        
        registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
        registerUserPromptsAsCommands(() => togglePanel('userPrompts'));
        console.log('Initial prompt registration completed');
      } catch (error) {
        console.error('Error loading initial data:', error);
      }
    };
    
    loadInitialData();
  }, [fetchPinnedCommands, fetchTools, fetchToolCategories, fetchUserPrompts, togglePanel]);

  useEffect(() => {
    initializeCoreCommands({
      toggleSidebar: () => togglePanel('sidebar'),
      toggleConvTree: handleToggleConvTree,
      toggleSettings: () => togglePanel('settings'),
      openNewWindow: handleOpenNewWindow,
    });

      initializeSystemPromptCommands({
      toggleLibrary: () => togglePanel('systemPrompts'),
      createNewPrompt: () => {
        togglePanel('systemPrompts');
        window.localStorage.setItem('systemPrompt_action', 'create');
      },
      editPrompt: (promptId) => {
        togglePanel('systemPrompts');
        window.localStorage.setItem('systemPrompt_edit', promptId);
      },
    });

    initializeUserPromptCommands({
      toggleLibrary: () => togglePanel('userPrompts'),
      createNewPrompt: () => {
        togglePanel('userPrompts');
        window.localStorage.setItem('userPrompt_action', 'create');
      },
      editPrompt: (promptId) => {
        togglePanel('userPrompts');
        window.localStorage.setItem('userPrompt_edit', promptId);
      },
    });

    initializeSettingsCommands({
      openSettings: () => togglePanel('settings'),
    });

    initializeAppearanceCommands({
      openAppearanceSettings: () => {
        togglePanel('settings');
      },
    });

    initializeModelCommands({
      getAvailableModels: () => models,
      selectPrimaryModel: (modelName) => handleModelSelect('primary', modelName),
      selectSecondaryModel: (modelName) => handleModelSelect('secondary', modelName),
    });

      initializeVoiceInputCommand();

      registerEditMessageCommands();

    const systemPromptsUpdated = () => {
      registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
    };

    systemPromptsUpdated();

    
    const userPromptsUpdated = () => {
      registerUserPromptsAsCommands(() => togglePanel('userPrompts'));
    };

    userPromptsUpdated();

    const unsubscribePrompts = useSystemPromptStore.subscribe(
      (state) => state.prompts,
      () => systemPromptsUpdated(),
    );

    const unsubscribeUserPrompts = useUserPromptStore.subscribe(
      (state) => state.prompts,
      () => userPromptsUpdated(),
    );

    const unsubscribeModels = useModelStore.subscribe(
      (state) => state.models,
      (models) => {
        if (models.length > 0) {
          registerModelCommands(models, () => togglePanel('settings'));
        }
      },
    );

    const unsubscribeProviders = useModelStore.subscribe(
      (state) => state.providers,
      (providers) => {
        if (providers.length > 0) {
          registerProviderCommands(providers, () => togglePanel('settings'));
        }
      },
    );

    const cleanupKeyboardShortcut = setupVoiceInputKeyboardShortcut();

    return () => {
      cleanupKeyboardShortcut();
      unsubscribePrompts();
      unsubscribeUserPrompts();
      unsubscribeModels();
      unsubscribeProviders();
    };
  }, [models, togglePanel, handleModelSelect]);

  return null;
}
