// AiStudioClient/src/components/InputBar/ToolsButton.tsx
import React, { useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Wrench } from 'lucide-react';
import { useModalStore } from '@/stores/useModalStore';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';

interface ToolsButtonProps {
    activeTools: string[];
    removeActiveTool: (toolId: string) => void;
    disabled: boolean;
}

// Custom comparison function for ToolsButton memoization
const areToolsButtonPropsEqual = (prevProps: ToolsButtonProps, nextProps: ToolsButtonProps) => {
  return (
    prevProps.disabled === nextProps.disabled &&
    prevProps.activeTools.length === nextProps.activeTools.length &&
    prevProps.activeTools.every((tool, index) => tool === nextProps.activeTools[index])
    // Note: We don't compare removeActiveTool function as it should be stable
  );
};

export const ToolsButton = React.memo(({ activeTools, removeActiveTool, disabled }: ToolsButtonProps) => {
    const handleOpenToolLibrary = useCallback(() => {
        useModalStore.getState().openModal('tool', {});
    }, []);
    
    const handleMouseDown = useCallback((e: React.MouseEvent) => {
        if (e.button === 1) {
            e.preventDefault();
            activeTools.forEach(toolId => removeActiveTool(toolId));
        }
    }, [activeTools, removeActiveTool]);
    return (
        <TooltipProvider delayDuration={300}>
            <Tooltip>
                <TooltipTrigger asChild>
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleOpenToolLibrary}
                        onMouseDown={handleMouseDown}
                        className="h-5 px-2 py-0 text-xs rounded-full bg-gray-600/10 border border-gray-700/20 text-gray-300 hover:bg-gray-600/30 hover:text-gray-100 transition-colors relative [&_svg]:shrink [&>*]:shrink min-w-0"
                        disabled={disabled}
                        style={{
                            color: 'var(--global-text-color)',
                            borderColor: 'var(--global-secondary-color, rgba(147, 51, 234, 0.2))',
                            border: '0px solid'
                        }}
                    >
                        <Wrench className="h-3 w-3 mr-1" />
                        <span className="truncate">Tools</span>
                        {activeTools.length > 0 && (
                            <span className="ml-1 inline-flex items-center justify-center px-1 py-0 text-xs font-bold leading-none text-white bg-blue-500 rounded-full min-w-0 shrink overflow-hidden" style={{ minWidth: '0px', minHeight: '0px' }} title="Middle-click to clear all">
                                {activeTools.length}
                            </span>
                        )}
                    </Button>
                </TooltipTrigger>
                <TooltipContent side="top" align="center">
                    Middle-click to clear all active tools
                </TooltipContent>
            </Tooltip>
        </TooltipProvider>
    );
}, areToolsButtonPropsEqual);