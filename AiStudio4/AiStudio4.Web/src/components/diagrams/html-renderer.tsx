// /src/components/html-renderer.tsx
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef } from 'react';

export const HtmlRenderer: CodeBlockRenderer = {
    type: ['html', 'htm','svg'],
    initialize: () => {
        // No initialization needed
    },
    render: async () => {
        // Rendering will happen in each iframe independently
    },
    Component: ({ content, className }) => {
        const iframeRef = useRef(null);

        useEffect(() => {
            if (!iframeRef.current) return;

            // Set up default CSS for the HTML iframe
            const styleTag = `
                <style>
                    body {
                        margin: 0;
                        padding: 10px;
                        font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
                        color: #e0e0e0;
                        background-color: transparent;
                    }
                    /* Default dark mode styling for common elements */
                    a { color: #3b82f6; }
                    a:hover { color: #60a5fa; }
                    button, input, select, textarea {
                        background-color: #374151;
                        border: 1px solid #4b5563;
                        color: #e0e0e0;
                        border-radius: 4px;
                    }
                    button {
                        padding: 4px 12px;
                        cursor: pointer;
                    }
                    button:hover {
                        background-color: #4b5563;
                    }
                </style>
            `;

            // Create the HTML content for the iframe
            const htmlContent = `
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    ${styleTag}
                    <script>
                        document.addEventListener('DOMContentLoaded', function() {
                            // Adjust iframe height after rendering
                            const height = document.body.scrollHeight;
                            window.parent.postMessage({ type: 'resize', height: height }, '*');
                            
                            // Handle clicks on links to prevent navigation
                            document.addEventListener('click', function(e) {
                                if (e.target.tagName === 'A' && e.target.getAttribute('href')) {
                                    e.preventDefault();
                                    // Can optionally send a message to parent to handle link clicks
                                    window.parent.postMessage({ 
                                        type: 'linkClicked', 
                                        href: e.target.getAttribute('href') 
                                    }, '*');
                                }
                            });

                            // Adjust height on window resize
                            window.addEventListener('resize', function() {
                                const height = document.body.scrollHeight;
                                window.parent.postMessage({ type: 'resize', height: height }, '*');
                            });
                        });
                    </script>
                </head>
                <body>
                    ${content}
                </body>
                </html>
            `;

            // Set the srcdoc of the iframe
            iframeRef.current.srcdoc = htmlContent;

            // Handle messages from the iframe
            const handleMessage = (event) => {
                if (event.data && event.data.type === 'resize') {
                    // Add a small buffer to avoid scrollbars
                    iframeRef.current.style.height = `${event.data.height + 20}px`;
                } else if (event.data && event.data.type === 'linkClicked') {
                    // Handle link clicks if needed
                    console.log('Link clicked in HTML preview:', event.data.href);
                }
            };

            window.addEventListener('message', handleMessage);
            return () => window.removeEventListener('message', handleMessage);
        }, [content]);

        // Helper method to launch the HTML in a new window
        const launchInNewWindow = () => {
            const newWindow = window.open('', '_blank');
            if (newWindow) {
                newWindow.document.write(`
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset="UTF-8">
                        <meta name="viewport" content="width=device-width, initial-scale=1.0">
                        <title>HTML Preview</title>
                        <style>
                            body {
                                font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
                                line-height: 1.5;
                                padding: 20px;
                            }
                        </style>
                    </head>
                    <body>
                        ${content}
                    </body>
                    </html>
                `);
                newWindow.document.close();
            }
        };

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
                title="HTML Preview"
            />
        );
    }
};