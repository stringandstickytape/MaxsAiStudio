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
  console.log('[Theme Debug] Creating theme with data:', themeData);
  try {
    const response = await createApiRequest('/api/themes/add', 'POST')(themeData);
    if (response.success) {
      console.log('[Theme Debug] Theme created successfully:', response);
      return response.theme;
    } else {
      console.error('[Theme Debug] Failed to create theme:', response.error);
      throw new Error(response.error || 'Failed to create theme');
    }
  } catch (error) {
    console.error('[Theme Debug] Error creating theme:', error);
    throw error;
  }
}

// Expose functions to window object
window.debugThemeStore = debugThemeStore;
window.applyRandomTheme = applyRandomTheme;
window.addThemeToStore = addThemeToStore;
window.createTheme = createTheme;

// Log that we've exposed the function
console.log('[Theme Debug] Exposed createTheme function to window object');

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