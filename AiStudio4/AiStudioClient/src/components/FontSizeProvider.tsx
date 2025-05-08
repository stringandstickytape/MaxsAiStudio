import { useEffect, useRef } from 'react';
import { useAppearanceStore, fontSizeUtils } from '@/stores/useAppearanceStore';

export function FontSizeProvider({ children }: { children: React.ReactNode }) {
  const { fontSize } = useAppearanceStore();
  const initialized = useRef(false);

  
  useEffect(() => {
    
    fontSizeUtils.applyFontSize(fontSize);
    
    
    const unsubscribe = useAppearanceStore.subscribe(
      (state) => state.fontSize,
      (newFontSize) => {
        fontSizeUtils.applyFontSize(newFontSize);
      }
    );
    
    
    return () => {
      unsubscribe();
    };
  }, [fontSize]);
  
  
  useEffect(() => {
    if (!initialized.current) {
      initialized.current = true;
      
    }
  }, [fontSize]);

  return <>{children}</>;
}


