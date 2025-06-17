// AiStudio4/AiStudioClient/src/components/diagrams/abc-renderer.tsx
import * as ABCJS from 'abcjs';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef, useState, useCallback } from 'react';

// Helper function to get theme variable from CSS custom properties
const getThemeVariable = (varName: string, fallback: string): string => {
    if (typeof window === 'undefined') return fallback;
    const value = getComputedStyle(document.documentElement).getPropertyValue(varName.trim()).trim();
    return value || fallback;
};

// Function to configure ABCJS rendering options based on current global CSS variables
const getAbcjsRenderingOptions = () => {
    return {
        add_classes: true,
        responsive: 'resize',
        staffwidth: 600,
        scale: 1.0,
        format: {
            titlefont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 16`,
            subtitlefont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 14`,
            composerfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            partsfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            tempofont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            gchordfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            annotationfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            vocalfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
            wordsfont: `${getThemeVariable('--global-font-family', 'Inter, system-ui, sans-serif')} 12`,
        },
        foregroundColor: getThemeVariable('--global-text-color', '#e0e0e0'),
        backgroundColor: 'transparent', // Let the container handle background
    };
};

// Helper function to create wrapper for SVG with zoom/pan controls
const wrapSvgWithZoomControls = (container: HTMLElement): void => {
    const existingContent = container.innerHTML;
    container.innerHTML = `
    <div class="abc-zoom-container">
      <div class="abc-zoom-controls">
        <button class="abc-zoom-in" title="Zoom In">+</button>
        <button class="abc-zoom-out" title="Zoom Out">-</button>
        <button class="abc-zoom-reset" title="Reset View">Reset</button>
      </div>
      <div class="abc-diagram-container">
        ${existingContent}
      </div>
      <div class="abc-zoom-hint">Use mouse wheel to zoom, drag to pan, or use the controls above</div>
    </div>
  `;
};

// Function to initialize zoom/pan functionality on an element
const initializeZoomPan = (container: HTMLElement) => {
    const diagramContainer = container.querySelector('.abc-diagram-container');
    const svg = diagramContainer?.querySelector('svg');

    if (!diagramContainer || !svg) return;

    svg.style.transformOrigin = '0 0';
    svg.style.transition = 'transform 0.1s';

    let scale = 1;
    let translateX = 0;
    let translateY = 0;
    let dragging = false;
    let lastX = 0;
    let lastY = 0;

    const applyTransform = () => {
        svg.style.transform = `translate(${translateX}px, ${translateY}px) scale(${scale})`;
    };

    const zoomInBtn = container.querySelector('.abc-zoom-in');
    if (zoomInBtn) {
        zoomInBtn.addEventListener('click', () => {
            scale = Math.min(scale + 0.1, 3);
            applyTransform();
        });
    }

    const zoomOutBtn = container.querySelector('.abc-zoom-out');
    if (zoomOutBtn) {
        zoomOutBtn.addEventListener('click', () => {
            scale = Math.max(scale - 0.1, 0.5);
            applyTransform();
        });
    }

    const resetBtn = container.querySelector('.abc-zoom-reset');
    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            scale = 1;
            translateX = 0;
            translateY = 0;
            applyTransform();
        });
    }

    diagramContainer.addEventListener('wheel', (e) => {
        e.preventDefault();
        const rect = diagramContainer.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;
        const delta = e.deltaY < 0 ? 0.1 : -0.1;
        const newScale = Math.max(0.5, Math.min(3, scale + delta));
        if (newScale !== scale) {
            const scaleRatio = newScale / scale;
            translateX -= (mouseX - translateX) * (scaleRatio - 1);
            translateY -= (mouseY - translateY) * (scaleRatio - 1);
            scale = newScale;
            applyTransform();
        }
    });

    diagramContainer.addEventListener('mousedown', (e) => {
        if (e.button === 0) {
            dragging = true;
            lastX = e.clientX;
            lastY = e.clientY;
            svg.style.transition = 'none';
        }
    });

    diagramContainer.addEventListener('mousemove', (e) => {
        if (dragging) {
            const deltaX = e.clientX - lastX;
            const deltaY = e.clientY - lastY;
            translateX += deltaX;
            translateY += deltaY;
            lastX = e.clientX;
            lastY = e.clientY;
            applyTransform();
        }
    });

    const endDrag = () => {
        if (dragging) {
            dragging = false;
            svg.style.transition = 'transform 0.1s';
        }
    };

    diagramContainer.addEventListener('mouseup', endDrag);
    diagramContainer.addEventListener('mouseleave', endDrag);
};

// Function to inject ABC-specific styles
const injectAbcStyles = () => {
    const styleId = 'abc-zoom-styles';
    if (!document.getElementById(styleId)) {
        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
        .abc-zoom-container {
          display: flex;
          flex-direction: column;
          width: 100%;
        }
        .abc-zoom-controls {
          display: flex;
          justify-content: center;
          margin-bottom: 8px;
          gap: 8px;
        }
        .abc-zoom-controls button {
          padding: 4px 8px;
          background-color: var(--global-secondary-color, #374151);
          border: none;
          border-radius: var(--global-border-radius, 4px);
          color: var(--global-text-color, white);
          cursor: pointer;
        }
        .abc-zoom-controls button:hover {
          background-color: var(--global-primary-color, #4b5563);
        }
        .abc-diagram-container {
          overflow: hidden;
          border: 1px solid var(--global-border-color, #374151);
          border-radius: var(--global-border-radius, 4px);
          padding: 16px;
          min-height: 200px;
          position: relative;
          cursor: move;
          background-color: var(--global-background-color, #111827);
        }
        .abc-zoom-hint {
          font-size: 0.75rem;
          color: var(--global-secondary-color, #6b7280);
          text-align: center;
          margin-top: 4px;
        }
        /* Style the ABC notation elements with theme colors */
        .abc-diagram-container svg {
          background-color: transparent;
        }
        .abc-diagram-container .abcjs-note {
          fill: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-staff {
          stroke: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-bar {
          stroke: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-text {
          fill: var(--global-text-color, #e0e0e0);
        }
        .abc-diagram-container .abcjs-title {
          fill: var(--global-text-color, #e0e0e0);
        }
      `;
        document.head.appendChild(style);
    }
};

export const AbcRenderer: CodeBlockRenderer = {
    type: ['abc'],
    initialize: () => {
        injectAbcStyles(); // Initial style injection
    },
    render: async () => {
        // No async rendering needed - handled by Component
    },
    Component: ({ content, className }) => {
        const containerRef = useRef<HTMLDivElement>(null);
        const [error, setError] = useState<string | null>(null);
        const [notationId] = useState('abc-notation-' + Math.random().toString(36).substring(2, 9));

        const renderNotation = useCallback(async () => {
            if (!containerRef.current) return;
            
            // Clear previous content and errors
            containerRef.current.innerHTML = '';
            setError(null);

            try {
                // Validate ABC content - basic check for ABC notation structure
                if (!content.trim()) {
                    setError('Empty ABC notation content');
                    return;
                }

                // Basic ABC validation - should contain at least one note or header
                const hasHeader = /^[A-Z]:\s*/.test(content);
                const hasNotes = /[A-Ga-g]/.test(content);
                
                if (!hasHeader && !hasNotes) {
                    setError('Invalid ABC notation: content should contain ABC headers (X:, T:, etc.) or musical notes');
                    return;
                }

                // Create a temporary container for rendering
                const tempContainer = document.createElement('div');
                tempContainer.id = notationId;
                containerRef.current.appendChild(tempContainer);

                // Render the ABC notation
                const renderingOptions = getAbcjsRenderingOptions();
                
                try {
                    ABCJS.renderAbc(tempContainer, content, renderingOptions);
                    
                    // Check if rendering was successful by looking for SVG content
                    const svg = tempContainer.querySelector('svg');
                    if (!svg) {
                        setError('ABC rendering failed: No musical notation was generated');
                        return;
                    }

                    // Wrap with zoom controls and initialize functionality
                    wrapSvgWithZoomControls(containerRef.current);
                    initializeZoomPan(containerRef.current);
                    
                } catch (renderError) {
                    const errorMessage = renderError instanceof Error ? renderError.message : String(renderError);
                    setError(`ABC rendering error: ${errorMessage}`);
                }

            } catch (err) {
                const errorMessage = err instanceof Error ? err.message : String(err);
                console.error('ABC notation rendering failed:', errorMessage);
                setError(errorMessage);
            }
        }, [content, notationId]);

        useEffect(() => {
            renderNotation(); // Initial render
            
            const handleThemeChange = () => {
                renderNotation(); // Re-render on theme change
            };

            window.addEventListener('themechange', handleThemeChange as EventListener);
            return () => {
                window.removeEventListener('themechange', handleThemeChange as EventListener);
            };
        }, [renderNotation]); // renderNotation dependency includes content and notationId

        // Enhanced JSX for displaying the error or the notation
        return (
            <div className={`${className || ''}`}>
                {error ? (
                    <div className="p-3 border border-red-500/50 rounded-md bg-red-900/20 text-red-300 text-xs">
                        <p className="font-bold mb-2">ABC Notation Error</p>
                        <pre className="whitespace-pre-wrap font-mono text-sm">
                            {error}
                        </pre>
                    </div>
                ) : (
                    <div ref={containerRef} className="abc-container w-full" />
                )}
            </div>
        );
    },
};