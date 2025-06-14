import { useState, useEffect, useRef, useMemo } from 'react';
import { useMediaQuery } from '@/hooks/useMediaQuery';
import { AppHeader } from './AppHeader';
import { ChatContainer } from './ChatContainer';
import { InputBar } from './InputBar';
import { CommandBar } from './CommandBar';

// Removed useStreamTokens import - streaming is now handled directly in MessageItem



import { useConvStore } from '@/stores/useConvStore';
import { useWebSocketStore } from '@/stores/useWebSocketStore';
import { usePanelStore } from '@/stores/usePanelStore';
import { setupPromptUtils } from '@/utils/promptUtils';

// AiStudioClient/src/components/ChatSpace.tsx
export function ChatSpace() {
  const isMobile = useMediaQuery('(max-width: 768px)');
  // Removed streamTokens - streaming is now handled directly in MessageItem
  const [isCommandBarOpen, setIsCommandBarOpen] = useState(false);
  const promptOverrideRef = useRef(false);
  // Only subscribe to activeConvId for header, not the full convs object
  const activeConvId = useConvStore(state => state.activeConvId);
  const { isCancelling } = useWebSocketStore();
  const { panels } = usePanelStore();
  
  // Setup window prompt utilities
  useEffect(() => {
    setupPromptUtils();
  }, []);

  // Memoize the ChatContainer to prevent unnecessary re-renders
  const memoizedChatContainer = useMemo(() => (
    <ChatContainer isMobile={isMobile} />
  ), [isMobile]);

  // Removed the effect that was automatically setting input value based on selected message
  // Now we'll only set input value when explicitly requested


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
        {memoizedChatContainer}
      </div>
      
      <div className="flex-shrink-0 w-full overflow-auto">        <InputBar
          onManageTools={openToolLibrary}
          disabled={isCancelling}
        />
      </div>
    </div>
  );
}