// AiStudio4.Web/src/components/tools/ToolDialog.tsx
import { useEffect } from 'react';
import { useModalStore } from '@/stores/useModalStore';

/**
 * This component is a bridge that listens for the legacy 'open-tool-library' event
 * and opens the new unified ToolModal instead.
 * 
 * It doesn't render anything itself - it just provides event handling.
 */
export function ToolDialog() {
  const { openModal } = useModalStore();
  
  useEffect(() => {
    // Listen for the legacy event and open the modal using the store
    const handleOpen = () => openModal('tool', {});
    window.addEventListener('open-tool-library', handleOpen);
    return () => window.removeEventListener('open-tool-library', handleOpen);
  }, [openModal]);

  // This component doesn't render anything
  return null;
}