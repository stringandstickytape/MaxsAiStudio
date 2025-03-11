// src/components/PinnedShortcuts.tsx
import { useEffect, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { useCommandStore } from '@/stores/useCommandStore';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { Pin, Command, ChevronDown, Plus, Settings, RefreshCw, GitBranch, Mic, GripVertical } from 'lucide-react';
import { usePinnedCommandsStore } from '@/stores/usePinnedCommandsStore';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuGroup,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { DragDropContext, Droppable, Draggable, DropResult } from 'react-beautiful-dnd';

interface PinnedShortcutsProps {
    orientation?: 'horizontal' | 'vertical';
    maxShown?: number;
    className?: string;
    /**
     * If true, automatically adjusts the number of visible buttons based on container width
     * (only applies to horizontal orientation)
     */
    autoFit?: boolean;
}

const getIconForCommand = (commandId: string, iconName?: string) => {
    if (iconName) {
        switch (iconName) {
            case 'Plus': return <Plus className="h-3.5 w-3.5" />;
            case 'Settings': return <Settings className="h-3.5 w-3.5" />;
            case 'RefreshCw': return <RefreshCw className="h-3.5 w-3.5" />;
            case 'GitBranch': return <GitBranch className="h-3.5 w-3.5" />;
            case 'Mic': return <Mic className="h-3.5 w-3.5" />;
            default: return <Command className="h-3.5 w-3.5" />;
        }
    }

    if (commandId.includes('new')) return <Plus className="h-3.5 w-3.5" />;
    if (commandId.includes('settings')) return <Settings className="h-3.5 w-3.5" />;
    if (commandId.includes('clear') || commandId.includes('reset')) return <RefreshCw className="h-3.5 w-3.5" />;
    if (commandId.includes('tree')) return <GitBranch className="h-3.5 w-3.5" />;
    if (commandId.includes('voice')) return <Mic className="h-3.5 w-3.5" />;

    return <Command className="h-3.5 w-3.5" />;
};

export function PinnedShortcuts({
    orientation = 'horizontal',
    maxShown = 10,
    className,
    autoFit = true
}: PinnedShortcutsProps) {
    const {
        pinnedCommands,
        loading,
        error,
        fetchPinnedCommands,
        addPinnedCommand,
        removePinnedCommand,
        reorderPinnedCommands,
        savePinnedCommands
    } = usePinnedCommandsStore();

    const clientId = localStorage.getItem('clientId');

    const [initialLoadComplete, setInitialLoadComplete] = useState(false);

    const prevPinnedCommandsRef = useRef<string>('');

    const wsConnectedRef = useRef<boolean>(false);

    const containerRef = useRef<HTMLDivElement>(null);
    const buttonRef = useRef<HTMLButtonElement>(null);

    const [visibleCount, setVisibleCount] = useState(maxShown);

    useEffect(() => {
        if (clientId) {
            fetchPinnedCommands();
        }
    }, [fetchPinnedCommands, clientId]);

    useEffect(() => {
        const handleWebSocketConnected = () => {
            if (wsConnectedRef.current && initialLoadComplete) {
                console.log('WebSocket reconnected, refreshing pinned commands');
                fetchPinnedCommands();
            }
            wsConnectedRef.current = true;
        };

        window.addEventListener('ws-connected', handleWebSocketConnected);

        return () => {
            window.removeEventListener('ws-connected', handleWebSocketConnected);
        };
    }, [fetchPinnedCommands, initialLoadComplete]);

    useEffect(() => {
        if (!loading && pinnedCommands.length > 0) {
            const currentCommandsString = JSON.stringify(pinnedCommands);

            prevPinnedCommandsRef.current = currentCommandsString;

            if (!initialLoadComplete) {
                setInitialLoadComplete(true);
                console.log('Initial pinned commands load complete');
            }
        }
    }, [loading, pinnedCommands, initialLoadComplete]);

    const [userModified, setUserModified] = useState(false);

    useEffect(() => {
        if (loading || !initialLoadComplete || !userModified) {
            return;
        }

        setUserModified(false);

        const saveTimer = setTimeout(() => {
            console.log('Saving user-modified pinned commands');
            savePinnedCommands();
        }, 1000);

        return () => clearTimeout(saveTimer);
    }, [pinnedCommands, savePinnedCommands, loading, initialLoadComplete, userModified]);

    useEffect(() => {
        if (!autoFit || orientation === 'vertical' || !containerRef.current || !buttonRef.current) {
            return;
        }

        const calculateVisibleButtons = () => {
            const containerWidth = containerRef.current?.clientWidth || 0;
            const buttonWidth = 90 + 4 + 30;
            const dropdownWidth = 30;

            const availableWidth = containerWidth - dropdownWidth;
            const maxFittingButtons = Math.floor(availableWidth / buttonWidth);

            const newVisibleCount = Math.min(
                Math.max(1, maxFittingButtons),
                pinnedCommands.length
            );

            if (newVisibleCount !== visibleCount) {
                setVisibleCount(newVisibleCount);
            }
        };

        calculateVisibleButtons();

        const resizeObserver = new ResizeObserver(calculateVisibleButtons);
        resizeObserver.observe(containerRef.current);

        return () => {
            if (containerRef.current) {
                resizeObserver.unobserve(containerRef.current);
            }
            resizeObserver.disconnect();
        };
    }, [autoFit, orientation, pinnedCommands.length, visibleCount]);

    const handleDragEnd = (result: DropResult) => {
        if (!result.destination) return;

        const items = Array.from(pinnedCommands);

        const [reorderedItem] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, reorderedItem);

        const reorderedIds = items.map(cmd => cmd.id);

        reorderPinnedCommands(reorderedIds);

        setUserModified(true);

        savePinnedCommands();
    };

    const handleCommandClick = (commandId: string) => {
        useCommandStore.getState().executeCommand(commandId);
    };

    const handlePinCommand = (commandId: string, isCurrentlyPinned: boolean) => {
        setUserModified(true);

        if (isCurrentlyPinned) {
            removePinnedCommand(commandId);
        } else {
            const command = useCommandStore.getState().getCommandById(commandId);
            if (command) {
                let iconName = undefined;
                if (command.icon && typeof command.icon === 'object') {
                    const iconType = command.icon.type?.name || command.icon.type?.displayName;
                    if (iconType) {
                        iconName = iconType;
                    }
                }

                addPinnedCommand({
                    id: command.id,
                    name: command.name,
                    iconName,
                    section: command.section
                });
            }
        }
    };

    const effectiveVisibleCount = (autoFit && orientation === 'horizontal') ? visibleCount : maxShown;

    const visibleCommands = pinnedCommands.slice(0, effectiveVisibleCount);
    const hiddenCommands = pinnedCommands.slice(effectiveVisibleCount);
    const hasMoreCommands = pinnedCommands.length > effectiveVisibleCount;

    if (pinnedCommands.length === 0) {
        return (
            <div className={cn(
                "flex items-center justify-center",
                orientation === 'vertical' ? "flex-col h-full" : "h-8",
                className
            )}>
                <div className="text-xs text-gray-500 flex items-center gap-1.5">
                    <Pin className="h-3 w-3" />
                    <span className="hidden sm:inline">Pin commands for quick access</span>
                </div>
            </div>
        );
    }

    if (error) {
        console.error("Error with pinned commands:", error);
    }

    return (
        <TooltipProvider>
            <DragDropContext onDragEnd={handleDragEnd}>
                <div
                    ref={containerRef}
                    className={cn(
                        "flex items-center justify-center gap-1 overflow-x-auto py-1 px-2",
                        orientation === 'vertical' ? "flex-col" : "flex-row w-full",
                        className
                    )}
                >
                    <Droppable
                        droppableId="pinned-commands"
                        direction={orientation === 'vertical' ? 'vertical' : 'horizontal'}
                        isDropDisabled={false}
                    >
                        {(provided) => (
                            <div
                                {...provided.droppableProps}
                                ref={provided.innerRef}
                                className={cn(
                                    "flex items-center gap-1",
                                    orientation === 'vertical' ? "flex-col" : "flex-row"
                                )}
                            >
                                {visibleCommands.map((command, index) => (
                                    <Draggable key={command.id} draggableId={command.id} index={index}>
                                        {(provided, snapshot) => (
                                            <div
                                                ref={provided.innerRef}
                                                {...provided.draggableProps}
                                                className={cn(
                                                    "flex flex-col items-center",
                                                    snapshot.isDragging && "opacity-70 z-50"
                                                )}
                                            >
                                                <Tooltip key={command.id} delayDuration={300}>
                                                    <TooltipTrigger asChild>
                                                        <Button
                                                            ref={command.id === visibleCommands[0]?.id ? buttonRef : null}
                                                            variant="ghost"
                                                            onClick={() => handleCommandClick(command.id)}
                                                            onContextMenu={(e) => {
                                                                e.preventDefault();
                                                                handlePinCommand(command.id, true);
                                                            }}
                                                            className="h-auto min-h-[40px] max-h-[50px] w-[90px] px-1 py-1 rounded-md bg-gray-800/60 hover:bg-gray-700 border border-gray-700/50 text-gray-300 hover:text-gray-100 flex flex-row items-center justify-start gap-1 relative"
                                                        >
                                                            <div className="flex-shrink-0">
                                                                {getIconForCommand(command.id, command.iconName)}
                                                            </div>
                                                            <span className="text-xs font-medium flex-1 text-left leading-tight break-words whitespace-normal overflow-hidden line-clamp-2">
                                                                {command.name}
                                                            </span>
                                                        </Button>
                                                    </TooltipTrigger>
                                                    <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                                                        <p>{command.name}</p>
                                                        <p className="text-xs text-gray-400">Drag handle below button to reorder · Right-click to unpin</p>
                                                    </TooltipContent>
                                                </Tooltip>
                                                <div
                                                    {...provided.dragHandleProps}
                                                    className="cursor-grab h-2 w-[90px] text-gray-500 hover:text-gray-300 flex items-center justify-center rounded hover:bg-gray-700 transition-colors"
                                                >
                                                    <div className="w-12 flex items-center justify-center">
                                                        <div className="h-[3px] w-8 bg-current rounded-full"></div>
                                                    </div>
                                                </div>
                                            </div>
                                        )}
                                    </Draggable>
                                ))}
                                {provided.placeholder}
                            </div>
                        )}
                    </Droppable>

                    {hasMoreCommands && (
                        <DropdownMenu>
                            <Tooltip>
                                <TooltipTrigger asChild>
                                    <DropdownMenuTrigger asChild>
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="h-6 w-6 p-0 rounded-md bg-gray-800/60 hover:bg-gray-700 border border-gray-700/50 text-gray-300 hover:text-gray-100"
                                        >
                                            <ChevronDown className="h-3 w-3" />
                                        </Button>
                                    </DropdownMenuTrigger>
                                </TooltipTrigger>
                                <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                                    <p>More commands ({pinnedCommands.length - effectiveVisibleCount})</p>
                                </TooltipContent>
                            </Tooltip>
                            <DropdownMenuContent align="end" className="w-48 max-h-[50vh] overflow-y-auto">
                                <DropdownMenuGroup>
                                    {hiddenCommands.map(command => (
                                        <DropdownMenuItem
                                            key={command.id}
                                            onClick={() => handleCommandClick(command.id)}
                                            onContextMenu={(e) => {
                                                e.preventDefault();
                                                handlePinCommand(command.id, true);
                                            }}
                                            className="flex items-center gap-1 text-xs whitespace-normal"
                                        >
                                            <span className="flex-shrink-0">{getIconForCommand(command.id, command.iconName)}</span>
                                            <span>{command.name}</span>
                                        </DropdownMenuItem>
                                    ))}
                                </DropdownMenuGroup>
                            </DropdownMenuContent>
                        </DropdownMenu>
                    )}
                </div>
            </DragDropContext>
        </TooltipProvider>
    );
}