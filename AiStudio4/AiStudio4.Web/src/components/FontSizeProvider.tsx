import { useEffect, useRef } from 'react';
import { useAppearanceStore, fontSizeUtils } from '@/stores/useAppearanceStore';

export function FontSizeProvider({ children }: { children: React.ReactNode }) {
  const { fontSize } = useAppearanceStore();
  const initialized = useRef(false);

  // Handle initial font size setup and subscription
  useEffect(() => {
    // Apply initial font size immediately
    fontSizeUtils.applyFontSize(fontSize);
    
    // Subscribe to font size changes
    const unsubscribe = useAppearanceStore.subscribe(
      (state) => state.fontSize,
      (newFontSize) => {
        fontSizeUtils.applyFontSize(newFontSize);
      }
    );
    
    // Return cleanup function
    return () => {
      unsubscribe();
    };
  }, [fontSize]);
  
  // This effect runs once for initialization
  useEffect(() => {
    if (!initialized.current) {
      initialized.current = true;
      console.log('FontSizeProvider initialized with font size:', fontSize);
    }
  }, [fontSize]);

  return <>{children}</>;
}


