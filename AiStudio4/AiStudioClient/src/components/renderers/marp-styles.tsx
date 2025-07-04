import React, { useEffect, useRef } from 'react';

interface MarpStylesProps {
  css: string;
  scopeId: string;
}

export const MarpStyles: React.FC<MarpStylesProps> = ({ css, scopeId }) => {
  const styleRef = useRef<HTMLStyleElement | null>(null);

  useEffect(() => {
    // Create a style element to inject the Marp CSS
    const styleElement = document.createElement('style');
    styleElement.setAttribute('data-marp-scope', scopeId);
    
    // Scope the CSS to prevent global style pollution
    const scopedCss = scopeStyles(css, scopeId);
    styleElement.textContent = scopedCss;
    
    // Add base Marp container styles
    const baseStyles = `
      .marp-container[data-scope="${scopeId}"] {
        position: relative;
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
        background: var(--background);
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-presentation-wrapper {
        flex: 1;
        display: flex;
        overflow: hidden;
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-slide-container {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
        background: #000;
        padding: 2rem;
        height: 400px; /* Fixed height to prevent jumping */
        min-height: 400px;
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-slide {
        width: 100%;
        max-width: 600px; /* Maximum slide width */
        height: 100%;
        max-height: 337px; /* 600 * 9/16 for 16:9 aspect ratio */
        aspect-ratio: 16/9;
        background: white;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
        overflow: hidden;
        display: flex;
        flex-direction: column;
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-slide section {
        width: 100%;
        height: 100%;
        padding: 40px;
        box-sizing: border-box;
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        justify-content: center;
      }
      
      /* Fullscreen styles */
      .marp-container[data-scope="${scopeId}"]:fullscreen {
        background: #000;
      }
      
      .marp-container[data-scope="${scopeId}"]:fullscreen .marp-slide-container {
        padding: 0;
        height: 100vh;
        min-height: 100vh;
      }
      
      .marp-container[data-scope="${scopeId}"]:fullscreen .marp-slide {
        width: calc(100vh * 16/9); /* Maintain 16:9 aspect ratio */
        height: 100vh;
        max-width: 100vw; /* Don't exceed viewport width */
        max-height: 100vh;
      }
      
      /* If screen is wider than 16:9, fit to height. If taller, fit to width */
      @media (max-aspect-ratio: 16/9) {
        .marp-container[data-scope="${scopeId}"]:fullscreen .marp-slide {
          width: 100vw;
          height: calc(100vw * 9/16);
        }
      }
      
      /* Progress bar */
      .marp-container[data-scope="${scopeId}"] .marp-progress {
        position: absolute;
        bottom: 0;
        left: 0;
        right: 0;
        height: 3px;
        background: var(--muted);
        z-index: 10;
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-progress-bar {
        height: 100%;
        background: var(--primary);
        transition: width 0.3s ease;
      }
    `;
    
    styleElement.textContent = baseStyles + '\n' + scopedCss;
    document.head.appendChild(styleElement);
    styleRef.current = styleElement;
    
    // Add scope attribute to container
    const container = document.querySelector('.marp-container');
    if (container) {
      container.setAttribute('data-scope', scopeId);
    }
    
    return () => {
      // Clean up styles on unmount
      if (styleRef.current && document.head.contains(styleRef.current)) {
        document.head.removeChild(styleRef.current);
      }
    };
  }, [css, scopeId]);

  // Helper function to scope CSS rules
  const scopeStyles = (css: string, scopeId: string): string => {
    // This is a simplified scoping mechanism
    // In production, you might want to use a proper CSS parser
    return css.replace(/([^{]+){/g, (match, selector) => {
      // Don't scope keyframes or other at-rules
      if (selector.trim().startsWith('@')) {
        return match;
      }
      
      // Add scope to each selector
      const scopedSelectors = selector
        .split(',')
        .map(s => {
          const trimmed = s.trim();
          if (trimmed === 'body' || trimmed === 'html') {
            return `.marp-container[data-scope="${scopeId}"]`;
          }
          return `.marp-container[data-scope="${scopeId}"] ${trimmed}`;
        })
        .join(', ');
      
      return `${scopedSelectors} {`;
    });
  };

  return null; // This component only manages styles, no visual output
};