// src/utils/hookLifecycle.ts
import { useState, useEffect, useRef, DependencyList } from 'react';

/**
 * Lifecycle stages for resource hooks
 */
export enum HookLifecycleStage {
  Initial = 'initial',
  Loading = 'loading',
  Loaded = 'loaded',
  Error = 'error'
}

/**
 * Generic hook state interface
 */
export interface HookState<T = any, E = any> {
  data: T | null;
  error: E | null;
  stage: HookLifecycleStage;
  isLoading: boolean;
  isLoaded: boolean;
  hasError: boolean;
}

/**
 * Create a hook state object with appropriate derived properties
 */
export function createHookState<T = any, E = any>(
  data: T | null = null,
  error: E | null = null,
  stage: HookLifecycleStage = HookLifecycleStage.Initial
): HookState<T, E> {
  return {
    data,
    error,
    stage,
    isLoading: stage === HookLifecycleStage.Loading,
    isLoaded: stage === HookLifecycleStage.Loaded,
    hasError: stage === HookLifecycleStage.Error
  };
}

/**
 * Hook to track resource initialization state
 */
export function useResourceInitialization<T = any, E = any>(
  initialState: HookState<T, E> = createHookState<T, E>(),
  loadFn?: () => Promise<T>,
  dependencies: DependencyList = []
) {
  const [state, setState] = useState<HookState<T, E>>(initialState);
  const isInitializing = useRef(false);
  const hasInitialized = useRef(false);
  
  // Load function to initialize data
  const load = async () => {
    if (!loadFn || isInitializing.current) return;
    
    isInitializing.current = true;
    setState(prev => ({ ...prev, stage: HookLifecycleStage.Loading, isLoading: true }));
    
    try {
      const data = await loadFn();
      setState(createHookState(data, null, HookLifecycleStage.Loaded));
      hasInitialized.current = true;
    } catch (error) {
      setState(createHookState(null, error as E, HookLifecycleStage.Error));
      console.error('Resource initialization error:', error);
    } finally {
      isInitializing.current = false;
    }
  };
  
  // Reset function to clear state
  const reset = () => {
    setState(initialState);
    hasInitialized.current = false;
  };
  
  // Initialize data when dependencies change
  useEffect(() => {
    if (!hasInitialized.current && loadFn) {
      load();
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, dependencies);
  
  return {
    ...state,
    load,
    reset,
    isInitialized: hasInitialized.current
  };
}

/**
 * Hook to handle asynchronous actions with lifecycle state management
 */
export function useAsyncAction<T = any, P = any, E = any>() {
  const [state, setState] = useState<HookState<T, E>>(createHookState<T, E>());
  const isMounted = useRef(true);
  
  // Ensure we don't update state after unmount
  useEffect(() => {
    isMounted.current = true;
    return () => { isMounted.current = false; };
  }, []);
  
  // Execute the action and track state
  const execute = async (actionFn: (params: P) => Promise<T>, params: P) => {
    setState(createHookState(null, null, HookLifecycleStage.Loading));
    
    try {
      const result = await actionFn(params);
      if (isMounted.current) {
        setState(createHookState(result, null, HookLifecycleStage.Loaded));
      }
      return result;
    } catch (error) {
      if (isMounted.current) {
        setState(createHookState(null, error as E, HookLifecycleStage.Error));
      }
      throw error;
    }
  };
  
  // Reset the state
  const reset = () => {
    if (isMounted.current) {
      setState(createHookState());
    }
  };
  
  return {
    ...state,
    execute,
    reset
  };
}
