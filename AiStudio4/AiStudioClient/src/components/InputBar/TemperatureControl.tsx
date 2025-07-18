﻿// AiStudio4/AiStudioClient/src/components/InputBar/TemperatureControl.tsx
import React, { useCallback } from 'react';
import { Slider } from '@/components/ui/slider';
import { Label } from '@/components/ui/label';
import { useGeneralSettingsStore } from '@/stores/useGeneralSettingsStore';
import { cn } from '@/lib/utils';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

// Custom comparison function for TemperatureControl memoization
const areTemperatureControlPropsEqual = () => {
  // This component has no props, so it only needs to re-render when the temperature/isLoading changes
  // The useGeneralSettingsStore hook will handle that internally
  return true;
};

export const TemperatureControl = React.memo(() => {
  const { temperature, setTemperatureLocally, updateTemperatureOnServer, isLoading } = useGeneralSettingsStore();

  const handleSliderChange = useCallback((value: number[]) => {
    setTemperatureLocally(value[0]);
  }, [setTemperatureLocally]);

  const handleSliderCommit = useCallback((value: number[]) => {
    updateTemperatureOnServer(value[0]);
  }, [updateTemperatureOnServer]);

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
              htmlFor="temperature-slider" 
                          className="text-xs text-gray-400 cursor-pointer shrink-[2] min-w-0 overflow-hidden text-ellipsis whitespace-nowrap"
                          style={{
                              color: 'var(--global-text-color)',
                          }}
            >
              Temp: {temperature.toFixed(1)}
            </Label>
            <Slider
              id="temperature-slider"
              value={[temperature]}
              min={0}
              max={2}
              step={0.1}
              onValueChange={handleSliderChange}
              onValueCommit={handleSliderCommit}
              className={cn("shrink w-[100px] md:w-[120px]", isLoading && "opacity-50 cursor-not-allowed")}
              disabled={isLoading}
                          aria-label="Temperature Slider"
                          style={{
                              color: 'var(--global-text-color)',
                          }}
            />
          </div>
        </TooltipTrigger>
        <TooltipContent side="top" align="center">
          <p>Controls AI creativity/randomness.</p>
          <p>0.0 = deterministic, 2.0 = very random.</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}, areTemperatureControlPropsEqual);