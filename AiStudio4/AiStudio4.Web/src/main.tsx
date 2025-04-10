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
      backgroundColor: '#4b0082', // indigo background for test purposes
    },
  });

  createRoot(document.getElementById('root')!).render(<App />);
})();
