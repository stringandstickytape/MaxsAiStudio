// /src/components/html-renderer.tsx
import { CodeBlockRenderer } from '@/components/diagrams/types';
import { useEffect, useRef } from 'react';

export const HtmlRenderer: CodeBlockRenderer = {
  type: ['html', 'htm', 'svg'],
  initialize: () => {
    
  },
  render: async () => {
    
  },
  Component: ({ content, className }) => {
    const iframeRef = useRef(null);

    useEffect(() => {
      if (!iframeRef.current) return;

      
      const styleTag = `
                <style>
                    body {
                        margin: 0;
                        padding: 10px;
                        font-family: Inter, system-ui, Avenir, Helvetica, Arial, sans-serif;
                        color: #e0e0e0;
                        background-color: transparent;
                    }
                    
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

      
      const htmlContent = `
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    ${styleTag}
                    <script>
                        document.addEventListener('DOMContentLoaded', function() {
                            
                            const height = document.body.scrollHeight;
                            window.parent.postMessage({ type: 'resize', height: height }, '*');
                            
                            
                            document.addEventListener('click', function(e) {
                                if (e.target.tagName === 'A' && e.target.getAttribute('href')) {
                                    e.preventDefault();
                                    
                                    window.parent.postMessage({ 
                                        type: 'linkClicked', 
                                        href: e.target.getAttribute('href') 
                                    }, '*');
                                }
                            });

                            
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

      
      iframeRef.current.srcdoc = htmlContent;

      
      const handleMessage = (event) => {
        if (event.data && event.data.type === 'resize') {
          
          iframeRef.current.style.height = `${event.data.height + 20}px`;
        } else if (event.data && event.data.type === 'linkClicked') {
          
          console.log('Link clicked in HTML preview:', event.data.href);
        }
      };

      window.addEventListener('message', handleMessage);
      return () => window.removeEventListener('message', handleMessage);
    }, [content]);

    
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
          height: '100px', 
          backgroundColor: 'transparent',
        }}
        title="HTML Preview"
      />
    );
  },
};

