// src/utils/uiUtils.ts
import { useState, useEffect, useCallback, RefObject } from 'react';

/**
 * Hook to handle dialog/modal state with confirmation
 */
export function useDialogState() {
  const [isOpen, setIsOpen] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  
  const open = useCallback(() => {
    setIsOpen(true);
    setIsDirty(false);
  }, []);
  
  const close = useCallback(() => {
    if (isDirty) {
      setIsConfirmOpen(true);
    } else {
      setIsOpen(false);
    }
  }, [isDirty]);
  
  const confirmClose = useCallback(() => {
    setIsOpen(false);
    setIsConfirmOpen(false);
    setIsDirty(false);
  }, []);
  
  const cancelClose = useCallback(() => {
    setIsConfirmOpen(false);
  }, []);
  
  return {
    isOpen,
    isConfirmOpen,
    isDirty,
    setIsDirty,
    open,
    close,
    confirmClose,
    cancelClose,
    setIsOpen
  };
}

/**
 * Hook to handle click outside detection
 */
export function useOutsideClick(
  ref: RefObject<HTMLElement>,
  callback: () => void,
  deps: any[] = []
) {
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        callback();
      }
    }
    
    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ref, callback, ...deps]);
}

/**
 * Hook to handle keyboard shortcuts
 */
export function useKeyboardShortcut(
  key: string,
  callback: (e: KeyboardEvent) => void,
  options?: {
    ctrl?: boolean;
    alt?: boolean;
    shift?: boolean;
    meta?: boolean;
    preventDefault?: boolean;
  }
) {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Check if all modifiers match
      const ctrlMatch = options?.ctrl ? e.ctrlKey : !e.ctrlKey;
      const altMatch = options?.alt ? e.altKey : !e.altKey;
      const shiftMatch = options?.shift ? e.shiftKey : !e.shiftKey;
      const metaMatch = options?.meta ? e.metaKey : !e.metaKey;
      
      // Check if all conditions are met
      if (
        e.key.toLowerCase() === key.toLowerCase() &&
        ctrlMatch && altMatch && shiftMatch && metaMatch
      ) {
        if (options?.preventDefault) {
          e.preventDefault();
        }
        callback(e);
      }
    };
    
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [key, callback, options]);
}

/**
 * Hook for container resize observations
 */
export function useContainerSize<T extends HTMLElement = HTMLDivElement>(deps: any[] = []) {
  const [size, setSize] = useState({ width: 0, height: 0 });
  const ref = useState<T | null>(null);
  
  useEffect(() => {
    const element = ref[0];
    if (!element) return;
    
    const updateSize = () => {
      setSize({
        width: element.clientWidth,
        height: element.clientHeight
      });
    };
    
    // Initialize size
    updateSize();
    
    // Observe size changes
    const resizeObserver = new ResizeObserver(updateSize);
    resizeObserver.observe(element);
    
    return () => {
      resizeObserver.disconnect();
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ref[0], ...deps]);
  
  return [ref, size] as const;
}