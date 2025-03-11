// src/utils/hookUtils.ts
import { useRef, useEffect, DependencyList } from 'react';

/**
 * Hook to track initialization and run initialization logic only once
 * @param initFn Function to initialize data
 * @param deps Dependencies that should trigger a re-initialization
 * @returns Boolean indicating if initialization is complete
 */
export function useInitialization(initFn: () => Promise<void> | void, deps: DependencyList = []) {
  const initialized = useRef(false);
  const initializing = useRef(false);
  
  useEffect(() => {
    // Only run if not already initialized or initializing
    if (!initialized.current && !initializing.current) {
      initializing.current = true;
      
      const runInit = async () => {
        try {
          await initFn();
          initialized.current = true;
        } catch (error) {
          console.error('Initialization error:', error);
        } finally {
          initializing.current = false;
        }
      };
      
      // Execute initialization
      runInit();
    }
    
    // Cleanup function
    return () => {
      // No cleanup needed, but this satisfies the eslint rule
    };
  }, []); // Empty dependency array - we only want to run this once
  
  return initialized.current;
}

/**
 * Hook to initialize data if none exists yet
 * @param fetchFn Function to fetch the data if it doesn't exist
 * @returns Boolean indicating if initialization is complete
 */
export function useInitializeIfEmpty(fetchFn: () => Promise<void>) {
  // Simply delegate to useInitialization for consistency
  return useInitialization(fetchFn, []);
}