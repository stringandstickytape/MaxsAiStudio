// src/components/PinnedShortcuts.tsx
import React from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { RootState } from '@/store/store';
import { removePinnedCommand } from '@/store/pinnedCommandsSlice';
import { Button } from '@/components/ui/button';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import {
    X, Command, Settings, GitBranch, Plus, RefreshCw, MessageSquare, Wrench,
    ExternalLink, Mic, Star, PenTool, Save, Edit, Pencil
} from 'lucide-react';
import { commandRegistry } from '@/commands/commandRegistry';

interface PinnedShortcutsProps {
    className?: string;
}

export const PinnedShortcuts: React.FC<PinnedShortcutsProps> = ({ className }) => {
    const dispatch = useDispatch();
    const { pinnedCommands } = useSelector((state: RootState) => state.pinnedCommands);

    const executeCommand = (commandId: string) => {
        commandRegistry.executeCommand(commandId);
    };

    const unpinCommand = (e: React.MouseEvent, commandId: string) => {
        e.stopPropagation();
        dispatch(removePinnedCommand(commandId));
    };

    // Map icon names to actual icon components
    const getIconComponent = (iconName?: string) => {
        if (!iconName) return null;

        const iconMap: Record<string, React.ReactNode> = {
            Command: <Command className="h-4 w-4" />,
            Settings: <Settings className="h-4 w-4" />,
            GitBranch: <GitBranch className="h-4 w-4" />,
            Plus: <Plus className="h-4 w-4" />,
            RefreshCw: <RefreshCw className="h-4 w-4" />,
            MessageSquare: <MessageSquare className="h-4 w-4" />,
            Wrench: <Wrench className="h-4 w-4" />,
            ExternalLink: <ExternalLink className="h-4 w-4" />,
            Mic: <Mic className="h-4 w-4" />,
            Star: <Star className="h-4 w-4" />,
            Tool: <PenTool className="h-4 w-4" />,
            PenTool: <PenTool className="h-4 w-4" />,
            Save: <Save className="h-4 w-4" />,
            Edit: <Edit className="h-4 w-4" />,
            Pencil: <Pencil className="h-4 w-4" />
        };

        return iconMap[iconName] || null;
    };

    if (pinnedCommands.length === 0) {
        return (
            <div className={`flex items-center text-gray-400 text-xs ${className}`}>
                <TooltipProvider>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <div className="bg-gray-800/60 hover:bg-gray-700 border border-gray-700/50 text-gray-400 py-1 px-2 rounded text-xs flex items-center">
                                <Command className="h-3 w-3 mr-1" />
                                Pin commands for quick access
                            </div>
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Use the pin icon in the command palette to add shortcuts here</p>
                        </TooltipContent>
                    </Tooltip>
                </TooltipProvider>
            </div>
        );
    }

    return (
        <div className={`flex items-center gap-2 ${className}`}>
            {pinnedCommands.map((command) => (
                <TooltipProvider key={command.id}>
                    <Tooltip>
                        <TooltipTrigger asChild>
                            <Button
                                onClick={() => executeCommand(command.id)}
                                variant="outline"
                                size="sm"
                                className="relative group bg-gray-800/60 hover:bg-gray-700 border-gray-700/50 text-gray-300"
                            >
                                {getIconComponent(command.iconName)}
                                <span className="ml-1">{command.name}</span>
                                <div
                                    onClick={(e) => unpinCommand(e, command.id)}
                                    className="absolute -top-1 -right-1 h-4 w-4 rounded-full bg-gray-700 text-gray-400 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
                                >
                                    <X className="h-3 w-3" />
                                </div>
                            </Button>
                        </TooltipTrigger>
                        <TooltipContent side="bottom">
                            <p>Execute: {command.name}</p>
                            <p className="text-xs text-gray-400">Click the X to unpin</p>
                        </TooltipContent>
                    </Tooltip>
                </TooltipProvider>
            ))}
        </div>
    );
};