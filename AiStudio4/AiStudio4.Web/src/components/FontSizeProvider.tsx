// src/components/FontSizeProvider.tsx
import { useEffect } from 'react';
import { useAppearanceStore } from '@/stores/useAppearanceStore';

export function FontSizeProvider({ children }: { children: React.ReactNode }) {
  const { fontSize, loadAppearanceSettings } = useAppearanceStore();

  
  useEffect(() => {
    
    document.documentElement.style.fontSize = `${fontSize}px`;

    
    loadAppearanceSettings().catch((err) => {
      console.warn('Failed to load appearance settings:', err);
    });

    
    const unsubscribe = useAppearanceStore.subscribe(
      (state) => state.fontSize,
      (newFontSize) => {
        document.documentElement.style.fontSize = `${newFontSize}px`;
      },
    );

    
    return () => {
      unsubscribe();
    };
  }, [loadAppearanceSettings]);

  
  return <>{children}</>;
}


