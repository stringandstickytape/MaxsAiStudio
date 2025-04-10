import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';

(async () => {
  await ThemeManager.discoverThemes();

  console.log('Theme schema:', ThemeManager.getSchema());

  ThemeManager.applyTheme({
    InputBar: {
      backgroundColor: '#ff69b4', // hot pink background for test purposes
    },
    SystemPromptComponent: {
      backgroundColor: '#4b0082',
      textColor: '#ffeb3b',
      borderColor: '#00ffff',
      borderRadius: '16px',
      fontFamily: 'Comic Sans MS, cursive',
      fontSize: '1rem',
      boxShadow: '0 0 20px #00ffff',
      pillActiveBg: '#00ff0033',
      pillInactiveBg: '#ff000033',
      popupBackground: 'rgba(0,0,0,0.7)',
      popupBorderColor: '#ff00ff',
      editBackground: '#222',
      editTextColor: '#0ff',
      // Explicitly set all color props to force CSS var injection
      // even if redundant
      pillActiveBg: '#00ff0033',
      pillInactiveBg: '#ff000033',
      popupBackground: 'rgba(0,0,0,0.7)',
      popupBorderColor: '#ff00ff',
      editBackground: '#222',
      editTextColor: '#0ff',
      style: {
        backgroundImage: 'linear-gradient(135deg, #4b0082, #8a2be2)',
        filter: 'brightness(1.2) contrast(1.1)',
        transform: 'skewY(-2deg)',
      },
      popupStyle: {
        backdropFilter: 'blur(8px)',
        borderRadius: '20px',
        boxShadow: '0 0 30px rgba(0,255,255,0.5)',
      },
      pillStyle: {
        fontWeight: 'bold',
        textTransform: 'uppercase',
        letterSpacing: '1px',
      },
      editAreaStyle: {
        fontFamily: 'Courier New, monospace',
        fontSize: '0.9rem',
      },
    },
  });

  createRoot(document.getElementById('root')!).render(<App />);
})();