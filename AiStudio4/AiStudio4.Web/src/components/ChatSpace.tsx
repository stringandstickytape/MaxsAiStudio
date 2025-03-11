// src/components/ChatSpace.tsx
import { useState, useEffect } from 'react';
import { useMediaQuery } from '@/hooks/use-media-query';
import { Button } from '@/components/ui/button';
import { AppHeader } from './AppHeader';
import { ChatContainer } from './ChatContainer';
import { InputBar } from './InputBar';
import { CommandBar } from './CommandBar';
import { VoiceInputOverlay } from './VoiceInputOverlay';
import { useStreamTokens } from '@/hooks/useStreamTokens';
import { useVoiceInputState } from '@/commands/voiceInputCommand';
import { useModelManagement } from '@/hooks/useModelManagement';
import { useToolStore } from '@/stores/useToolStore';
import { useCommandStore } from '@/stores/useCommandStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConvStore } from '@/stores/useConvStore';
import { ModelType } from '@/types/modelTypes';

export function ChatSpace() {
    const isMobile = useMediaQuery("(max-width: 768px)");
    const { streamTokens } = useStreamTokens();
    const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
    const [inputValue, setInputValue] = useState('');

    const { activeTools } = useToolStore();
    const { togglePanel, panels } = usePanelStore();
    const { activeConvId } = useConvStore();

    const {
        models,
        selectedPrimaryModel,
        selectedSecondaryModel,
        handleModelSelect
    } = useModelManagement();

    const { isVoiceInputOpen, setVoiceInputOpen, handleTranscript } = useVoiceInputState(
        (text) => {
            setInputValue(text);
        }
    );

    useEffect(() => {
        const handleOpenToolPanel = () => togglePanel('toolPanel');
        window.addEventListener('openToolPanel', handleOpenToolPanel);
        return () => window.removeEventListener('openToolPanel', handleOpenToolPanel);
    }, [togglePanel]);

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                setIsCommandBarOpen(prev => !prev);
            }
        };

        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, []);

    const handleLocalModelSelect = (modelType: ModelType, modelName: string) => {
        handleModelSelect(modelType, modelName);

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

    const openToolPanel = () => {
        togglePanel('toolPanel');
    };

    return (
        <>
            <div className="flex-none h-[155px] bg-background">
                <AppHeader
                    onToggleSystemPrompts={() => togglePanel('systemPrompts')}
                    isCommandBarOpen={isCommandBarOpen}
                    setIsCommandBarOpen={setIsCommandBarOpen}
                    CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
                    activeConvId={activeConvId}
                />
            </div>

            <div className="flex-1 overflow-auto">
                <ChatContainer
                    streamTokens={streamTokens}
                    isMobile={isMobile}
                />
            </div>

            <div className="flex-none h-[30vh] bg-background border-t">
                <InputBar
                    selectedModel={selectedPrimaryModel}
                    onVoiceInputClick={() => setVoiceInputOpen(true)}
                    inputValue={inputValue}
                    onInputChange={setInputValue}
                    activeTools={activeTools}
                    onManageTools={openToolPanel}
                />
            </div>

            <VoiceInputOverlay
                isOpen={isVoiceInputOpen}
                onClose={() => setVoiceInputOpen(false)}
                onTranscript={handleTranscript}
            />

        </>
    );
}