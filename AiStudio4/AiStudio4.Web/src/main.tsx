import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';
import { useThemeStore, debugThemeStore, applyRandomTheme, addThemeToStore } from './stores/useThemeStore';
import { useThemeManagement } from './hooks/useThemeManagement';

(async () => {
  await ThemeManager.discoverThemes();
  console.log('Theme schema:', ThemeManager.getSchema());

  try {
    // Create a temporary hook instance to access the theme management functions
    const { refreshThemes, loadActiveTheme } = useThemeManagement();
    
    // First load all themes from the server
    await refreshThemes();
    
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
    // Render the app anyway, as we can still function without themes
    createRoot(document.getElementById('root')!).render(<App />);
  }
})();