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
    
    // Extract @import statements from CSS and add them first
    const importStatements = css.match(/@import[^;]+;/g) || [];
    const cssWithoutImports = css.replace(/@import[^;]+;/g, '');
    
    // Scope the CSS (but don't scope @import statements)
    const scopedCss = scopeStyles(cssWithoutImports, scopeId);
    
    // Combine imports and scoped CSS
    const finalCss = importStatements.join('\n') + '\n' + scopedCss;
    
    // Add base Marp container styles
    const baseStyles = `
      .marp-container[data-scope="${scopeId}"] {
        position: relative;
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
        background: var(--background);
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
      }
      
      .marp-container[data-scope="${scopeId}"] *,
      .marp-container[data-scope="${scopeId}"] *::before,
      .marp-container[data-scope="${scopeId}"] *::after {
        font-family: inherit !important;
      }
      
      /* Override any monospace inheritance from parent elements */
      .marp-code-block-container,
      .marp-code-block-container * {
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif !important;
        white-space: normal !important;
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
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-slide {
        width: 100%;
        height: 100%;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
        overflow: hidden;
        display: flex;
        flex-direction: column;
      }
      
      .marp-container[data-scope="${scopeId}"] .marp-slide section {
        width: 100%;
        height: 100%;
        box-sizing: border-box;
        overflow-y: auto;
      }
      
      /* Fix content sizing and emoji issues - less aggressive overrides */
      .marp-container[data-scope="${scopeId}"] .marp-slide section img,
      .marp-container[data-scope="${scopeId}"] .marp-slide section svg {
        height: auto;
        display: inline;
      }
      
      /* Native emoji handling - prevent scaling issues */
      .marp-container[data-scope="${scopeId}"] .marp-slide section {
        font-feature-settings: "liga" off;
      }
      
      /* Specific emoji and image controls */
      .marp-container[data-scope="${scopeId}"] .marp-slide section .emoji,
      .marp-container[data-scope="${scopeId}"] .marp-slide section img[alt*="emoji"],
      .marp-container[data-scope="${scopeId}"] .marp-slide section img[src*="twemoji"],
      .marp-container[data-scope="${scopeId}"] .marp-slide section img[src*="emoji"] {
        display: inline !important;
        vertical-align: -0.1em !important;
        transform: none !important;
      }
      
      /* Fullscreen styles */
      .marp-container[data-scope="${scopeId}"]:fullscreen {
        background: #000;
      }
      
      .marp-container[data-scope="${scopeId}"]:fullscreen .marp-slide-container {
        padding: 0;
      }
      
      .marp-container[data-scope="${scopeId}"]:fullscreen .marp-slide {
        width: 100%;
        height: 100%;
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
    
    styleElement.textContent = importStatements.join('\n') + '\n' + baseStyles + '\n' + scopedCss;
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
      // Don't scope keyframes, media queries, or other at-rules
      if (selector.trim().startsWith('@')) {
        return match;
      }
      
      // Add scope to each selector
      const scopedSelectors = selector
        .split(',')
        .map(s => {
          const trimmed = s.trim();
          
          // Handle special cases
          if (trimmed === 'body' || trimmed === 'html') {
            return `.marp-container[data-scope="${scopeId}"]`;
          }
          
          // If selector already contains section, be more careful about scoping
          if (trimmed.includes('section')) {
            // Replace 'section' with our scoped section
            return trimmed.replace(/\bsection\b/g, `.marp-container[data-scope="${scopeId}"] section`);
          }
          
          // For root-level selectors (no descendant), scope them directly
          if (!trimmed.includes(' ') && !trimmed.includes('>') && !trimmed.includes('+') && !trimmed.includes('~')) {
            return `.marp-container[data-scope="${scopeId}"] ${trimmed}`;
          }
          
          // For complex selectors, add scope at the beginning
          return `.marp-container[data-scope="${scopeId}"] ${trimmed}`;
        })
        .join(', ');
      
      return `${scopedSelectors} {`;
    });
  };

  return null; // This component only manages styles, no visual output
};