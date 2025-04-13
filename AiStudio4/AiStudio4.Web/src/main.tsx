import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';
import { useThemeStore, debugThemeStore, applyRandomTheme, addThemeToStore } from './stores/useThemeStore';
import { createApiRequest } from '@/utils/apiUtils';

// Add TypeScript declarations for window object extensions
declare global {
  interface Window {
    debugThemeStore: typeof debugThemeStore;
    applyRandomTheme: typeof applyRandomTheme;
    addThemeToStore: typeof addThemeToStore;
    applyLLMTheme: (themeJson: any) => void;
    createTheme: (themeData: any) => Promise<any>;
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

// Expose functions to window object

(async () => {
  await ThemeManager.discoverThemes();
  console.log('Theme schema:', ThemeManager.getSchema());

  try {
    // Render the app
    createRoot(document.getElementById('root')!).render(<App />);
  } catch (error) {
    console.error('Failed to initialize themes:', error);
    // Render the app anyway, as we can still function without themes
    createRoot(document.getElementById('root')!).render(<App />);
  }
})();