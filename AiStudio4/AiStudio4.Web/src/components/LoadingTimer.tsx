// AiStudio4.Web\src\components\LoadingTimer.tsx
import { useEffect, useState, useRef } from 'react';

// IMPORTANT: Do not add dynamic status messages (e.g., 'Analyzing...', 'Generating...')
// to this component. Keep the loading indicator purely visual with timing.
// Do not remove this comment.

export function LoadingTimer() {
  const [seconds, setSeconds] = useState(0);
  const [visible, setVisible] = useState(true);
  const [shouldRender, setShouldRender] = useState(true);
  
  // Container ref for measuring
  const containerRef = useRef<HTMLDivElement>(null);

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
      {/* Define animations */}
      <style>
        {`
          @keyframes pulse-opacity {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.4; }
          }
          
          @keyframes electric-pulse {
            0%, 100% { filter: drop-shadow(0 0 2px rgba(59, 130, 246, 0.7)); }
            50% { filter: drop-shadow(0 0 6px rgba(59, 130, 246, 0.9)); }
          }
          
          @keyframes lightning-loop {
            0%, 9%, 11%, 100% { opacity: 0; }
            10% { opacity: 1; }
          }
          
          @keyframes spark {
            0% { transform: scale(0); opacity: 1; }
            50% { opacity: 0.7; }
            100% { transform: scale(1.2); opacity: 0; }
          }
          
          @keyframes progress-animation {
            from { stroke-dashoffset: 283; } /* 2 * PI * 45 (radius) */
            to { stroke-dashoffset: 0; }
          }
          
          .electricity-container {
            position: relative;
            width: 100%;
            height: 100%;
            padding: 20px 0;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            overflow: hidden;
          }
          

          
          /* Electric arcs */
          .electric-arc {
            position: absolute;
            background: linear-gradient(90deg, transparent, rgba(59, 130, 246, 0.7), transparent);
            height: 1px;
            width: 40px;
            animation: spark 1.5s infinite;
            opacity: 0.7;
            z-index: 1;
          }
          
          .progress-ring {
            transform: rotate(-90deg);
          }
          
          .progress-ring-circle {
            stroke-dasharray: 283; /* 2 * PI * 45 (radius) */
            transition: stroke-dashoffset 0.5s;
            transform-origin: center;
          }
        `}
      </style>
      <div
        ref={containerRef}
        className={`electricity-container transition-all duration-300 ease-in-out transform ${
          visible ? 'translate-y-0 opacity-100' : '-translate-y-full opacity-0'
        }`}
      >

        
        {/* Main timer component with electrical theme */}
        <div className="relative w-28 h-28 flex items-center justify-center">
          {/* Electric sparks - positioned absolutely */}
          <div className="electric-arc absolute -top-4 -right-6 rotate-45" style={{animationDelay: '0.2s'}}></div>
          <div className="electric-arc absolute top-10 -right-8" style={{animationDelay: '0.7s'}}></div>
          <div className="electric-arc absolute -bottom-4 -right-6 -rotate-45" style={{animationDelay: '1.1s'}}></div>
          <div className="electric-arc absolute -bottom-6 left-10 rotate-90" style={{animationDelay: '0.5s'}}></div>
          <div className="electric-arc absolute -top-6 left-10 rotate-90" style={{animationDelay: '0.9s'}}></div>
          <div className="electric-arc absolute -top-4 -left-6 -rotate-45" style={{animationDelay: '1.3s'}}></div>
          <div className="electric-arc absolute top-10 -left-8" style={{animationDelay: '0.3s'}}></div>
          <div className="electric-arc absolute -bottom-4 -left-6 rotate-45" style={{animationDelay: '0.8s'}}></div>
          
          {/* Outer energy field - glowing ring */}
          <div className="absolute inset-0 rounded-full border-2 border-blue-400 opacity-70 animate-[electric-pulse_2s_ease-in-out_infinite] shadow-lg shadow-blue-500/50"></div>
          
          {/* Middle ring with electrical pulse */}
          <div className="absolute inset-2 rounded-full border border-blue-500 opacity-80"></div>
          
          {/* SVG Progress ring */}
          <svg className="absolute inset-4 progress-ring" width="80" height="80">
            <circle 
              className="progress-ring-circle" 
              stroke="rgba(59, 130, 246, 0.8)" 
              strokeWidth="2"
              strokeLinecap="round"
              fill="transparent"
              r="36" 
              cx="40" 
              cy="40"
              style={{
                animation: `progress-animation ${60}s linear infinite`,
                strokeDashoffset: 283 - (283 * (seconds % 60)) / 60
              }}
            />
          </svg>

          {/* Inner spinning electric field */}
          <div className="relative w-20 h-20">
            {/* Multiple rotating arcs with electric colors */}
            <div className="absolute inset-0 rounded-full border-2 border-transparent border-t-blue-400 border-r-blue-300/30 animate-spin"></div>
            <div className="absolute inset-1 rounded-full border-2 border-transparent border-t-cyan-400 animate-spin [animation-duration:3.5s] [animation-direction:reverse]"></div>
            <div className="absolute inset-2 rounded-full border border-transparent border-t-white/70 animate-spin [animation-duration:1.8s]"></div>

            {/* Inner glowing core with time display */}
            <div className="absolute inset-3 flex items-center justify-center rounded-full bg-gray-900 shadow-inner overflow-hidden">
              {/* Energy core background effect */}
              <div className="absolute inset-0 bg-gradient-to-br from-blue-900/30 to-blue-600/10 animate-pulse"></div>
              
              {/* Digital timer display */}
              <div className="relative z-10 flex flex-col items-center">
                <span className="text-blue-300 font-mono text-sm font-bold tracking-wider">
                  {formatTime(seconds)}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Status text with electric styling */}
        <div className="mt-6 text-sm flex flex-col items-center"> 
          <span className="text-blue-300 font-medium tracking-wide animate-[pulse-opacity_2s_ease-in-out_infinite]">
            AI is thinking<span className="inline-block animate-pulse">...</span>
          </span>
        </div>
      </div>
    </>
  );
}