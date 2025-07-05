import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ChevronLeft, ChevronRight, X, Clock, Play, Pause } from 'lucide-react';

interface Slide {
  content: string;
  notes: string;
  index: number;
}

interface MarpPresenterViewProps {
  slides: Slide[];
  currentSlide: number;
  onSlideChange: (index: number) => void;
  onExitPresenter: () => void;
}

export const MarpPresenterView: React.FC<MarpPresenterViewProps> = ({
  slides,
  currentSlide,
  onSlideChange,
  onExitPresenter
}) => {
  const [startTime, setStartTime] = useState<Date | null>(null);
  const [elapsedTime, setElapsedTime] = useState(0);
  const [isTimerRunning, setIsTimerRunning] = useState(false);

  // Timer logic
  useEffect(() => {
    let interval: NodeJS.Timeout;
    
    if (isTimerRunning && startTime) {
      interval = setInterval(() => {
        setElapsedTime(Date.now() - startTime.getTime());
      }, 1000);
    }
    
    return () => clearInterval(interval);
  }, [isTimerRunning, startTime]);

  const formatTime = (ms: number): string => {
    const totalSeconds = Math.floor(ms / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  const startTimer = () => {
    if (!startTime) {
      setStartTime(new Date());
    }
    setIsTimerRunning(true);
  };

  const pauseTimer = () => {
    setIsTimerRunning(false);
  };

  const resetTimer = () => {
    setStartTime(new Date());
    setElapsedTime(0);
    setIsTimerRunning(false);
  };

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      switch (e.key) {
        case 'ArrowLeft':
          if (currentSlide > 0) onSlideChange(currentSlide - 1);
          break;
        case 'ArrowRight':
        case ' ':
          if (currentSlide < slides.length - 1) onSlideChange(currentSlide + 1);
          break;
        case 'Home':
          onSlideChange(0);
          break;
        case 'End':
          onSlideChange(slides.length - 1);
          break;
        case 'Escape':
          onExitPresenter();
          break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [currentSlide, slides.length, onSlideChange, onExitPresenter]);

  const currentSlideData = slides[currentSlide];
  const nextSlideData = slides[currentSlide + 1];

  return (
    <div className="marp-presenter-view h-full bg-background flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b">
        <div className="flex items-center gap-4">
          <Badge variant="outline">
            Slide {currentSlide + 1} of {slides.length}
          </Badge>
          
          <div className="flex items-center gap-2">
            <Clock className="h-4 w-4" />
            <span className="font-mono text-sm">{formatTime(elapsedTime)}</span>
            <Button
              variant="ghost"
              size="sm"
              onClick={isTimerRunning ? pauseTimer : startTimer}
            >
              {isTimerRunning ? <Pause className="h-4 w-4" /> : <Play className="h-4 w-4" />}
            </Button>
            <Button variant="ghost" size="sm" onClick={resetTimer}>
              Reset
            </Button>
          </div>
        </div>
        
        <Button variant="ghost" size="sm" onClick={onExitPresenter}>
          <X className="h-4 w-4" />
          Exit Presenter
        </Button>
      </div>

      {/* Main content */}
      <div className="flex-1 flex">
        {/* Current slide */}
        <div className="flex-1 p-4">
          <Card className="h-full">
            <CardHeader>
              <CardTitle className="text-lg">Current Slide</CardTitle>
            </CardHeader>
            <CardContent className="flex-1 flex items-center justify-center">
              <div className="marp-slide-preview max-w-full max-h-full">
                <div 
                  className="marp-slide scale-75 origin-center"
                  dangerouslySetInnerHTML={{ __html: currentSlideData.content }}
                />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Right sidebar */}
        <div className="w-80 p-4 border-l space-y-4">
          {/* Next slide preview */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Next Slide</CardTitle>
            </CardHeader>
            <CardContent>
              {nextSlideData ? (
                <div className="marp-slide-preview-small">
                  <div 
                    className="marp-slide scale-25 origin-top-left w-[400%] h-[400%] pointer-events-none"
                    dangerouslySetInnerHTML={{ __html: nextSlideData.content }}
                  />
                </div>
              ) : (
                <div className="text-muted-foreground text-sm">
                  No more slides
                </div>
              )}
            </CardContent>
          </Card>

          {/* Speaker notes */}
          <Card className="flex-1">
            <CardHeader>
              <CardTitle className="text-base">Speaker Notes</CardTitle>
            </CardHeader>
            <CardContent>
              {currentSlideData.notes ? (
                <div className="text-sm whitespace-pre-wrap">
                  {currentSlideData.notes}
                </div>
              ) : (
                <div className="text-muted-foreground text-sm">
                  No notes for this slide
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Navigation controls */}
      <div className="flex items-center justify-center gap-4 p-4 border-t">
        <Button
          variant="outline"
          onClick={() => onSlideChange(Math.max(0, currentSlide - 1))}
          disabled={currentSlide === 0}
        >
          <ChevronLeft className="h-4 w-4" />
          Previous
        </Button>
        
        <Button
          variant="outline"
          onClick={() => onSlideChange(Math.min(slides.length - 1, currentSlide + 1))}
          disabled={currentSlide === slides.length - 1}
        >
          Next
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
};