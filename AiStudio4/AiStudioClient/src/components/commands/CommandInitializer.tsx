// AiStudio4/AiStudioClient/src/components/commands/CommandInitializer.tsx
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
import { useModalStore } from '@/stores/useModalStore';
import { useFileSystemManagement } from '@/hooks/useFileSystemManagement';
import { useCommandStore } from '@/stores/useCommandStore';
import { useToolStore } from '@/stores/useToolStore';
import { useFileSystemStore } from '@/stores/useFileSystemStore';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import { registerMcpServersAsCommands, initializeMcpServerManagementCommand } from '@/commands/mcpServerCommands';

export function CommandInitializer() {
  // Panel and UI management
  const { togglePanel } = usePanelStore();
  
  // Model management
  const { models, handleModelSelect } = useModelManagement();
  
  // Command and pinned commands management
  const { fetchPinnedCommands } = usePinnedCommandsStore();
  
  // Tools management
  const { fetchTools, fetchToolCategories } = useToolsManagement();
  
  // User prompts management
  const { fetchUserPrompts } = useUserPromptManagement();
  
  // File system management
  const { fetchFileSystem } = useFileSystemManagement();
  
  // MCP server management
  const {
    servers: mcpServers,
    setServerEnabled: toggleMcpServerEnabled,
    fetchServers: fetchMcpServers,
  } = useMcpServerStore();

  // UI event handlers
  const handleOpenNewWindow = () => {
    window.open(window.location.href, '_blank');
  };

  const openToolLibrary = () => {
    window.dispatchEvent(new CustomEvent('open-tool-library'));
  };

  // Register tool commands
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

  // Initial data loading
  useEffect(() => {
    const loadInitialData = async () => {
      try {
        await Promise.all([
          fetchPinnedCommands(),
          fetchTools(),
          fetchToolCategories(),
          fetchUserPrompts(),
          fetchFileSystem(),
          fetchMcpServers()
        ]);
        
        // Register commands that depend on loaded data
        registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
        registerUserPromptsAsCommands(() => togglePanel('userPrompts'));
        
      } catch (error) {
        console.error('Error loading initial data:', error);
      }
    };
    
    loadInitialData();
  }, [fetchPinnedCommands, fetchTools, fetchToolCategories, fetchUserPrompts, fetchFileSystem, fetchMcpServers, togglePanel]);

  // Register MCP server commands when servers change
  useEffect(() => {
    if (mcpServers && mcpServers.length > 0) {
      registerMcpServersAsCommands(mcpServers, toggleMcpServerEnabled);
    } else if (mcpServers && mcpServers.length === 0) {
      try {
        // Unregister group if no servers
        const { commandRegistry } = require('@/services/commandRegistry');
        commandRegistry.unregisterGroup('mcp-servers-list');
      } catch (e) {}
    }
  }, [mcpServers, toggleMcpServerEnabled]);

  // Register static MCP server management command
  useEffect(() => {
    initializeMcpServerManagementCommand();
  }, []);

  // Initialize core commands and UI interactions
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

    // Register system prompts as commands and subscribe to changes
    const systemPromptsUpdated = () => {
      registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
    };

    systemPromptsUpdated();
    
    // Register user prompts as commands and subscribe to changes
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

    // Cleanup function to unsubscribe from all subscriptions
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