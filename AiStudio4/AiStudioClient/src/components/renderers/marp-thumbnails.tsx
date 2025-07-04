import React, { useEffect, useRef } from 'react';
import { ScrollArea } from '@/components/ui/scroll-area';
import { cn } from '@/lib/utils';

interface Slide {
  content: string;
  notes: string;
  index: number;
}

interface MarpThumbnailsProps {
  slides: Slide[];
  currentSlide: number;
  onSlideClick: (index: number) => void;
}

export const MarpThumbnails: React.FC<MarpThumbnailsProps> = ({
  slides,
  currentSlide,
  onSlideClick
}) => {
  const thumbnailRefs = useRef<(HTMLDivElement | null)[]>([]);
  const scrollAreaRef = useRef<HTMLDivElement>(null);

  // Scroll to current slide thumbnail when it changes
  useEffect(() => {
    const currentThumbnail = thumbnailRefs.current[currentSlide];
    if (currentThumbnail && scrollAreaRef.current) {
      currentThumbnail.scrollIntoView({
        behavior: 'smooth',
        block: 'center'
      });
    }
  }, [currentSlide]);

  return (
    <div className="marp-thumbnails w-48 border-r bg-muted/50">
      <div className="p-2 border-b">
        <h3 className="text-sm font-medium">Slides</h3>
      </div>
      <ScrollArea className="h-[calc(100%-3rem)]" ref={scrollAreaRef}>
        <div className="p-2 space-y-2">
          {slides.map((slide, index) => (
            <div
              key={index}
              ref={el => thumbnailRefs.current[index] = el}
              className={cn(
                "marp-thumbnail relative cursor-pointer rounded-md overflow-hidden transition-all",
                "hover:ring-2 hover:ring-primary",
                currentSlide === index && "ring-2 ring-primary"
              )}
              onClick={() => onSlideClick(index)}
            >
              <div className="aspect-video bg-background border">
                <div className="p-1 text-xs text-muted-foreground">
                  Slide {index + 1}
                </div>
                <div 
                  className="marp-thumbnail-content scale-[0.15] origin-top-left w-[666%] h-[666%] pointer-events-none"
                  dangerouslySetInnerHTML={{ __html: slide.content }}
                />
              </div>
              <div className={cn(
                "absolute inset-0 bg-primary/20 opacity-0 transition-opacity",
                currentSlide === index && "opacity-100"
              )} />
            </div>
          ))}
        </div>
      </ScrollArea>
    </div>
  );
};