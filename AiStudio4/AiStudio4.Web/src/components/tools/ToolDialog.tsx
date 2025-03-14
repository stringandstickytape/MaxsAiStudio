// src/components/tools/ToolDialog.tsx
import { useState, useEffect } from 'react';
import { Dialog, DialogContent } from '@/components/ui/dialog';
import { ToolPanel } from './ToolPanel';
import { useToolStore } from '@/stores/useToolStore';

export function ToolDialog() {
  const [isOpen, setIsOpen] = useState(false);
  const { addActiveTool, removeActiveTool } = useToolStore();
  
  useEffect(() => {
    const handleOpen = () => setIsOpen(true);
    window.addEventListener('open-tool-library', handleOpen);
    return () => window.removeEventListener('open-tool-library', handleOpen);
  }, []);

  const handleToolSelect = (toolId: string) => {
    
    
    
  };

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogContent className="bg-gray-900 border-gray-700 text-gray-100 max-w-4xl h-[80vh] p-0">
        <div className="h-full overflow-hidden">
          <ToolPanel 
            isOpen={isOpen} 
            onClose={() => setIsOpen(false)} 
            onToolSelect={handleToolSelect}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}
