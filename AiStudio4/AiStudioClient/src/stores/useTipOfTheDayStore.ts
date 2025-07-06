import { create } from 'zustand';
import type { TipOfTheDay, TipOfTheDaySettings } from '@/types/tipOfTheDay';
import { createApiRequest } from '@/utils/apiUtils';

interface TipOfTheDayState {
  // State
  tips: TipOfTheDay[];
  currentTipIndex: number;
  showOnStartup: boolean;
  isVisible: boolean;
  isLoading: boolean;
  error: string | null;
  
  // Actions
  setTips: (tips: TipOfTheDay[]) => void;
  setCurrentTipIndex: (index: number) => void;
  setShowOnStartup: (show: boolean) => void;
  setVisible: (visible: boolean) => void;
  showTip: () => void;
  hideTip: () => void;
  nextTip: () => void;
  previousTip: () => void;
  fetchSettings: () => Promise<void>;
  saveSettings: () => Promise<void>;
  getCurrentTip: () => TipOfTheDay | null;
}

// API endpoints
const getTipSettings = createApiRequest('/api/tipOfTheDay/getSettings', 'POST');
const saveTipSettings = createApiRequest('/api/tipOfTheDay/saveSettings', 'POST');

export const useTipOfTheDayStore = create<TipOfTheDayState>((set, get) => ({
  // Initial state
  tips: [],
  currentTipIndex: 0,
  showOnStartup: true,
  isVisible: false,
  isLoading: false,
  error: null,
  
  // Actions
  setTips: (tips) => set({ tips }),
  
  setCurrentTipIndex: (index) => set({ currentTipIndex: index }),
  
  setShowOnStartup: (show) => set({ showOnStartup: show }),
  
  setVisible: (visible) => set({ isVisible: visible }),
  
  showTip: () => set({ isVisible: true }),
  
  hideTip: () => set({ isVisible: false }),
  
  nextTip: () => {
    const { tips, currentTipIndex } = get();
    if (tips.length > 0) {
      const nextIndex = (currentTipIndex + 1) % tips.length;
      set({ currentTipIndex: nextIndex });
    }
  },
  
  previousTip: () => {
    const { tips, currentTipIndex } = get();
    if (tips.length > 0) {
      const prevIndex = currentTipIndex === 0 ? tips.length - 1 : currentTipIndex - 1;
      set({ currentTipIndex: prevIndex });
    }
  },
  
  fetchSettings: async () => {
    set({ isLoading: true, error: null });
    try {
      const response = await getTipSettings();
      if (response.success && response.data) {
        const settings = response.data as TipOfTheDaySettings;
        set({
          tips: settings.tips || [],
          currentTipIndex: settings.currentTipIndex || 0,
          showOnStartup: settings.showOnStartup ?? true,
          isLoading: false,
        });
      } else {
        set({ 
          error: response.error || 'Failed to fetch tip settings', 
          isLoading: false 
        });
      }
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Failed to fetch tip settings', 
        isLoading: false 
      });
    }
  },
  
  saveSettings: async () => {
    const { tips, currentTipIndex, showOnStartup } = get();
    set({ isLoading: true, error: null });
    
    try {
      const settings: TipOfTheDaySettings = {
        tips,
        currentTipIndex,
        showOnStartup,
      };
      
      const response = await saveTipSettings({ settings });
      if (!response.success) {
        set({ 
          error: response.error || 'Failed to save tip settings', 
          isLoading: false 
        });
      } else {
        set({ isLoading: false });
      }
    } catch (error) {
      set({ 
        error: error instanceof Error ? error.message : 'Failed to save tip settings', 
        isLoading: false 
      });
    }
  },
  
  
  getCurrentTip: () => {
    const { tips, currentTipIndex } = get();
    return tips.length > 0 ? tips[currentTipIndex] : null;
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