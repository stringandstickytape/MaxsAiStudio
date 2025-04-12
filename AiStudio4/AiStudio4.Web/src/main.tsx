import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';
import { useThemeStore, debugThemeStore, applyRandomTheme, addThemeToStore } from './stores/useThemeStore';

(async () => {
  await ThemeManager.discoverThemes();
  console.log('Theme schema:', ThemeManager.getSchema());

  try {
    // Load themes and active theme from server
    const { loadThemes, loadActiveTheme } = useThemeStore.getState();
    
    // First load all themes from the server
    await loadThemes();
    
    // Then load and apply the active theme
    await loadActiveTheme();
    
    // Explicitly assign window functions
    window.debugThemeStore = debugThemeStore;
    window.applyRandomTheme = applyRandomTheme;
    window.addThemeToStore = addThemeToStore;

    // Render the app after themes are loaded
    createRoot(document.getElementById('root')!).render(<App />);
  } catch (error) {
    console.error('Failed to initialize themes:', error);
    // Application will crash if no themes are available from the server
    throw new Error('Failed to load themes from server. Application cannot start.');
  }
})();