import { useState, useEffect, useRef } from 'react';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { AppHeader } from './AppHeader';
import { ChatContainer } from './ChatContainer';
import { Attachment } from '@/types/attachment';
import { InputBar } from './InputBar';
import { CommandBar } from './CommandBar';
import { VoiceInputOverlay } from './VoiceInputOverlay';
import { useStreamTokens } from '@/hooks/useStreamTokens';
import { useVoiceInputState } from '@/commands/voiceInputCommand';
import { useModelManagement } from '@/hooks/useResourceManagement';
import { useToolStore } from '@/stores/useToolStore';
import { useConvStore } from '@/stores/useConvStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { usePanelStore } from '@/stores/usePanelStore';

// AiStudio4.Web/src/components/ChatSpace.tsx
export function ChatSpace() {
  const isMobile = useMediaQuery('(max-width: 768px)');
  const { streamTokens } = useStreamTokens();
  const [currentAttachments, setCurrentAttachments] = useState<Attachment[]>([]);
  const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
  const [inputValue, setInputValue] = useState('');
  const promptOverrideRef = useRef(false);

  const { activeTools } = useToolStore();
  const { activeConvId, convs, slctdMsgId } = useConvStore();

  const { selectedPrimaryModel } = useModelManagement();
  const { isCancelling } = useWebSocketStore();
  const { panels } = usePanelStore();

  const { isVoiceInputOpen, setVoiceInputOpen, handleTranscript } = useVoiceInputState((text) => {
    setInputValue(text);
  });

  useEffect(() => {
    if (promptOverrideRef.current) {
      promptOverrideRef.current = false;
      return;
    }
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

  // Listen for set-prompt event and update inputValue directly, with override flag
  useEffect(() => {
    const handleSetPrompt = (event: CustomEvent<{ text: string }>) => {
      promptOverrideRef.current = true;
      setInputValue(event.detail.text);
    };
    window.addEventListener('set-prompt', handleSetPrompt as EventListener);
    return () => {
      window.removeEventListener('set-prompt', handleSetPrompt as EventListener);
    };
  }, []);

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

  
  const handleAttachmentChange = (attachments: Attachment[]) => {
    setCurrentAttachments(attachments);
  };
  
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
                sidebarOpen={panels.sidebar?.isOpen || false}
                rightSidebarOpen={(panels.settings?.isOpen) || false}
                activeConvId={activeConvId}
            />
        </div >

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
          onAttachmentChange={handleAttachmentChange}
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