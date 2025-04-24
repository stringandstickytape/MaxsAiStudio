// AiStudio4.Web/src/components/tools/ToolDialog.tsx
import { useState, useEffect } from 'react';
// import { Dialog, DialogContent } from '@/components/ui/dialog'; // Removed old import
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
} from '@/components/ui/unified-modal-dialog'; // Added new import
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
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={setIsOpen}
      variant="library" // Use library variant as a base
      size="4xl" // Corresponds to max-w-4xl
      height="xl" // Corresponds roughly to 80vh, adjust if needed
      className="p-0" // Remove default padding as ToolPanel likely handles it
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Tools</h2>
      </UnifiedModalHeader>
      <UnifiedModalContent className="p-0"> {/* Ensure content area has no padding if ToolPanel manages it */}
        {/* Render ToolPanel only when open to potentially save resources */}
        {isOpen && (
          <ToolPanel 
            isOpen={isOpen} // ToolPanel might not need isOpen prop anymore
            onClose={() => setIsOpen(false)} 
            onToolSelect={handleToolSelect}
          />
        )}
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}