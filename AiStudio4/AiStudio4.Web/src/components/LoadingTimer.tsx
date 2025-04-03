// AiStudio4.Web\src\components\LoadingTimer.tsx
import { useEffect, useState } from 'react';

// IMPORTANT: Do not add dynamic status messages (e.g., 'Analyzing...', 'Generating...')
// to this component. Keep the loading indicator purely visual with timing.
// Do not remove this comment.

export function LoadingTimer() {
  const [seconds, setSeconds] = useState(0);
  const [visible, setVisible] = useState(true);
  const [shouldRender, setShouldRender] = useState(true);

  // Timer effect
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

  return (
    <>
      {/* Define pulse-opacity animation keyframes */}
      <style>
        {`
          @keyframes pulse-opacity {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.4; }
          }
        `}
      </style>
      <div
        className={`flex flex-col items-center py-8 transition-all duration-300 ease-in-out transform ${
          visible ? 'translate-y-0 opacity-100' : '-translate-y-full opacity-0'
        }`}
      >
        <div className="relative w-20 h-20 flex items-center justify-center"> {/* Increased size slightly for rings */}
          {/* Outer static ring */}
          <div className="absolute inset-0 rounded-full border border-gray-600"></div>
          {/* Inner pulsing ring */}
          <div className="absolute inset-1 rounded-full border-2 border-blue-500 animate-[pulse-opacity_2s_cubic-bezier(0.4,0,0.6,1)_infinite]"></div>

          {/* Spinner container (original size) */}
          <div className="relative w-16 h-16">
            {/* Multiple rotating arcs with sharp colors & thinner borders */}
            <div className="absolute inset-0 rounded-full border-2 border-transparent border-t-blue-500 animate-spin"></div>
            <div className="absolute inset-0 rounded-full border-2 border-transparent border-t-orange-500 animate-spin [animation-duration:1.3s]"></div>
            <div className="absolute inset-0 rounded-full border-2 border-transparent border-t-white animate-spin [animation-duration:2.2s] [animation-direction:reverse]"></div>

            {/* Inner circle with time (high contrast) */}
            <div className="absolute inset-0 flex items-center justify-center rounded-full bg-gray-900">
              <span className="text-white font-mono text-sm font-bold">{formatTime(seconds)}</span>
            </div>
          </div>
        </div>

        <div className="mt-5 text-gray-300 text-sm flex flex-col items-center"> {/* Adjusted margin-top */}
          {/* Static status message */}
          <span className="text-xs text-gray-400 mt-1">AI is thinking...</span> {/* Slightly brighter subtext */}
        </div>
      </div>
    </>
  );
}