import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';
import { useThemeStore, debugThemeStore, applyRandomTheme, addThemeToStore } from './stores/useThemeStore';

(async () => {
  await ThemeManager.discoverThemes();

  console.log('Theme schema:', ThemeManager.getSchema());

    const theme = {
        InputBar: {
            backgroundColor: '#1a1a1a', // deep dark background
        },
        SystemPromptComponent: {
            backgroundColor: '#1a1a1a', // dark background
            textColor: '#f9e1e9',       // sakura pink text
            borderColor: '#ff8fb1',     // soft pink border
            borderRadius: '12px',
            fontFamily: '"Segoe UI", "Noto Sans JP", sans-serif',
            fontSize: '1rem',
            boxShadow: '0 4px 12px rgba(0,0,0,0.4)',

            pillActiveBg: '#ff8fb133',       // translucent pink for active pill
            pillInactiveBg: '#444',          // dark gray for inactive pill
            popupBackground: 'rgba(30,30,30,0.95)', // dark semi-transparent popup
            popupBorderColor: '#ff8fb1',     // pink border for popup
            editBackground: '#2a2a2a',       // dark edit background
            editTextColor: '#f9e1e9',        // sakura pink text in edit area

            style: {
                backgroundImage: '', // subtle dark gradient with hint of pink
                filter: 'none',
                transform: 'none',
            },

            popupStyle: {
                backdropFilter: 'blur(6px)',
                borderRadius: '14px',
                boxShadow: '0 8px 24px rgba(0,0,0,0.5)',
            },

            pillStyle: {
                fontWeight: '500',
                textTransform: 'none',
                letterSpacing: '0.5px',
                color: '#f9e1e9', // pink text on pills
            },

            editAreaStyle: {
                fontFamily: '"Consolas", monospace',
                fontSize: '0.95rem',
            },
        },
    };

    ThemeManager.applyTheme(theme);
    window.theme = theme;
    
    // Initialize theme store with the default theme
    useThemeStore.getState().addTheme({
      name: 'Default Theme',
      description: 'Default application theme',
      themeJson: theme
    });
    
    // Explicitly assign window functions
    window.debugThemeStore = debugThemeStore;
    window.applyRandomTheme = applyRandomTheme;
    window.addThemeToStore = addThemeToStore;

  createRoot(document.getElementById('root')!).render(<App />);
})();