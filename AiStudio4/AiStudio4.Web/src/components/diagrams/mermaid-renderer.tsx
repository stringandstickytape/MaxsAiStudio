import mermaid from 'mermaid';
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef } from 'react';

export const MermaidRenderer: CodeBlockRenderer = {
    type: ['mermaid'],
    initialize: () => {
        // No need to initialize globally since each diagram will be in its own iframe
    },
    render: async () => {
        // Rendering will happen in each iframe independently
    },
    Component: ({ content, className }) => {
        const iframeRef = useRef(null);

        useEffect(() => {
            if (!iframeRef.current) return;

            // Create the HTML content for the iframe
            const htmlContent = `
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {
                            margin: 0;
                            padding: 0;
                            font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
                            background-color: transparent;
                        }
                        .mermaid {
                            display: flex;
                            justify-content: center;
                        }
                    </style>
                    <script src="https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js"></script>
                    <script>
                        document.addEventListener('DOMContentLoaded', function() {
                            mermaid.initialize({
                                startOnLoad: true,
                                theme: 'dark',
                                securityLevel: 'strict', // Use strict here for better security
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
                            
                            // Render the diagram
                            const element = document.getElementById('diagram');
                            element.innerHTML = \`${content.replace(/`/g, '\\\`').replace(/\$/g, '\\$')}\`;
                            
                            // Adjust iframe height after rendering
                            mermaid.run().then(() => {
                                // Send message to parent with the height
                                const height = document.body.scrollHeight;
                                window.parent.postMessage({ type: 'resize', height: height }, '*');
                            });
                        });
                    </script>
                </head>
                <body>
                    <div class="mermaid" id="diagram"></div>
                </body>
                </html>
            `;

            // Set the srcdoc of the iframe
            iframeRef.current.srcdoc = htmlContent;

            // Handle messages from the iframe
            const handleMessage = (event) => {
                if (event.data && event.data.type === 'resize') {
                    iframeRef.current.style.height = `${event.data.height}px`;
                }
            };

            window.addEventListener('message', handleMessage);
            return () => window.removeEventListener('message', handleMessage);
        }, [content]);

        return (
            <iframe
                ref={iframeRef}
                sandbox="allow-scripts"
                className={className || ''}
                style={{
                    width: '100%',
                    border: 'none',
                    overflow: 'hidden',
                    height: '100px', // Default height, will be adjusted
                    backgroundColor: 'transparent'
                }}
                title="Mermaid Diagram"
            />
        );
    }
};