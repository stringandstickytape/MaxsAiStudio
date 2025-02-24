import mermaid from 'mermaid';
import { DiagramRenderer } from '@/components/diagrams/types';

export const MermaidRenderer: DiagramRenderer = {
    type: 'mermaid',
    initialize: () => {
        mermaid.initialize({
            startOnLoad: true,
            theme: 'dark',
            securityLevel: 'loose',
            darkMode: true,
            themeVariables: {
                fontFamily: 'Inter, system-ui, Avenir, Helvetica, Arial, sans-serif',
                primaryColor: '#3b82f6',
                primaryTextColor: '#e0e0e0',
                primaryBorderColor: '#374151',
                lineColor: '#4b5563',
                secondaryColor: '#475569',
                tertiaryColor: '#1f2937',
            }
        });
    },
    render: async (content: string) => {
        try {
            // Reset any existing Mermaid diagrams
            document.querySelectorAll('.mermaid').forEach((element) => {
                element.innerHTML = element.getAttribute('data-content') || '';
            });

            // Re-render all Mermaid diagrams
            await mermaid.run({
                querySelector: '.mermaid',
            });
        } catch (error) {
            console.error('Error rendering Mermaid diagrams:', error);
        }
    },
    Component: ({ content, className }) => (
        <div
            className={`mermaid ${className || ''}`}
            data-content={content}
        >
            {content}
        </div>
    )
};