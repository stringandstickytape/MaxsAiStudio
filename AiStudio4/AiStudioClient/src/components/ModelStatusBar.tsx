
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
    <div className="flex flex-col gap-1">
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              onClick={onPrimaryClick}
              className="h-5 px-2 py-0 text-xs rounded-full bg-blue-600/10 hover:bg-blue-600/30 hover:text-blue-100 transition-colors"
              style={{
                color: 'var(--global-text-color)',
                borderColor: 'var(--global-primary-color, rgba(37, 99, 235, 0.2))',
                border: '1px solid'
              }}
            >
              <span className="truncate max-w-[160px]">{selectedPrimaryModel !== 'Select Model' ? selectedPrimaryModel : 'Primary Model'}</span>
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
              className="h-5 px-2 py-0 text-xs rounded-full bg-purple-600/10 hover:bg-purple-600/30 hover:text-purple-100 transition-colors"
              style={{
                color: 'var(--global-text-color)',
                borderColor: 'var(--global-secondary-color, rgba(147, 51, 234, 0.2))',
                border: '1px solid'
              }}
            >
              <span className="truncate max-w-[160px]">{selectedSecondaryModel !== 'Select Model' ? selectedSecondaryModel : 'Secondary Model'}</span>
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
