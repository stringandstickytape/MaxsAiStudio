// src/components/ModelStatusBar.tsx
import { Info, Zap } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import {
    Tooltip,
    TooltipContent,
    TooltipProvider,
    TooltipTrigger,
} from '@/components/ui/tooltip';
import { useModelStore } from '@/stores/useModelStore';

interface ModelStatusBarProps {
    onPrimaryClick?: () => void;
    onSecondaryClick?: () => void;
    orientation?: 'horizontal' | 'vertical';
}

export function ModelStatusBar({
    onPrimaryClick,
    onSecondaryClick,
    orientation = 'horizontal'
}: ModelStatusBarProps) {
    // Use Zustand store
    const { selectedPrimaryModel, selectedSecondaryModel } = useModelStore();
    
    const isVertical = orientation === 'vertical';

    return (
        <div className={cn(
            "flex gap-2",
            isVertical ? "flex-col" : "items-center"
        )}>
            {/* Primary Model */}
            <TooltipProvider>
                <Tooltip>
                    <TooltipTrigger asChild>
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={onPrimaryClick}
                            className={cn(
                                "bg-gradient-to-r from-blue-800/80 to-blue-700/80 hover:from-blue-800 hover:to-blue-700 text-white border-blue-700/50 flex items-center gap-2",
                                isVertical ? "w-36 justify-between px-3" : ""
                            )}
                        >
                            <Zap className="h-3.5 w-3.5 text-blue-300" />
                            <span className="truncate">
                                {selectedPrimaryModel !== "Select Model" ? selectedPrimaryModel : "Select Model"}
                            </span>
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
                                "bg-gradient-to-r from-purple-800/80 to-purple-700/80 hover:from-purple-800 hover:to-purple-700 text-white border-purple-700/50 flex items-center gap-2",
                                isVertical ? "w-36 justify-between px-3" : ""
                            )}
                        >
                            <Info className="h-3.5 w-3.5 text-purple-300" />
                            <span className="truncate">
                                {selectedSecondaryModel !== "Select Model" ? selectedSecondaryModel : "Select Model"}
                            </span>
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