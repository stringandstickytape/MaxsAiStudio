// src/components/ChatSpace.tsx
import { useState, useEffect } from 'react';
import { useMediaQuery } from '@/hooks/use-media-query';
import { AppHeader } from './AppHeader';
import { ChatContainer } from './ChatContainer';
import { InputBar } from './InputBar';
import { CommandBar } from './CommandBar';
import { VoiceInputOverlay } from './VoiceInputOverlay';
import { useStreamTokens } from '@/hooks/useStreamTokens';
import { useVoiceInputState } from '@/commands/voiceInputCommand';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { useToolStore } from '@/stores/useToolStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { useConvStore } from '@/stores/useConvStore';

export function ChatSpace() {
  const isMobile = useMediaQuery('(max-width: 768px)');
  const { streamTokens } = useStreamTokens();
  const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
  const [inputValue, setInputValue] = useState('');

  const { activeTools } = useToolStore();
  const { togglePanel } = usePanelStore();
  const { activeConvId, convs, slctdMsgId } = useConvStore();

  const { selectedPrimaryModel } = useModelManagement();

  const { isVoiceInputOpen, setVoiceInputOpen, handleTranscript } = useVoiceInputState((text) => {
    setInputValue(text);
  });

  useEffect(() => {
    const handleOpenToolPanel = () => togglePanel('toolPanel');
    window.addEventListener('openToolPanel', handleOpenToolPanel);
    return () => window.removeEventListener('openToolPanel', handleOpenToolPanel);
  }, [togglePanel]);
  
  
  useEffect(() => {
    if (activeConvId && slctdMsgId && convs[activeConvId]) {
      const conv = convs[activeConvId];
      
      const messages = conv.messages;
      const slctdMsgIndex = messages.findIndex(msg => msg.id === slctdMsgId);
      
      if (slctdMsgIndex >= 0) {
        
        const nextIndex = slctdMsgIndex + 1;
        if (nextIndex < messages.length && messages[nextIndex].source === 'user') {
          
          setInputValue(messages[nextIndex].content);
        }
      }
    }
  }, [activeConvId, slctdMsgId, convs]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setIsCommandBarOpen((prev) => !prev);
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const openToolPanel = () => {
    togglePanel('toolPanel');
  };

  return (
    <>
      <div className="flex-none w-full">
        <AppHeader
          onToggleSystemPrompts={() => togglePanel('systemPrompts')}
          isCommandBarOpen={isCommandBarOpen}
          setIsCommandBarOpen={setIsCommandBarOpen}
          CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
          activeConvId={activeConvId}
        />
      </div>

      <div className="flex-1 overflow-auto w-full">
        <ChatContainer streamTokens={streamTokens} isMobile={isMobile} />
      </div>

      <div className="flex-none h-[30vh] border-t w-full">
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
