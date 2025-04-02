// AiStudio4.Web\src\components\LoadingTimer.tsx
import { useEffect, useState } from 'react';

export function LoadingTimer() {
  const [seconds, setSeconds] = useState(0);
  const [visible, setVisible] = useState(true);
  const [shouldRender, setShouldRender] = useState(true);

  useEffect(() => {
    const startTime = Date.now();
    const timer = setInterval(() => {
      const elapsedSeconds = Math.floor((Date.now() - startTime) / 1000);
      setSeconds(elapsedSeconds);
    }, 1000);

    return () => clearInterval(timer);
  }, []);

  // Handle component unmounting with animation
  useEffect(() => {
    return () => {
      setVisible(false);
      // Wait for animation to complete before actual unmount
      setTimeout(() => setShouldRender(false), 300);
    };
  }, []);

  const formatTime = (totalSeconds: number) => {
    if (totalSeconds < 60) {
      return `${totalSeconds}s`;
    } else {
      const minutes = Math.floor(totalSeconds / 60);
      const seconds = totalSeconds % 60;
      return `${minutes}m ${seconds.toString().padStart(2, '0')}s`;
    }
  };

  if (!shouldRender) return null;

  return (
    <div
      className={`flex flex-col items-center py-8 transition-all duration-300 ease-in-out transform ${
        visible ? 'translate-y-0 opacity-100' : '-translate-y-full opacity-0'
      }`}
    >
      <div className="relative">
        {/* Outer glow container */}
        <div className="absolute -inset-2 bg-blue-500/20 rounded-full blur-md animate-pulse"></div>
        
        {/* Spinner container */}
        <div className="relative w-16 h-16">
          {/* Track */}
          <div className="absolute inset-0 rounded-full border-4 border-gray-700/50"></div>
          
          {/* Animated spinner */}
          <div className="absolute inset-0 rounded-full border-4 border-transparent border-t-blue-500 border-r-indigo-500 animate-spin"></div>
          
          {/* Inner circle with time */}
          <div className="absolute inset-0 flex items-center justify-center rounded-full bg-gray-800">
            <span className="text-white font-mono text-sm font-bold">{formatTime(seconds)}</span>
          </div>
        </div>
      </div>
      
      <div className="mt-4 text-gray-300 text-sm flex flex-col items-center">
        <span className="font-semibold text-blue-400">AI is thinking</span>
        <span className="text-xs text-gray-500 mt-1">Waiting for response...</span>
      </div>
    </div>
  );
}