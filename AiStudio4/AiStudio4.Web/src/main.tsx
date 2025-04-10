import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App.tsx';
import ThemeManager from './lib/ThemeManager';

(async () => {
  await ThemeManager.discoverThemes();

  ThemeManager.applyTheme({
    InputBar: {
      backgroundColor: '#ff69b4', // hot pink background
      borderColor: '#00ff00',     // bright green border
      textColor: '#0000ff',       // blue text
    },
  });

  createRoot(document.getElementById('root')!).render(<App />);
})();

