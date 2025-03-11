// src/utils/hookUtils.ts
import { useRef, useEffect, DependencyList } from 'react';


export function useInitialization(initFn: () => Promise<void> | void, deps: DependencyList = []) {
  const initialized = useRef(false);
  const initializing = useRef(false);

  useEffect(() => {
    
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

      
      runInit();
    }

    
    return () => {
      
    };
  }, []); 

  return initialized.current;
}


export function useInitializeIfEmpty(fetchFn: () => Promise<void>) {
  
  return useInitialization(fetchFn, []);
}

