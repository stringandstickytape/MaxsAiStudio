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
        shortcode: false, 
        unicode: false 
      },
      math: 'mathjax',
      script: { source: 'cdn' },
      minifyCSS: true,
      allowLocalFiles: true
    });
  }, []);

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

  // Parse the markdown and generate HTML/CSS
  const { html, css, slides } = useMemo(() => {
    try {
      const { html, css } = marp.render(markdown);
      
      // Parse slides from the HTML
      const tempDiv = document.createElement('div');
      tempDiv.innerHTML = html;
      const slideElements = tempDiv.querySelectorAll('section');
      const slides = Array.from(slideElements).map((slide, index) => ({
        content: slide.outerHTML,
        notes: extractSpeakerNotes(slide),
        index
      }));
      
      return { html, css, slides };
    } catch (error) {
      console.error('Error rendering Marp presentation:', error);
      return { html: '', css: '', slides: [] };
    }
  }, [markdown, marp]);

  // Handle keyboard navigation - only when focused or hovered
  const [isFocused, setIsFocused] = useState(false);
  
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Only handle keyboard events if the Marp container is focused/hovered or in fullscreen mode
      if (!isFocused && !isFullscreen) return;
      if (presenterMode) return; // Let presenter view handle its own navigation
      
      switch (e.key) {
        case 'ArrowLeft':
          if (currentSlide > 0) setCurrentSlide(currentSlide - 1);
          e.preventDefault();
          break;
        case 'ArrowRight':
        case ' ':
          if (currentSlide < slides.length - 1) setCurrentSlide(currentSlide + 1);
          e.preventDefault();
          break;
        case 'Home':
          setCurrentSlide(0);
          e.preventDefault();
          break;
        case 'End':
          setCurrentSlide(slides.length - 1);
          e.preventDefault();
          break;
        case 'Escape':
          if (isFullscreen) {
            document.exitFullscreen();
            setIsFullscreen(false);
            e.preventDefault();
          }
          break;
        case 'f':
        case 'F':
          if (!isFullscreen) {
            containerRef.current?.requestFullscreen();
            setIsFullscreen(true);
            e.preventDefault();
          }
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [currentSlide, slides.length, presenterMode, isFullscreen, isFocused]);

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
    <div 
      className={`marp-container relative transition-all ${isFocused ? 'ring-2 ring-primary/20' : ''}`}
      ref={containerRef}
      tabIndex={0}
      onMouseEnter={() => setIsFocused(true)}
      onMouseLeave={() => setIsFocused(false)}
      onFocus={() => setIsFocused(true)}
      onBlur={() => setIsFocused(false)}
    >
      <MarpStyles css={css} scopeId={`marp-${Date.now()}`} />
      
      {/* Focus hint */}
      {!isFocused && !isFullscreen && (
        <div className="absolute top-2 right-2 bg-background/80 backdrop-blur-sm px-2 py-1 rounded text-xs text-muted-foreground opacity-70 pointer-events-none">
          Hover to interact
        </div>
      )}
      
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