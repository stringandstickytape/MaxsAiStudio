
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
import { useConvStore } from '@/stores/useConvStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';

export function ChatSpace() {
  const isMobile = useMediaQuery('(max-width: 768px)');
  const { streamTokens } = useStreamTokens();
  const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
  const [inputValue, setInputValue] = useState('');

  const { activeTools } = useToolStore();
  const { activeConvId, convs, slctdMsgId } = useConvStore();

  const { selectedPrimaryModel } = useModelManagement();
  const { isCancelling } = useWebSocketStore();

  const { isVoiceInputOpen, setVoiceInputOpen, handleTranscript } = useVoiceInputState((text) => {
    setInputValue(text);
  });

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

    const handleSelectPrimaryModel = () => {
      setIsCommandBarOpen(true);
      setTimeout(() => {
        const element = document.getElementById('command-input') as HTMLInputElement;
        if (element) {
          element.focus();
          element.value = 'select primary model';
          const inputEvent = new Event('input', { bubbles: true });
          element.dispatchEvent(inputEvent);
        }
      }, 100);
    };

    const handleSelectSecondaryModel = () => {
      setIsCommandBarOpen(true);
      setTimeout(() => {
        const element = document.getElementById('command-input') as HTMLInputElement;
        if (element) {
          element.focus();
          element.value = 'select secondary model';
          const inputEvent = new Event('input', { bubbles: true });
          element.dispatchEvent(inputEvent);
        }
      }, 100);
    };

    window.addEventListener('keydown', handleKeyDown);
    window.addEventListener('select-primary-model', handleSelectPrimaryModel);
    window.addEventListener('select-secondary-model', handleSelectSecondaryModel);
    
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('select-primary-model', handleSelectPrimaryModel);
      window.removeEventListener('select-secondary-model', handleSelectSecondaryModel);
    };
  }, []);

  
  const openToolLibrary = () => {
    window.dispatchEvent(new CustomEvent('open-tool-library'));
  };

  return (
    <>
      <div className="flex-none w-full">
        <AppHeader
          isCommandBarOpen={isCommandBarOpen}
          setIsCommandBarOpen={setIsCommandBarOpen}
          CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
          activeConvId={activeConvId}
        />
      </div>

      <div className="flex-1 overflow-auto w-full">
        <ChatContainer streamTokens={streamTokens} isMobile={isMobile} />
      </div>

      <div className="flex-none w-full">
        <InputBar
          selectedModel={selectedPrimaryModel}
          onVoiceInputClick={() => setVoiceInputOpen(true)}
          inputValue={inputValue}
          onInputChange={setInputValue}
          activeTools={activeTools}
          onManageTools={openToolLibrary}
          disabled={isCancelling}
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

