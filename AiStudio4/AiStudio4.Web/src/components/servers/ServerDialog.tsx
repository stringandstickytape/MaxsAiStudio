// AiStudio4.Web/src/components/servers/ServerDialog.tsx
import { useEffect } from 'react';
import { useModalStore } from '@/stores/useModalStore';

/**
 * This component is a bridge that listens for the legacy 'open-server-list' event
 * and opens the new unified ServerModal instead.
 * 
 * It doesn't render anything itself - it just provides event handling.
 */
export function ServerDialog() {
  const { openModal } = useModalStore();
  
  useEffect(() => {
    // Listen for the legacy event and open the modal using the store
    const handleOpen = () => openModal('server', {});
    window.addEventListener('open-server-list', handleOpen);
    return () => window.removeEventListener('open-server-list', handleOpen);
  }, [openModal]);

  // This component doesn't render anything
  return null;
}