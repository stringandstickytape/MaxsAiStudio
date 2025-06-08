// AiStudio4/AiStudioClient/src/components/diagrams/mermaid-renderer.tsx
import mermaid from 'mermaid';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef, useState, useCallback } from 'react';

// Helper function to get theme variable from CSS custom properties
const getThemeVariable = (varName: string, fallback: string): string => {
    if (typeof window === 'undefined') return fallback;
    const value = getComputedStyle(document.documentElement).getPropertyValue(varName.trim()).trim();
    return value || fallback;
};

// Function to configure Mermaid's theme based on current global CSS variables
const configureMermaidTheme = () => {
    mermaid.initialize({
        startOnLoad: false, // Set to false, rendering is handled by the component
        theme: 'dark', // Or 'base' if you want to control more with themeVariables
        darkMode: true, // Assuming a dark mode context, can be dynamic
        securityLevel: 'strict',
        themeVariables: {
            fontFamily: getThemeVariable('--global-font-family', 'Inter, system-ui, Avenir, Helvetica, Arial, sans-serif'),
            primaryColor: getThemeVariable('--global-primary-color', '#3b82f6'),
            primaryTextColor: getThemeVariable('--global-text-color', '#e0e0e0'),
            primaryBorderColor: getThemeVariable('--global-border-color', '#374151'),
            lineColor: getThemeVariable('--global-secondary-color', '#4b5563'),
            secondaryColor: getThemeVariable('--global-secondary-color', '#475569'),
            tertiaryColor: getThemeVariable('--global-background-color', '#1f2937'), // Often background for nodes
            background: getThemeVariable('--global-background-color', '#111827'), // Diagram background
            textColor: getThemeVariable('--global-text-color', '#e0e0e0'), // General text in diagrams
            // You can map more Mermaid theme variables to global CSS variables as needed
            // mainBkg: getThemeVariable('--global-background-color', '#1f2937'), // Example for main background
            // nodeBorder: getThemeVariable('--global-border-color', '#374151'), // Example for node border
        },
    });
};

// Helper function to create wrapper for SVG with zoom/pan controls
const wrapSvgWithZoomControls = (svgContent: string): string => {
    return `
    <div class="mermaid-zoom-container">
      <div class="mermaid-zoom-controls">
        <button class="mermaid-zoom-in" title="Zoom In">+</button>
        <button class="mermaid-zoom-out" title="Zoom Out">-</button>
        <button class="mermaid-zoom-reset" title="Reset View">Reset</button>
      </div>
      <div class="mermaid-diagram-container">
        ${svgContent}
      </div>
      <div class="mermaid-zoom-hint">Use mouse wheel to zoom, drag to pan, or use the controls above</div>
    </div>
  `;
};

// Function to initialize zoom/pan functionality on an element
const initializeZoomPan = (container: HTMLElement) => {
    const diagramContainer = container.querySelector('.mermaid-diagram-container');
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

    const zoomInBtn = container.querySelector('.mermaid-zoom-in');
    if (zoomInBtn) {
        zoomInBtn.addEventListener('click', () => {
            scale = Math.min(scale + 0.1, 3);
            applyTransform();
        });
    }

    const zoomOutBtn = container.querySelector('.mermaid-zoom-out');
    if (zoomOutBtn) {
        zoomOutBtn.addEventListener('click', () => {
            scale = Math.max(scale - 0.1, 0.5);
            applyTransform();
        });
    }

    const resetBtn = container.querySelector('.mermaid-zoom-reset');
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

    const styleId = 'mermaid-zoom-styles';
    if (!document.getElementById(styleId)) {
        const style = document.createElement('style');
        style.id = styleId;
        style.textContent = `
        .mermaid-zoom-container {
          display: flex;
          flex-direction: column;
          width: 100%;
        }
        .mermaid-zoom-controls {
          display: flex;
          justify-content: center;
          margin-bottom: 8px;
          gap: 8px;
        }
        .mermaid-zoom-controls button {
          padding: 4px 8px;
          background-color: var(--global-secondary-color, #374151);
          border: none;
          border-radius: var(--global-border-radius, 4px);
          color: var(--global-text-color, white);
          cursor: pointer;
        }
        .mermaid-zoom-controls button:hover {
          background-color: var(--global-primary-color, #4b5563);
        }
        .mermaid-diagram-container {
          overflow: hidden;
          border: 1px solid var(--global-border-color, #374151);
          border-radius: var(--global-border-radius, 4px);
          padding: 16px;
          height: 400px; /* Or make dynamic based on content */
          position: relative;
          cursor: move;
          background-color: var(--global-background-color, #111827); /* Diagram background */
        }
        .mermaid-zoom-hint {
          font-size: 0.75rem;
          color: var(--global-secondary-color, #6b7280);
          text-align: center;
          margin-top: 4px;
        }
        /* Ensure SVG itself is transparent to show container background */
        .mermaid-diagram-container svg {
          background-color: transparent;
        }
      `;
        document.head.appendChild(style);
    }
};

export const MermaidRenderer: CodeBlockRenderer = {
    type: ['mermaid'],
    initialize: () => {
        configureMermaidTheme(); // Initial theme configuration
    },
    Component: ({ content, className }) => {
        const containerRef = useRef<HTMLDivElement>(null);
        const [error, setError] = useState<string | null>(null);
        const [diagramId] = useState('mermaid-diagram-' + Math.random().toString(36).substring(2, 9));

        // Inject a global style to hide Mermaid's temporary div and prevent layout shifts
        useEffect(() => {
            const styleId = 'mermaid-temp-fix-style';
            if (document.getElementById(styleId)) return;

            const styleElement = document.createElement('style');
            styleElement.id = styleId;
            styleElement.innerHTML = `
            div[id^="dmermaid-diagram-"] {
                position: absolute !important;
                top: -9999px !important;
                left: -9999px !important;
                z-index: -100 !important;
                visibility: hidden !important;
                pointer-events: none !important;
            }
        `;
            document.head.appendChild(styleElement);
        }, []);

        const renderDiagram = useCallback(async () => {
            if (!containerRef.current) return;
            // Clear previous content and errors
            containerRef.current.innerHTML = '';
            setError(null);

            try {
                
                // 1. Validate the Mermaid syntax first. This will throw an error on invalid syntax.
                let parsed = await mermaid.parse(content, { suppressErrors: true });
                if (!parsed) {
                    console.error('Mermaid rendering failed');
                    setError('Mermaid rendering failed');
                    return;
                }

                // 2. If validation succeeds, proceed to render the diagram.
                configureMermaidTheme();
                const { svg } = await mermaid.render(diagramId, content);
                if (containerRef.current) {
                    containerRef.current.innerHTML = wrapSvgWithZoomControls(svg);
                    initializeZoomPan(containerRef.current);
                }
            } catch (err) {
                // 3. Catch errors from either mermaid.parse() or mermaid.render().
                const errorMessage = err instanceof Error ? err.message : String(err);
                console.error('Mermaid rendering failed:', errorMessage);
                setError(errorMessage);
            }
        }, [content, diagramId]);

        useEffect(() => {
            try {
                renderDiagram(); // Initial render
            } catch (err) {
                // 3. Catch errors from either mermaid.parse() or mermaid.render().
                const errorMessage = err instanceof Error ? err.message : String(err);
                console.error('Mermaid rendering failed:', errorMessage);
                setError(errorMessage);
            }
             
            const handleThemeChange = () => {
                renderDiagram(); // Re-render on theme change
            };

            window.addEventListener('themechange', handleThemeChange as EventListener);
            return () => {
                window.removeEventListener('themechange', handleThemeChange as EventListener);
            };
        }, [renderDiagram]); // renderDiagram dependency includes content and diagramId

        // Enhanced JSX for displaying the error or the diagram
        return (
            <div className={`${className || ''}`}>
                {error ? (
                    <div className="p-3 border border-red-500/50 rounded-md bg-red-900/20 text-red-300 text-xs">
                        <p className="font-bold mb-2">Mermaid Syntax Error</p>
                        <pre className="whitespace-pre-wrap font-mono text-sm">
                            {error}
                        </pre>
                    </div>
                ) : (
                    <div ref={containerRef} className="mermaid-container w-full" />
                )}
            </div>
        );
    },
};