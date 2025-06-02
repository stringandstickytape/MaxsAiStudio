// AiStudio4/AiStudioClient/src/components/InputBar/TopPControl.tsx
import React, { useCallback } from 'react';
import { Slider } from '@/components/ui/slider';
import { Label } from '@/components/ui/label';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';
import { cn } from '@/lib/utils';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

export function TopPControl() {
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
          <div className="flex items-center gap-2"
            style={{
              color: 'var(--global-text-color)',
            }}
          >
            <Label 
              htmlFor="top-p-slider" 
              className="text-xs text-gray-400 whitespace-nowrap cursor-pointer"
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
              className={cn("w-[100px] md:w-[120px]", isLoading && "opacity-50 cursor-not-allowed")}
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
}