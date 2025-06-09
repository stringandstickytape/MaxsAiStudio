// AiStudio4/AiStudioClient/src/components/InputBar/TopPControl.tsx
import React, { useCallback } from 'react';
import { Slider } from '@/components/ui/slider';
import { Label } from '@/components/ui/label';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';
import { cn } from '@/lib/utils';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

// Custom comparison function for TopPControl memoization
const areTopPControlPropsEqual = () => {
  // This component has no props, so it only needs to re-render when the topP/isLoading changes
  // The useGeneralSettingsStore hook will handle that internally
  return true;
};

export const TopPControl = React.memo(() => {
  const { topP, setTopPLocally, updateTopPOnServer, isLoading } = useGeneralSettingsStore();

  const handleSliderChange = useCallback((value: number[]) => {
    setTopPLocally(value[0]);
  }, [setTopPLocally]);

  const handleSliderCommit = useCallback((value: number[]) => {
    updateTopPOnServer(value[0]);
  }, [updateTopPOnServer]);

  return (
    <TooltipProvider delayDuration={300}>
      <Tooltip>
        <TooltipTrigger asChild>
          <div className="flex items-center gap-2 min-w-0"
            style={{
              color: 'var(--global-text-color)',
            }}
          >
            <Label 
              htmlFor="top-p-slider" 
              className="text-xs text-gray-400 cursor-pointer shrink-[2] min-w-0 overflow-hidden text-ellipsis whitespace-nowrap"
              style={{
                color: 'var(--global-text-color)',
              }}
            >
              Top P: {topP.toFixed(2)}
            </Label>
            <Slider
              id="top-p-slider"
              value={[topP]}
              min={0.0}
              max={1.0}
              step={0.01}
              onValueChange={handleSliderChange}
              onValueCommit={handleSliderCommit}
              className={cn("shrink w-[100px] md:w-[120px]", isLoading && "opacity-50 cursor-not-allowed")}
              disabled={isLoading}
              aria-label="Top P Slider"
              style={{
                color: 'var(--global-text-color)',
              }}
            />
          </div>
        </TooltipTrigger>
        <TooltipContent side="top" align="center">
          <p>Controls nucleus sampling.</p>
          <p>Higher values (e.g., 0.95) = more diversity.</p>
          <p>Lower values (e.g., 0.1) = more focused output.</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}, areTopPControlPropsEqual);