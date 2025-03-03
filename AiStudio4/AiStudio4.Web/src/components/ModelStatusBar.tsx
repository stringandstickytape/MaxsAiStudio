// src/components/ModelStatusBar.tsx
import React from 'react';
import { cn } from '@/lib/utils';

interface ModelStatusBarProps {
    primaryModel: string;
    secondaryModel: string;
    className?: string;
    onPrimaryClick?: () => void;
    onSecondaryClick?: () => void;
}

export function ModelStatusBar({
    primaryModel,
    secondaryModel,
    className,
    onPrimaryClick,
    onSecondaryClick
}: ModelStatusBarProps) {
    // Handle default/empty model names
    const primaryDisplay = primaryModel && primaryModel !== "Select Model"
        ? primaryModel
        : "No model selected";

    const secondaryDisplay = secondaryModel && secondaryModel !== "Select Model"
        ? secondaryModel
        : "No model selected";

    return (
        <div className={cn(
            "w-full px-2 py-1 bg-gray-800/50 border-t border-gray-700/30 text-xs",
            className
        )}>
            <div className="flex items-center space-x-2">
                <span className="text-gray-400">Models:</span>

                <button
                    onClick={onPrimaryClick}
                    className="flex items-center px-2 py-0.5 bg-gray-700/70 hover:bg-gray-700 rounded-full border border-gray-700/50 transition-colors"
                >
                    <span className="w-2 h-2 bg-emerald-500 rounded-full mr-1.5"></span>
                    <span className="text-gray-100 text-xs font-medium truncate max-w-[120px]">
                        {primaryDisplay}
                    </span>
                </button>

                <button
                    onClick={onSecondaryClick}
                    className="flex items-center px-2 py-0.5 bg-gray-700/70 hover:bg-gray-700 rounded-full border border-gray-700/50 transition-colors"
                >
                    <span className="w-2 h-2 bg-blue-500 rounded-full mr-1.5"></span>
                    <span className="text-gray-100 text-xs font-medium truncate max-w-[120px]">
                        {secondaryDisplay}
                    </span>
                </button>
            </div>
        </div>
    );
}