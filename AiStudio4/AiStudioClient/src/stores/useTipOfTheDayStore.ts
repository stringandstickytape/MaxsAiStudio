import { create } from 'zustand';
import type { TipOfTheDay } from '@/types/tipOfTheDay';
import { createApiRequest } from '@/utils/apiUtils';

interface TipOfTheDayState {
  // State
  currentTip: TipOfTheDay | null;
  isVisible: boolean;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  setVisible: (visible: boolean) => void;
  showTip: () => void;
  hideTip: () => void;
  fetchNextTip: () => Promise<void>;
  getCurrentTip: () => TipOfTheDay | null;
}

// API endpoints
const getTipOfTheDay = createApiRequest('/api/tipOfTheDay/getTipOfTheDay', 'POST');

export const useTipOfTheDayStore = create<TipOfTheDayState>((set, get) => ({
  // Initial state
  currentTip: null,
  isVisible: false,
  isLoading: false,
  error: null,
  
  // Actions
  setVisible: (visible) => set({ isVisible: visible }),
  
  showTip: () => set({ isVisible: true }),
  
  hideTip: () => set({ isVisible: false }),
  
  fetchNextTip: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await getTipOfTheDay();
      if (response.success && response.data) {
        const tip = response.data as TipOfTheDay;
        set({
          currentTip: tip,
          isLoading: false,
        });
      } else {
        set({ 
          error: response.error || 'Failed to fetch tip', 
          isLoading: false 
        });
      }
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Failed to fetch tip', 
        isLoading: false 
      });
    }
  },
  
  getCurrentTip: () => {
    const { currentTip } = get();
    return currentTip;
  },
}));

// Debug utilities
if (process.env.NODE_ENV === 'development') {
  window.tipOfTheDayStore = {
    getState: () => useTipOfTheDayStore.getState(),
    setState: (state: Partial<TipOfTheDayState>) => useTipOfTheDayStore.setState(state),
    subscribe: useTipOfTheDayStore.subscribe,
  };
}