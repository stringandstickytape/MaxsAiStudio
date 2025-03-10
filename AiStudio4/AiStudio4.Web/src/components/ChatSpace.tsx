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
  
  // Define openToolPanel function to be used in components
  const openToolPanel = () => {
    setIsToolPanelOpen(true);
  };
  
  // Get panel states for layout calculations
  const hasLeftPanel = panels.sidebar?.isPinned || false;
  const hasRightPanel = panels.conversationTree?.isPinned || 
                      panels.settings?.isPinned || 
                      panels.systemPrompts?.isPinned || false;

  return (
    <>
      {/* Top header - fixed height */}
      <div className="flex-none h-[155px] bg-background">
        <AppHeader
          onToggleSystemPrompts={() => togglePanel('systemPrompts')}
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
          activeTools={activeTools}
          onManageTools={openToolPanel}
        />
      </div>

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
                <span className="h-5 w-5">*</span>
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