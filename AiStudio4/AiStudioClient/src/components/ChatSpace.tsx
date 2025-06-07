import { useState, useEffect, useRef } from 'react';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { AppHeader } from './AppHeader';
import { ChatContainer } from './ChatContainer';
import { Attachment } from '@/types/attachment';
import { InputBar } from './InputBar';
import { CommandBar } from './CommandBar';

import { useStreamTokens } from '@/hooks/useStreamTokens';

import { useModelManagement } from '@/hooks/useResourceManagement';
import { useToolStore } from '@/stores/useToolStore';
import { useConvStore } from '@/stores/useConvStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { usePanelStore } from '@/stores/usePanelStore';

// AiStudioClient/src/components/ChatSpace.tsx
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

  // Removed the effect that was automatically setting input value based on selected message
  // Now we'll only set input value when explicitly requested

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
    <div className="flex flex-col h-full w-full">
      <div className="flex-none w-full">
        <AppHeader
          isCommandBarOpen={isCommandBarOpen}
          setIsCommandBarOpen={setIsCommandBarOpen}
          CommandBarComponent={<CommandBar isOpen={isCommandBarOpen} setIsOpen={setIsCommandBarOpen} />}
          sidebarOpen={panels.sidebar?.isOpen || false}
          rightSidebarOpen={(panels.settings?.isOpen) || false}
          activeConvId={activeConvId}
        />
      </div>
      
      <div className="flex-1 overflow-auto min-h-0">
        <ChatContainer streamTokens={streamTokens} isMobile={isMobile} />
      </div>
      
      <div className="flex-shrink-0 w-full max-h-[400px] overflow-auto">
        <InputBar
          selectedModel={selectedPrimaryModel}
          inputValue={inputValue}
          onInputChange={setInputValue}
          activeTools={activeTools}
          onManageTools={openToolLibrary}
          disabled={isCancelling}
          onAttachmentChange={handleAttachmentChange}
        />
      </div>
    </div>
  );
}