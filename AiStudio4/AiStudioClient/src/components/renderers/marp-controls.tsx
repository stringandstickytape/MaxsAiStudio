import React from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  ChevronLeft,
  ChevronRight,
  Maximize,
  Minimize,
  Download,
  Eye,
  Grid3X3,
  Presentation
} from 'lucide-react';

interface MarpControlsProps {
  currentSlide: number;
  totalSlides: number;
  onPrevious: () => void;
  onNext: () => void;
  onSlideChange: (slide: number) => void;
  showThumbnails: boolean;
  onToggleThumbnails: () => void;
  isFullscreen: boolean;
  onToggleFullscreen: () => void;
  onEnterPresenter: () => void;
  markdown: string;
}

export const MarpControls: React.FC<MarpControlsProps> = ({
  currentSlide,
  totalSlides,
  onPrevious,
  onNext,
  onSlideChange,
  showThumbnails,
  onToggleThumbnails,
  isFullscreen,
  onToggleFullscreen,
  onEnterPresenter,
  markdown
}) => {
  const handleExportPdf = async () => {
    // This would integrate with the Marp CLI or a backend service
    // For now, we'll show a placeholder
    console.log('Export to PDF not yet implemented');
    // TODO: Implement PDF export
  };

  const handleExportHtml = () => {
    // Create a complete HTML bundle
    const htmlBundle = `
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>Marp Presentation</title>
  <script src="https://unpkg.com/@marp-team/marp-core@latest/dist/marp.min.js"></script>
  <style>
    body { margin: 0; overflow: hidden; }
    .marp { width: 100vw; height: 100vh; }
  </style>
</head>
<body>
  <div class="marp"></div>
  <script>
    const markdown = ${JSON.stringify(markdown)};
    const marp = new window.Marp();
    const { html, css } = marp.render(markdown);
    document.querySelector('.marp').innerHTML = html;
    const style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);
  </script>
</body>
</html>`;
    
    const blob = new Blob([htmlBundle], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'presentation.html';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="marp-controls flex items-center justify-between p-4 bg-background border-t">
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="icon"
          onClick={onPrevious}
          disabled={currentSlide === 0}
          title="Previous slide (Left arrow)"
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        
        <div className="flex items-center gap-2">
          <Input
            type="number"
            value={currentSlide + 1}
            onChange={(e) => {
              const slide = parseInt(e.target.value) - 1;
              if (slide >= 0 && slide < totalSlides) {
                onSlideChange(slide);
              }
            }}
            className="w-16 text-center"
            min={1}
            max={totalSlides}
          />
          <span className="text-sm text-muted-foreground">/ {totalSlides}</span>
        </div>
        
        <Button
          variant="outline"
          size="icon"
          onClick={onNext}
          disabled={currentSlide === totalSlides - 1}
          title="Next slide (Right arrow or Space)"
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
      
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="icon"
          onClick={onToggleThumbnails}
          title={showThumbnails ? "Hide thumbnails" : "Show thumbnails"}
        >
          <Grid3X3 className="h-4 w-4" />
        </Button>
        
        <Button
          variant="outline"
          size="icon"
          onClick={onEnterPresenter}
          title="Presenter mode"
        >
          <Presentation className="h-4 w-4" />
        </Button>
        
        <Button
          variant="outline"
          size="icon"
          onClick={onToggleFullscreen}
          title={isFullscreen ? "Exit fullscreen (Esc)" : "Enter fullscreen (F)"}
        >
          {isFullscreen ? (
            <Minimize className="h-4 w-4" />
          ) : (
            <Maximize className="h-4 w-4" />
          )}
        </Button>
        
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="icon" title="Export presentation">
              <Download className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <DropdownMenuItem onClick={handleExportPdf}>
              Export as PDF
            </DropdownMenuItem>
            <DropdownMenuItem onClick={handleExportHtml}>
              Export as HTML
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
};