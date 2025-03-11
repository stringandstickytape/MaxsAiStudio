// src/components/commands/CommandInitializer.tsx
import { useEffect } from 'react';
import { useCommandStore } from '@/stores/useCommandStore';
import { initializeCoreCommands } from '@/commands/coreCommands';
import { initializeModelCommands } from '@/plugins/modelCommands';
import { initializeVoiceCommands } from '@/plugins/voiceCommands';
import { initializeSystemPromptCommands, registerSystemPromptsAsCommands } from '@/commands/systemPromptCommands';
import { initializeSettingsCommands, registerModelCommands, registerProviderCommands } from '@/commands/settingsCommands';
import { initializeAppearanceCommands } from '@/commands/appearanceCommands';
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

    const handleOpenNewWindow = () => {
        window.open(window.location.href, '_blank');
    };

    const handleToggleConvTree = () => {
        togglePanel('convTree');
    };

    const openToolPanel = () => {
        // First try the panel system directly
        togglePanel('toolPanel');
    };

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

    useEffect(() => {
        fetchPinnedCommands();
        fetchTools();
        fetchToolCategories();
    }, [fetchPinnedCommands, fetchTools, fetchToolCategories]);

    useEffect(() => {
        initializeCoreCommands({
            toggleSidebar: () => togglePanel('sidebar'),
            toggleConvTree: handleToggleConvTree,
            toggleSettings: () => togglePanel('settings'),
            openNewWindow: handleOpenNewWindow
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
            }
        });

        initializeSettingsCommands({
            openSettings: () => togglePanel('settings')
        });

        initializeAppearanceCommands({
            openAppearanceSettings: () => {
                togglePanel('settings');
            }
        });

        initializeModelCommands({
            getAvailableModels: () => models.map(m => m.modelName),
            selectPrimaryModel: (modelName) => handleModelSelect('primary', modelName),
            selectSecondaryModel: (modelName) => handleModelSelect('secondary', modelName)
        });

        initializeVoiceCommands();

        const systemPromptsUpdated = () => {
            registerSystemPromptsAsCommands(() => togglePanel('systemPrompts'));
        };

        systemPromptsUpdated();

        const unsubscribePrompts = useSystemPromptStore.subscribe(
            (state) => state.prompts,
            () => systemPromptsUpdated()
        );

        const unsubscribeModels = useModelStore.subscribe(
            (state) => state.models,
            (models) => {
                if (models.length > 0) {
                    registerModelCommands(models, () => togglePanel('settings'));
                }
            }
        );

        const unsubscribeProviders = useModelStore.subscribe(
            (state) => state.providers,
            (providers) => {
                if (providers.length > 0) {
                    registerProviderCommands(providers, () => togglePanel('settings'));
                }
            }
        );

        const cleanupKeyboardShortcut = setupVoiceInputKeyboardShortcut();

        return () => {
            cleanupKeyboardShortcut();
            unsubscribePrompts();
            unsubscribeModels();
            unsubscribeProviders();
        };
    }, [models, togglePanel, handleModelSelect]);

    return null;
}