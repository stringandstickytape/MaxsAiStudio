import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';

(async () => {
  await ThemeManager.discoverThemes();

  console.log('Theme schema:', ThemeManager.getSchema());

    ThemeManager.applyTheme({
        InputBar: {
            backgroundColor: '#000000', // deep charcoal background
        },
        SystemPromptComponent: {
            backgroundColor: '#000000', // slightly lighter dark background
            textColor: '#e0e0e0',       // soft light gray text
            borderColor: '#444',        // subtle border
            borderRadius: '12px',
            fontFamily: 'Segoe UI, Roboto, sans-serif',
            fontSize: '1rem',
            boxShadow: '0 4px 12px rgba(0,0,0,0.4)',

            pillActiveBg: '#3a3f5a',       // muted indigo for active pill
            pillInactiveBg: '#444',        // dark gray for inactive pill
            popupBackground: 'rgba(30,30,30,0.95)', // dark semi-transparent popup
            popupBorderColor: '#555',      // subtle border for popup
            editBackground: '#1e1e1e',     // match input bar
            editTextColor: '#ccc',         // soft gray text in edit area

            style: {
                backgroundImage: 'linear-gradient(135deg, #2a2a2a, #1e1e1e)', // subtle gradient
                filter: 'none',
                transform: 'none',
            },

            popupStyle: {
                backdropFilter: 'blur(6px)',
                borderRadius: '14px',
                boxShadow: '0 8px 24px rgba(0,0,0,0.5)'
            },

            pillStyle: {
                fontWeight: '500',
                textTransform: 'none',
                letterSpacing: '0.5px',
            },

            editAreaStyle: {
                fontFamily: 'Consolas, monospace',
                fontSize: '0.95rem',
            },
        },
    });

  createRoot(document.getElementById('root')!).render(<App />);
})();