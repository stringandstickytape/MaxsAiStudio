import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';
import { useThemeStore, debugThemeStore, applyRandomTheme, addThemeToStore } from './stores/useThemeStore';
import { initDebugUtils } from './utils/debugUtils';
import { useSystemPromptStore } from './stores/useSystemPromptStore';
import { useToolStore } from './stores/useToolStore';
import { createApiRequest } from '@/utils/apiUtils';
import { useGeneralSettingsStore } from './stores/useGeneralSettingsStore'; // Add this
import { Buffer } from 'buffer';

// Polyfill Buffer for gray-matter
window.Buffer = Buffer;

// Add TypeScript declarations for window object extensions
declare global {
  interface Window {
    debugThemeStore: typeof debugThemeStore;
    applyRandomTheme: typeof applyRandomTheme;
    addThemeToStore: typeof addThemeToStore;
    applyLLMTheme: (themeJson: any) => void;
    createTheme: (themeData: any) => Promise<any>;
    Buffer: typeof Buffer;
  }
}

// Create a non-hook based function to create themes
async function createTheme(themeData: any): Promise<any> {
  try {
    const response = await createApiRequest('/api/themes/add', 'POST')(themeData);
    if (response.success) {
      return response.theme;
    } else {
      throw new Error(response.error || 'Failed to create theme');
    }
  } catch (error) {
    console.error('Error creating theme:', error);
    throw error;
  }
}

// Expose functions to window object
window.debugThemeStore = debugThemeStore;
window.applyRandomTheme = applyRandomTheme;
window.addThemeToStore = addThemeToStore;
window.createTheme = createTheme;

// Function to load the default system prompt and its associated tools
async function loadDefaultSystemPromptAndTools() {
  try {
    // Get the default system prompt
    const defaultPromptResp = await createApiRequest('/api/getDefaultSystemPrompt', 'POST')({});
    if (defaultPromptResp.success && defaultPromptResp.prompt) {
      const defaultPrompt = defaultPromptResp.prompt;
      
      
      // Set the default prompt in the store
      useSystemPromptStore.getState().setDefaultPromptId(defaultPrompt.guid);
      
      // Apply associated tools if available
      if (defaultPrompt.associatedTools && defaultPrompt.associatedTools.length > 0) {
        useToolStore.getState().setActiveTools(defaultPrompt.associatedTools);
        
      }
    }
  } catch (err) {
    console.error('Failed to load default system prompt and tools at startup:', err);
  }
}

(async () => {
  await ThemeManager.discoverThemes();
  

  // Fetch and load the active theme after theme discovery
  try {
    const response = await createApiRequest('/api/themes/getActive', 'POST')({});
    if (response.success && response.themeId) {
      // Fetch all themes to populate the store (if not already done elsewhere)
      const allThemesResp = await createApiRequest('/api/themes/getAll', 'POST')({});
      if (allThemesResp.success && Array.isArray(allThemesResp.themes)) {
        // Populate the theme store so theme commands can be registered
        useThemeStore.getState().setThemes(allThemesResp.themes);
        // Find the active theme
        const activeTheme = allThemesResp.themes.find(t => t.guid === response.themeId);
        if (activeTheme) {
          ThemeManager.applyLLMTheme(activeTheme.themeJson);
        }
      }
    }
  } catch (err) {
    console.error('Failed to load and apply active theme at startup:', err);
  }
  
  // Load default system prompt and its associated tools (existing code)
  await loadDefaultSystemPromptAndTools();
  
  // Fetch general settings including temperature // <-- ADD THIS
  await useGeneralSettingsStore.getState().fetchSettings();
  
  // Initialize debug utilities (existing code)
  initDebugUtils();

  try {
    // Render the app
    createRoot(document.getElementById('root')!).render(<App />);
  } catch (error) {
    console.error('Failed to initialize themes:', error);
    // Render the app anyway, as we can still function without themes
    createRoot(document.getElementById('root')!).render(<App />);
  }
})();