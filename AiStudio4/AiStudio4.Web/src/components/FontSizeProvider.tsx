// src/components/FontSizeProvider.tsx
import { useEffect } from 'react';
import { useAppearanceStore } from '@/stores/useAppearanceStore';

/**
 * Component that applies the font size from the store to the document root.
 * This should be included near the root of your application.
 */
export function FontSizeProvider({ children }: { children: React.ReactNode }) {
  const { fontSize, loadAppearanceSettings } = useAppearanceStore();
  
  // Load settings on mount and apply font size
  useEffect(() => {
    // Apply initial font size
    document.documentElement.style.fontSize = `${fontSize}px`;
    
    // Load settings from server
    loadAppearanceSettings()
      .catch(err => {
        console.warn('Failed to load appearance settings:', err);
      });
      
    // Setup effect to dynamically apply font size changes
    const unsubscribe = useAppearanceStore.subscribe(
      state => state.fontSize,
      (newFontSize) => {
        document.documentElement.style.fontSize = `${newFontSize}px`;
      }
    );
    
    // Clean up subscription on unmount
    return () => {
      unsubscribe();
    };
  }, [loadAppearanceSettings]);

  // Just render children - this component only handles side effects
  return <>{children}</>;
}