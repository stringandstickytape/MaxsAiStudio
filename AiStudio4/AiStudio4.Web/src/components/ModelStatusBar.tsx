// src/components/ModelStatusBar.tsx
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useModelManagement } from '@/hooks/useResourceManagement';

interface ModelStatusBarProps {
  onPrimaryClick?: () => void;
  onSecondaryClick?: () => void;
  orientation?: 'horizontal' | 'vertical';
}

export function ModelStatusBar({ onPrimaryClick, onSecondaryClick, orientation = 'horizontal' }: ModelStatusBarProps) {
  // Use model management hook
  const { selectedPrimaryModel, selectedSecondaryModel } = useModelManagement();

  const isVertical = orientation === 'vertical';

  // Helper function to break text at natural points (spaces, hyphens, etc.)
  const findBreakPoint = (text: string) => {
    if (!text) return { firstLine: '', secondLine: '' };

    // Look for natural break points
    const midPoint = Math.ceil(text.length / 2);

    // Search for a space, hyphen, or other separator near the midpoint
    let breakIndex = -1;

    // First try to find a space close to the middle
    for (let i = 0; i < 10; i++) {
      if (midPoint - i > 0 && /\s/.test(text[midPoint - i])) {
        breakIndex = midPoint - i;
        break;
      }
      if (midPoint + i < text.length && /\s/.test(text[midPoint + i])) {
        breakIndex = midPoint + i;
        break;
      }
    }

    // If no space found, look for other punctuation
    if (breakIndex === -1) {
      for (let i = 0; i < 10; i++) {
        if (midPoint - i > 0 && /[-_.,:]/.test(text[midPoint - i])) {
          breakIndex = midPoint - i + 1; // Include the punctuation in the first line
          break;
        }
        if (midPoint + i < text.length && /[-_.,:]/.test(text[midPoint + i])) {
          breakIndex = midPoint + i + 1; // Include the punctuation in the first line
          break;
        }
      }
    }

    // If still no good break point, just break at the midpoint
    if (breakIndex === -1) {
      breakIndex = midPoint;
    }

    return {
      firstLine: text.substring(0, breakIndex),
      secondLine: text.substring(breakIndex).trim(),
    };
  };

  return (
    <div className={cn('flex gap-2', isVertical ? 'flex-col' : 'items-center')}>
      {/* Primary Model */}
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="outline"
              size="sm"
              onClick={onPrimaryClick}
              className={cn(
                'p-0 border-none text-white flex items-center gap-2 bg-transparent text-gray-300 hover:text-blue-400 hover:bg-gray-700 animate-hover',
                'justify-between',
              )}
            >
              <div className="grid grid-rows-2 w-full h-[40px] overflow-hidden text-left">
                {selectedPrimaryModel !== 'Select Model' ? (
                  selectedPrimaryModel.length > 15 ? (
                    <>
                      {/* Find a natural break point near the middle */}
                      <span className=" text-xs self-end">{findBreakPoint(selectedPrimaryModel).firstLine}</span>
                      <span className=" text-xs self-start">{findBreakPoint(selectedPrimaryModel).secondLine}</span>
                    </>
                  ) : (
                    <span className="truncate self-center row-span-2">{selectedPrimaryModel}</span>
                  )
                ) : (
                  <span className="truncate self-center row-span-2">Select Model</span>
                )}
              </div>
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            <p>Primary model for chat responses</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>

      {/* Secondary Model */}
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="outline"
              size="sm"
              onClick={onSecondaryClick}
              className={cn(
                'p-0 border-none text-white flex items-center gap-2 bg-transparent text-gray-300 hover:text-blue-400 hover:bg-gray-700 animate-hover',
                'justify-between',
              )}
            >
              <div className="grid grid-rows-2 w-full h-[40px] overflow-hidden text-left">
                {selectedSecondaryModel !== 'Select Model' ? (
                  selectedSecondaryModel.length > 15 ? (
                    <>
                      {/* Find a natural break point near the middle */}
                      <span className=" text-xs self-end">{findBreakPoint(selectedSecondaryModel).firstLine}</span>
                      <span className=" text-xs self-start">{findBreakPoint(selectedSecondaryModel).secondLine}</span>
                    </>
                  ) : (
                    <span className="truncate self-center row-span-2">{selectedSecondaryModel}</span>
                  )
                ) : (
                  <span className="truncate self-center row-span-2">Select Model</span>
                )}
              </div>
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            <p>Secondary model for summaries & short tasks</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </div>
  );
}
