import { useEffect } from 'react';
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
import { registerThemeCommands } from '@/commands/themeCommands';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useModelStore } from '@/stores/useModelStore';
import { useThemeStore } from '@/stores/useThemeStore';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { usePanelStore } from '@/stores/usePanelStore';
import { useToolCommands } from '@/hooks/useToolCommands';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { setupVoiceInputKeyboardShortcut } from '@/commands/voiceInputCommand';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';
import { useUserPromptStore } from '@/stores/useUserPromptStore';
import { useModalStore } from '@/stores/useModalStore'; // Added import for modal control
import { useFileSystemManagement } from '@/hooks/useFileSystemManagement';

export function CommandInitializer() {
  const { togglePanel } = usePanelStore();
  const { models, handleModelSelect } = useModelManagement();
  const { fetchPinnedCommands } = usePinnedCommandsStore();
  const { fetchUserPrompts } = useUserPromptManagement();
  const { fetchTools, fetchToolCategories } = useToolsManagement();
  const { fetchFileSystem } = useFileSystemManagement();

  const handleOpenNewWindow = () => {
    window.open(window.location.href, '_blank');
  };

  const openToolLibrary = () => {
    window.dispatchEvent(new CustomEvent('open-tool-library'));
  };

  useToolCommands({
    openToolLibrary,
    createNewTool: () => {
      window.localStorage.setItem('toolPanel_action', 'create');
      window.dispatchEvent(new CustomEvent('open-tool-library'));
    },
    exportTools: () => {
      window.localStorage.setItem('toolPanel_action', 'export');
      window.dispatchEvent(new CustomEvent('open-tool-library'));
    },
  });

  
  useEffect(() => {
    const loadInitialData = async () => {
      try {
        await Promise.all([
          fetchPinnedCommands(),
          fetchTools(),
          fetchToolCategories(),
          fetchUserPrompts(),
          fetchFileSystem() // Fetch file system data on startup
        ]);
        
        
        registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
        registerUserPromptsAsCommands(() => togglePanel('userPrompts'));
        
      } catch (error) {
        console.error('Error loading initial data:', error);
      }
    };
    
    loadInitialData();
  }, [fetchPinnedCommands, fetchTools, fetchToolCategories, fetchUserPrompts, fetchFileSystem, togglePanel]);

  useEffect(() => {
    initializeCoreCommands({
      toggleSidebar: () => togglePanel('sidebar'),
      // Use the same function for both commands to ensure consistent behavior
      toggleSidebarCollapse: () => togglePanel('sidebar'),
      toggleSettings: () => togglePanel('settings'),
      openNewWindow: handleOpenNewWindow,
    });

      initializeSystemPromptCommands({
      toggleLibrary: () => window.dispatchEvent(new CustomEvent('open-system-prompt-library')),
      createNewPrompt: () => {
        window.dispatchEvent(new CustomEvent('open-system-prompt-library'));
        window.localStorage.setItem('systemPrompt_action', 'create');
      },
      editPrompt: (promptId) => {
        useModalStore.getState().openModal('systemPrompt', { editPromptId: promptId });
      },
    });

    initializeUserPromptCommands({
      toggleLibrary: () => window.dispatchEvent(new CustomEvent('open-user-prompt-library')),
      createNewPrompt: () => {
        window.dispatchEvent(new CustomEvent('open-user-prompt-library'));
        window.localStorage.setItem('userPrompt_action', 'create');
      },
      editPrompt: (promptId) => {
        window.dispatchEvent(new CustomEvent('open-user-prompt-library'));
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
        selectPrimaryModel: (guid) => handleModelSelect('primary', guid),
        selectSecondaryModel: (guid) => handleModelSelect('secondary', guid),
    });

    // Register theme commands initially and subscribe to theme changes
    const selectTheme = (themeGuid: string) => {
      useThemeStore.getState().setActiveThemeId(themeGuid);
    };
    registerThemeCommands(selectTheme);
    const unsubscribeThemes = useThemeStore.subscribe(
      (state) => [state.themes, state.activeThemeId],
      () => {
        registerThemeCommands(selectTheme);
      }
    );

    initializeVoiceInputCommand();


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
      unsubscribeThemes();
    };
  }, [models, togglePanel, handleModelSelect]);

  return null;
}
