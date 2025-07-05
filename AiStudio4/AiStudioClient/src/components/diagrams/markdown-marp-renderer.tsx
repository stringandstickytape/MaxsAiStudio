import React, { useEffect, useRef, useState } from 'react';
import { CodeBlockRenderer } from './types';
import matter from 'gray-matter';
import { Marp } from '@marp-team/marp-core';

interface MarkdownMarpComponentProps {
  content: string;
  className?: string;
}

const MarkdownMarpComponent: React.FC<MarkdownMarpComponentProps> = ({ content, className }) => {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [isReady, setIsReady] = useState(false);
  const [currentSlide, setCurrentSlide] = useState(0);
  const [totalSlides, setTotalSlides] = useState(0);

  // Parse the markdown content to check for Marp frontmatter
  try {
    const parsed = matter(content);
    const isMarp = parsed.data?.marp === true;
    
    if (isMarp) {
      // Generate the complete HTML document for the iframe
      useEffect(() => {
        if (!iframeRef.current) return;

        const marp = new Marp({
          html: true,
          emoji: { 
            shortcode: false, 
            unicode: false 
          },
          math: 'mathjax',
          script: { source: 'cdn' },
          minifyCSS: false, // Don't minify CSS to preserve custom styles
          allowLocalFiles: true,
          inlineSVG: true
        });
        

        try {
          // Pass the FULL content including frontmatter to Marp, not just parsed.content
          const { html, css } = marp.render(content);
          
          // Debug: Log the generated CSS to see if custom styles are included
          console.log('Generated Marp CSS:', css);
          console.log('Generated HTML:', html);
          
          
          // Count slides
          const slideCount = (html.match(/<section/g) || []).length;
          setTotalSlides(slideCount);
          
          // Create a complete HTML document with slide navigation
          const iframeDocument = `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Marp Presentation</title>
  <style>
    body {
      margin: 0;
      padding: 0;
      background: transparent;
      overflow: hidden;
      width: 100vw;
      height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    
    /* Simple container - let content size naturally */
    .marp-wrapper {
      width: 100%;
      height: 100%;
      position: relative;
      overflow: auto;
    }
    
    /* Content wrapper */
    .marp-content {
      width: 100%;
      height: 100%;
      margin: 0;
    }
    
    /* Hide all SVG slides by default */
    .marpit svg {
      display: none;
    }
    
    /* Show only active SVG slide */
    .marpit svg.active {
      display: block;
    }
    
    /* Marp styles come last to take precedence */
    ${css}
  </style>
</head>
<body>
  <div class="marp-wrapper">
    <div class="marp-content">
      ${html}
    </div>
  </div>
  <script>
    let currentSlide = 0;
    const slides = document.querySelectorAll('.marpit svg');
    
    function showSlide(index) {
      slides.forEach((slide, i) => {
        slide.classList.toggle('active', i === index);
      });
      currentSlide = index;
      // Notify parent of slide change
      window.parent.postMessage({ type: 'slideChanged', slide: index }, '*');
    }
    
    // Show first slide initially
    if (slides.length > 0) {
      showSlide(0);
    }
    
    // Listen for navigation commands from parent
    window.addEventListener('message', (event) => {
      if (event.data.type === 'navigate') {
        const newSlide = event.data.slide;
        if (newSlide >= 0 && newSlide < slides.length) {
          showSlide(newSlide);
        }
      }
    });
    
    // Notify parent that iframe is ready
    window.parent.postMessage({ type: 'ready', totalSlides: slides.length }, '*');
  </script>
</body>
</html>`;

          const iframe = iframeRef.current;
          
          // Write the document to the iframe
          const doc = iframe.contentDocument || iframe.contentWindow?.document;
          if (doc) {
            doc.open();
            doc.write(iframeDocument);
            doc.close();
          }
        } catch (error) {
          console.error('Error rendering Marp in iframe:', error);
        }
      }, [parsed.content]);

      // Listen for messages from iframe
      useEffect(() => {
        const handleMessage = (event: MessageEvent) => {
          if (event.data.type === 'ready') {
            setTotalSlides(event.data.totalSlides);
            setIsReady(true);
          } else if (event.data.type === 'slideChanged') {
            setCurrentSlide(event.data.slide);
          }
        };

        window.addEventListener('message', handleMessage);
        return () => window.removeEventListener('message', handleMessage);
      }, []);

      const navigateToSlide = (slideIndex: number) => {
        if (iframeRef.current) {
          iframeRef.current.contentWindow?.postMessage({
            type: 'navigate',
            slide: slideIndex
          }, '*');
        }
      };

      const goToPrevious = () => {
        if (currentSlide > 0) {
          navigateToSlide(currentSlide - 1);
        }
      };

      const goToNext = () => {
        if (currentSlide < totalSlides - 1) {
          navigateToSlide(currentSlide + 1);
        }
      };


      return (
        <div className={`marp-iframe-container ${className || ''}`}>
          <iframe
            ref={iframeRef}
            style={{
              width: '100%',
              height: '400px',
              border: 'none',
              borderRadius: '8px',
              background: 'transparent'
            }}
            sandbox="allow-scripts allow-same-origin"
            title="Marp Presentation"
          />
          
          {/* Navigation Controls */}
          {(!isReady || totalSlides > 1) && (
            <div className="flex items-center justify-between p-2 bg-background border-t">
              <div className="flex items-center gap-2">
                <button
                  onClick={goToPrevious}
                  disabled={!isReady || currentSlide === 0}
                  className="px-3 py-1 text-sm border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-muted"
                >
                  ← Previous
                </button>
                
                <span className="text-sm text-muted-foreground">
                  {isReady ? `${currentSlide + 1} / ${totalSlides}` : 'Loading...'}
                </span>
                
                <button
                  onClick={goToNext}
                  disabled={!isReady || currentSlide === totalSlides - 1}
                  className="px-3 py-1 text-sm border rounded disabled:opacity-50 disabled:cursor-not-allowed hover:bg-muted"
                >
                  Next →
                </button>
              </div>
            </div>
          )}
          
          {!isReady && (
            <div className="absolute inset-0 flex items-center justify-center bg-muted/50 rounded-lg">
              <span className="text-sm text-muted-foreground">Loading presentation...</span>
            </div>
          )}
        </div>
      );
    }
  } catch (error) {
    console.error('Error parsing markdown for Marp:', error);
  }
  
  // Fallback: render as plain text if not valid Marp
  return (
    <pre className={`text-sm ${className || ''}`}>
      <code>{content}</code>
    </pre>
  );
};

export const MarkdownMarpRenderer: CodeBlockRenderer = {
  type: ['markdown'],
  Component: MarkdownMarpComponent,
  initialize: () => {
    console.log('Markdown Marp renderer initialized');
  }
};