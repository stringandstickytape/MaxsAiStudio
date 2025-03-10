// src/components/ChatSpace.tsx
import { useState, useEffect } from 'react';
import { useMediaQuery } from '@/hooks/use-media-query';
import { Button } from '@/components/ui/button';
import { AppHeader } from './AppHeader';
import { ChatContainer } from './ChatContainer';
import { InputBar } from './InputBar';
import { CommandBar } from './CommandBar';
import { VoiceInputOverlay } from './VoiceInputOverlay';
import { ToolPanel } from './tools/ToolPanel';
import { useStreamTokens } from '@/hooks/useStreamTokens';
import { useVoiceInputState } from '@/commands/voiceInputCommand';
import { useModelManagement } from '@/hooks/useModelManagement';
import { useToolStore } from '@/stores/useToolStore';
import { useCommandStore } from '@/stores/useCommandStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConversationStore } from '@/stores/useConversationStore';
import { ModelType } from '@/types/modelTypes';

export function ChatSpace() {
  const isMobile = useMediaQuery("(max-width: 768px)");
  const { streamTokens } = useStreamTokens();
  const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
  const [inputValue, setInputValue] = useState('');
  const [isToolPanelOpen, setIsToolPanelOpen] = useState(false);
  
  // Zustand stores
  const { activeTools } = useToolStore();
  const { togglePanel, panels } = usePanelStore();
  const { activeConversationId } = useConversationStore();
  
  // Use the centralized management hooks
  const {
    models,
    selectedPrimaryModel,
    selectedSecondaryModel,
    handleModelSelect
  } = useModelManagement();

  // Voice input integration
  const { isVoiceInputOpen, setVoiceInputOpen, handleTranscript } = useVoiceInputState(
    (text) => {
      // This function handles the transcript when voice input is done
      setInputValue(text);
    }
  );

  // Listen for tool panel open events
  useEffect(() => {
    const handleOpenToolPanel = () => setIsToolPanelOpen(true);
    window.addEventListener('openToolPanel', handleOpenToolPanel);
    return () => window.removeEventListener('openToolPanel', handleOpenToolPanel);
  }, []);

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
