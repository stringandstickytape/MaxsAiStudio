
import { useState, useEffect } from 'react';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { UserPromptLibrary } from './UserPromptLibrary';

export function UserPromptDialog() {
  const [isOpen, setIsOpen] = useState(false);
  
  useEffect(() => {
    const handleOpen = () => setIsOpen(true);
    window.addEventListener('open-user-prompt-library', handleOpen);
    return () => window.removeEventListener('open-user-prompt-library', handleOpen);
  }, []);

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-3xl h-[80vh] p-0">
        <div className="h-full overflow-hidden">
          <UserPromptLibrary
            onInsertPrompt={(prompt) => {
              if (prompt) {
                window.setPrompt(prompt.content);
                setIsOpen(false);
              }
            }}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}
