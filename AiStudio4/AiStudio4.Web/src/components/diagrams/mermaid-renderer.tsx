import mermaid from 'mermaid';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef, useState } from 'react';

export const MermaidRenderer: CodeBlockRenderer = {
  type: ['mermaid'],
  initialize: () => {
    mermaid.initialize({
      startOnLoad: true,
      theme: 'dark',
      securityLevel: 'strict',
      darkMode: true,
      themeVariables: {
        fontFamily: 'Inter, system-ui, Avenir, Helvetica, Arial, sans-serif',
        primaryColor: '#3b82f6',
        primaryTextColor: '#e0e0e0',
        primaryBorderColor: '#374151',
        lineColor: '#4b5563',
        secondaryColor: '#475569',
        tertiaryColor: '#1f2937',
      },
    });
  },
  render: async (content: string, element: HTMLElement) => {
    try {
      const { svg } = await mermaid.render('mermaid-svg-' + Math.random().toString(36).substr(2, 9), content);
      element.innerHTML = svg;
    } catch (error) {
      console.error('Failed to render mermaid diagram:', error);
      element.innerHTML = `<div class="error">Failed to render diagram: ${error.message}</div>`;
    }
  },
  Component: ({ content, className }) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
      if (!containerRef.current) return;

      const renderDiagram = async () => {
        try {
          
          const id = 'mermaid-diagram-' + Math.random().toString(36).substr(2, 9);
          containerRef.current.innerHTML = `<div id="${id}">${content}</div>`;

          
          const { svg } = await mermaid.render(id, content);
          containerRef.current.innerHTML = svg;
          setError(null);
        } catch (error) {
          console.error('Failed to render mermaid diagram:', error);
          setError(error.message);
        }
      };

      renderDiagram();
    }, [content]);

    return (
      <div className={`flex justify-center ${className || ''}`}>
        {error ? (
          <div className="text-red-500 p-2 border border-red-300 rounded">Failed to render diagram: {error}</div>
        ) : (
          <div ref={containerRef} className="mermaid-container w-full" />
        )}
      </div>
    );
  },
};


