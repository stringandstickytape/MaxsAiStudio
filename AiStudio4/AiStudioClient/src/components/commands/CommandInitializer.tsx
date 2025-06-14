// AiStudio4/AiStudioClient/src/components/commands/CommandInitializer.tsx
import { useEffect } from 'react';
import { initializeCoreCommands } from '@/commands/coreCommands';
import { initializeModelCommands } from '@/commands/modelCommands';
import { initializeVoiceInputCommand } from '@/commands/voiceInputCommand';
import { initializeSystemPromptCommands } from '@/commands/systemPromptCommands';
import { initializeUserPromptCommands } from '@/commands/userPromptCommands';
import { initializeSettingsCommands } from '@/commands/settingsCommands';
import { initializeAppearanceCommands } from '@/commands/appearanceCommands';
import { registerThemeCommands } from '@/commands/themeCommands';
import { useThemeStore } from '@/stores/useThemeStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { useToolCommands } from '@/hooks/useToolCommands';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';
import { useModalStore } from '@/stores/useModalStore';
import { useFileSystemManagement } from '@/hooks/useFileSystemManagement';
import { useMcpServerStore } from '@/stores/useMcpServerStore';
import useProjectStore from '@/stores/useProjectStore';
import { registerMcpServersAsCommands, initializeMcpServerManagementCommand } from '@/commands/mcpServerCommands';

export function CommandInitializer() {
  // Panel and UI management
  const { togglePanel } = usePanelStore();
  
  // Command and pinned commands management
  const { fetchPinnedCommands } = usePinnedCommandsStore();
  
  // Tools management
  const { fetchTools, fetchToolCategories } = useToolsManagement();
  
  // User prompts management
  const { fetchUserPrompts } = useUserPromptManagement();
  
  // File system management
  const { fetchFileSystem } = useFileSystemManagement();
  
  // Project management
  const { fetchProjects } = useProjectStore();
  
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
          fetchMcpServers(),
          fetchProjects()
        ]);
        // After fetching, the stores will automatically handle command registration.
      } catch (error) {
        console.error('Error loading initial data:', error);
      }
    };
    
    loadInitialData();
  }, [fetchPinnedCommands, fetchTools, fetchToolCategories, fetchUserPrompts, fetchFileSystem, fetchMcpServers, fetchProjects]);

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


  // Initialize static commands once
  useEffect(() => {
    // Register static command groups and commands
    initializeCoreCommands({
      toggleSidebar: () => togglePanel('sidebar'),
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
    
    initializeSettingsCommands();
    
    initializeAppearanceCommands();
    
    initializeMcpServerManagementCommand();
    initializeVoiceInputCommand();
    
    // Initial empty registration for model commands
    initializeModelCommands({ 
      getAvailableModels: () => [], 
      selectPrimaryModel: () => {}, 
      selectSecondaryModel: () => {} 
    });

    // Initial theme registration and subscription
    const selectTheme = (themeGuid: string) => useThemeStore.getState().setActiveThemeId(themeGuid);
    registerThemeCommands(selectTheme);
    const unsubscribeThemes = useThemeStore.subscribe(
      (state) => [state.themes, state.activeThemeId],
      () => registerThemeCommands(selectTheme)
    );

    // Cleanup theme subscription
    return () => {
      unsubscribeThemes();
    };
  }, []); // Empty dependency array ensures this runs only ONCE.

  return null;
}