// src/components/SystemPrompt/SystemPromptDialog.tsx
import { useState, useEffect } from 'react';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { SystemPromptLibrary } from './SystemPromptLibrary';
import { useConvStore } from '@/stores/useConvStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';

export function SystemPromptDialog() {
  const [isOpen, setIsOpen] = useState(false);
  
  const { activeConvId } = useConvStore();
  const { setConvPrompt } = useSystemPromptStore();
  const { setConvSystemPrompt } = useSystemPromptManagement();

  useEffect(() => {
    const handleOpen = () => setIsOpen(true);
    window.addEventListener('open-system-prompt-library', handleOpen);
    return () => window.removeEventListener('open-system-prompt-library', handleOpen);
  }, []);

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-3xl h-[80vh] p-0">
        <div className="h-full overflow-hidden">
          <SystemPromptLibrary
            convId={activeConvId || undefined}
            onApplyPrompt={(prompt) => {
              const convId = activeConvId;
              const promptId = prompt?.guid || prompt?.Guid;

              if (convId && promptId) {
                setConvSystemPrompt({ convId, promptId });
                setConvPrompt(convId, promptId);
              } else {
                console.error('Cannot apply prompt - missing required data:', {
                  convId,
                  promptId
                });
              }

              setIsOpen(false);
            }}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}