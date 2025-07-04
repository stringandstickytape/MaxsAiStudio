import React, { useEffect, useState, useMemo, useRef } from 'react';
import { Marp } from '@marp-team/marp-core';
import { MarpControls } from './marp-controls';
import { MarpStyles } from './marp-styles';
import { MarpThumbnails } from './marp-thumbnails';
import { MarpPresenterView } from './marp-presenter-view';

interface MarpRendererProps {
  markdown: string;
  frontmatter: Record<string, any>;
}

export const MarpRenderer: React.FC<MarpRendererProps> = ({ markdown, frontmatter }) => {
  console.log('MarpRenderer: Component initialized', {
    markdown: markdown.slice(0, 200),
    frontmatter
  });
  
  const [currentSlide, setCurrentSlide] = useState(0);
  const [showThumbnails, setShowThumbnails] = useState(false);
  const [presenterMode, setPresenterMode] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  
  // Initialize Marp instance with configuration
  const marp = useMemo(() => {
    return new Marp({
      html: true,
      emoji: { 
        shortcode: 'twemoji', 
        unicode: 'twemoji' 
      },
      math: 'mathjax',
      script: { source: 'cdn' },
      minifyCSS: true,
      allowLocalFiles: true
    });
  }, []);

  // Parse the markdown and generate HTML/CSS
  const { html, css, slides } = useMemo(() => {
    try {
      console.log('MarpRenderer: Rendering markdown with Marp', {
        markdownLength: markdown.length,
        markdownStart: markdown.slice(0, 100)
      });
      
      const { html, css } = marp.render(markdown);
      
      console.log('MarpRenderer: Marp render complete', {
        htmlLength: html.length,
        cssLength: css.length
      });
      
      // Parse slides from the HTML
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = html;
      const slideElements = tempDiv.querySelectorAll('section');
      const slides = Array.from(slideElements).map((slide, index) => ({
        content: slide.outerHTML,
        notes: extractSpeakerNotes(slide),
        index
      }));
      
      console.log('MarpRenderer: Parsed slides', {
        slideCount: slides.length,
        slideElements: slideElements.length
      });
      
      return { html, css, slides };
    } catch (error) {
      console.error('Error rendering Marp presentation:', error);
      return { html: '', css: '', slides: [] };
    }
  }, [markdown, marp]);

  // Extract speaker notes from slide
  const extractSpeakerNotes = (slideElement: Element): string => {
    // Look for comments in the slide that represent speaker notes
    const comments = slideElement.innerHTML.match(/<!--\s*([\s\S]*?)\s*-->/g);
    if (comments) {
      return comments.map(comment => 
        comment.replace(/<!--\s*/, '').replace(/\s*-->/, '').trim()
      ).join('\n');
    }
    return '';
  };

  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (presenterMode) return; // Let presenter view handle its own navigation
      
      switch (e.key) {
        case 'ArrowLeft':
          if (currentSlide > 0) setCurrentSlide(currentSlide - 1);
          break;
        case 'ArrowRight':
        case ' ':
          if (currentSlide < slides.length - 1) setCurrentSlide(currentSlide + 1);
          break;
        case 'Home':
          setCurrentSlide(0);
          break;
        case 'End':
          setCurrentSlide(slides.length - 1);
          break;
        case 'Escape':
          if (isFullscreen) {
            document.exitFullscreen();
            setIsFullscreen(false);
          }
          break;
        case 'f':
        case 'F':
          if (!isFullscreen) {
            containerRef.current?.requestFullscreen();
            setIsFullscreen(true);
          }
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [currentSlide, slides.length, presenterMode, isFullscreen]);

  // Handle fullscreen changes
  useEffect(() => {
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };

    document.addEventListener('fullscreenchange', handleFullscreenChange);
    return () => document.removeEventListener('fullscreenchange', handleFullscreenChange);
  }, []);

  if (!html || slides.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-muted-foreground">
        <p>No Marp presentation content to display</p>
      </div>
    );
  }

  return (
    <div className="marp-container relative" ref={containerRef}>
      <MarpStyles css={css} scopeId={`marp-${Date.now()}`} />
      
      {presenterMode ? (
        <MarpPresenterView
          slides={slides}
          currentSlide={currentSlide}
          onSlideChange={setCurrentSlide}
          onExitPresenter={() => setPresenterMode(false)}
        />
      ) : (
        <>
          <div className="marp-presentation-wrapper flex">
            {showThumbnails && (
              <MarpThumbnails
                slides={slides}
                currentSlide={currentSlide}
                onSlideClick={setCurrentSlide}
              />
            )}
            
            <div className="marp-slide-container flex-1 relative overflow-hidden">
              <div 
                className="marp-slide"
                dangerouslySetInnerHTML={{ __html: slides[currentSlide].content }}
              />
            </div>
          </div>
          
          <MarpControls
            currentSlide={currentSlide}
            totalSlides={slides.length}
            onPrevious={() => setCurrentSlide(Math.max(0, currentSlide - 1))}
            onNext={() => setCurrentSlide(Math.min(slides.length - 1, currentSlide + 1))}
            onSlideChange={setCurrentSlide}
            showThumbnails={showThumbnails}
            onToggleThumbnails={() => setShowThumbnails(!showThumbnails)}
            isFullscreen={isFullscreen}
            onToggleFullscreen={() => {
              if (isFullscreen) {
                document.exitFullscreen();
              } else {
                containerRef.current?.requestFullscreen();
              }
            }}
            onEnterPresenter={() => setPresenterMode(true)}
            markdown={markdown}
          />
        </>
      )}
    </div>
  );
};