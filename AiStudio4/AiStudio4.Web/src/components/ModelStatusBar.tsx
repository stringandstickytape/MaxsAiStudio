// src/components/ModelStatusBar.tsx
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { useModelManagement } from '@/hooks/useResourceManagement';

interface ModelStatusBarProps {
  onPrimaryClick?: () => void;
  onSecondaryClick?: () => void;
}

export function ModelStatusBar({ onPrimaryClick, onSecondaryClick }: ModelStatusBarProps) {
  const { selectedPrimaryModel, selectedSecondaryModel } = useModelManagement();

  return (
    <div className="flex items-center gap-2">
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              onClick={onPrimaryClick}
              className="h-5 px-2 py-0 text-xs rounded-full bg-blue-600/10 border border-blue-700/20 text-blue-200 hover:bg-blue-600/30 hover:text-blue-100 transition-colors"
            >
              <span className="truncate max-w-[130px]">{selectedPrimaryModel !== 'Select Model' ? selectedPrimaryModel : 'Primary Model'}</span>
            </Button>
          </TooltipTrigger>
          <TooltipContent>
            <p>Primary model for chat responses</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>

      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              onClick={onSecondaryClick}
              className="h-5 px-2 py-0 text-xs rounded-full bg-purple-600/10 border border-purple-700/20 text-purple-200 hover:bg-purple-600/30 hover:text-purple-100 transition-colors"
            >
              <span className="truncate max-w-[130px]">{selectedSecondaryModel !== 'Select Model' ? selectedSecondaryModel : 'Secondary Model'}</span>
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