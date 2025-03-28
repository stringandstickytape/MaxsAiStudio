// src/components/diagrams/dot-renderer.tsx
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef, useState } from 'react';
import { Graphviz as graphviz } from '@hpcc-js/wasm';

// Helper function to create wrapper for SVG with zoom/pan controls
const wrapSvgWithZoomControls = (svgContent: string): string => {
    return `
    <div class="dot-zoom-container">
      <div class="dot-zoom-controls">
        <button class="dot-zoom-in" title="Zoom In">+</button>
        <button class="dot-zoom-out" title="Zoom Out">-</button>
        <button class="dot-zoom-reset" title="Reset View">Reset</button>
      </div>
      <div class="dot-diagram-container">
        ${svgContent}
      </div>
      <div class="dot-zoom-hint">Use mouse wheel to zoom, drag to pan, or use the controls above</div>
    </div>
  `;
};

// Function to initialize zoom/pan functionality on an element
const initializeZoomPan = (container: HTMLElement) => {
    const diagramContainer = container.querySelector('.dot-diagram-container');
    const svg = diagramContainer?.querySelector('svg');

    if (!diagramContainer || !svg) return;

    // Add necessary styles to SVG
    svg.style.transformOrigin = '0 0';
    svg.style.transition = 'transform 0.1s';

    let scale = 1;
    let translateX = 0;
    let translateY = 0;
    let dragging = false;
    let lastX = 0;
    let lastY = 0;

    // Apply transformation
    const applyTransform = () => {
        svg.style.transform = `translate(${translateX}px, ${translateY}px) scale(${scale})`;
    };

    // Zoom in button
    const zoomInBtn = container.querySelector('.dot-zoom-in');
    if (zoomInBtn) {
        zoomInBtn.addEventListener('click', () => {
            scale = Math.min(scale + 0.1, 3);
            applyTransform();
        });
    }

    // Zoom out button
    const zoomOutBtn = container.querySelector('.dot-zoom-out');
    if (zoomOutBtn) {
        zoomOutBtn.addEventListener('click', () => {
            scale = Math.max(scale - 0.1, 0.5);
            applyTransform();
        });
    }

    // Reset button
    const resetBtn = container.querySelector('.dot-zoom-reset');
    if (resetBtn) {
        resetBtn.addEventListener('click', () => {
            scale = 1;
            translateX = 0;
            translateY = 0;
            applyTransform();
        });
    }

    // Mouse wheel zoom
    diagramContainer.addEventListener('wheel', (e) => {
        e.preventDefault();

        const rect = diagramContainer.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const delta = e.deltaY < 0 ? 0.1 : -0.1;
        const newScale = Math.max(0.5, Math.min(3, scale + delta));

        if (newScale !== scale) {
            const scaleRatio = newScale / scale;
            translateX = translateX - ((mouseX - translateX) * (scaleRatio - 1));
            translateY = translateY - ((mouseY - translateY) * (scaleRatio - 1));
            scale = newScale;
            applyTransform();
        }
    });

    // Mouse drag to pan
    diagramContainer.addEventListener('mousedown', (e) => {
        if (e.button === 0) { // Left button only
            dragging = true;
            lastX = e.clientX;
            lastY = e.clientY;
            svg.style.transition = 'none'; // Disable transition during drag
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
            svg.style.transition = 'transform 0.1s'; // Re-enable transition
        }
    };

    diagramContainer.addEventListener('mouseup', endDrag);
    diagramContainer.addEventListener('mouseleave', endDrag);

    // Add CSS for the container
    const style = document.createElement('style');
    style.textContent = `
    .dot-zoom-container {
      display: flex;
      flex-direction: column;
      width: 100%;
    }
    .dot-zoom-controls {
      display: flex;
      justify-content: center;
      margin-bottom: 8px;
      gap: 8px;
    }
    .dot-zoom-controls button {
      padding: 4px 8px;
      background-color: #374151;
      border: none;
      border-radius: 4px;
      color: white;
      cursor: pointer;
    }
    .dot-zoom-controls button:hover {
      background-color: #4b5563;
    }
    .dot-diagram-container {
      overflow: hidden;
      border: 1px solid #374151;
      border-radius: 4px;
      padding: 16px;
      height: 400px;
      position: relative;
      cursor: move;
    }
    .dot-zoom-hint {
      font-size: 0.75rem;
      color: #6b7280;
      text-align: center;
      margin-top: 4px;
    }
    .dot-diagram-container svg {
      max-width: 100%;
      height: auto;
      background: transparent;
    }
  `;
    document.head.appendChild(style);
};

// Flag to track if graphviz is initialized
let graphvizInitialized = false;

export const DotRenderer: CodeBlockRenderer = {
    type: ['dot', 'graphviz'],
    initialize: async () => {
        // Initialize graphviz only once
        if (!graphvizInitialized) {
            try {
                await graphviz.init();
                graphvizInitialized = true;
            } catch (error) {
                console.error('Failed to initialize graphviz:', error);
            }
        }
    },
    render: async (content: string, element: HTMLElement) => {
        try {
            // Make sure graphviz is initialized
            if (!graphvizInitialized) {
                await graphviz.init();
                graphvizInitialized = true;
            }

            // Render the DOT content to SVG
            const svgString = graphviz.dot(content);

            // Wrap the SVG with zoom controls
            element.innerHTML = wrapSvgWithZoomControls(svgString);

            // Initialize zoom/pan functionality
            initializeZoomPan(element);
        } catch (error) {
            console.error('Failed to render DOT diagram:', error);
            element.innerHTML = `<div class="error text-red-500 p-2 border border-red-300 rounded">Failed to render diagram: ${error.message}</div>`;
        }
    },
    Component: ({ content, className }) => {
        const containerRef = useRef<HTMLDivElement>(null);
        const [error, setError] = useState<string | null>(null);

        useEffect(() => {
            if (!containerRef.current) return;

            const renderDiagram = async () => {
                try {
                    // Make sure graphviz is initialized
                    if (!graphvizInitialized) {
                        await graphviz.init();
                        graphvizInitialized = true;
                    }

                    // Render the DOT content to SVG
                    const svgString = graphviz.dot(content);

                    // Set the HTML with the wrapper for zoom/pan controls
                    if (containerRef.current) {
                        containerRef.current.innerHTML = wrapSvgWithZoomControls(svgString);

                        // Initialize zoom/pan functionality
                        initializeZoomPan(containerRef.current);
                    }

                    setError(null);
                } catch (error) {
                    console.error('Failed to render DOT diagram:', error);
                    setError(error.message);
                }
            };

            renderDiagram();
        }, [content]);

        return (
            <div className={`${className || ''}`}>
                {error ? (
                    <div className="text-red-500 p-2 border border-red-300 rounded">Failed to render diagram: {error}</div>
                ) : (
                    <div ref={containerRef} className="dot-container w-full" />
                )}
            </div>
        );
    },
};