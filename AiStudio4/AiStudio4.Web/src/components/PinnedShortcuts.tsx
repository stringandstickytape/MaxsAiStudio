
import { useEffect, useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { useCommandStore } from '@/stores/useCommandStore';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip';
import { cn } from '@/lib/utils';
import { Pin, Command, ChevronDown, Plus, Settings, RefreshCw, GitBranch, Mic } from 'lucide-react';
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
    autoFit?: boolean;
    maxRows?: number;
}

const getIconForCommand = (commandId: string, iconName?: string) => {
    const iconProps = { className: "h-3.5 w-3.5" };
    
    if (iconName) {
        const iconMap: Record<string, JSX.Element> = {
            'Plus': <Plus {...iconProps} />,
            'Settings': <Settings {...iconProps} />,
            'RefreshCw': <RefreshCw {...iconProps} />,
            'GitBranch': <GitBranch {...iconProps} />,
            'Mic': <Mic {...iconProps} />
        };
        return iconMap[iconName] || <Command {...iconProps} />;
    }

    return commandId.includes('new') ? <Plus {...iconProps} /> :
           commandId.includes('settings') ? <Settings {...iconProps} /> :
           commandId.includes('clear') || commandId.includes('reset') ? <RefreshCw {...iconProps} /> :
           commandId.includes('tree') ? <GitBranch {...iconProps} /> :
           commandId.includes('voice') ? <Mic {...iconProps} /> :
           <Command {...iconProps} />;
}

export function PinnedShortcuts({
    orientation = 'horizontal',
    maxShown = 10,
    className,
    autoFit = true,
    maxRows = 3,
}: PinnedShortcutsProps) {
    const {
        pinnedCommands,
        loading,
        error,
        fetchPinnedCommands,
        addPinnedCommand,
        removePinnedCommand,
        reorderPinnedCommands,
        savePinnedCommands,
    } = usePinnedCommandsStore();

    const clientId = localStorage.getItem('clientId');

    const [initialLoadComplete, setInitialLoadComplete] = useState(false);

    const prevPinnedCommandsRef = useRef<string>('');

    const wsConnectedRef = useRef<boolean>(false);

    const containerRef = useRef<HTMLDivElement>(null);
    const buttonRef = useRef<HTMLButtonElement>(null);

    const [visibleCount, setVisibleCount] = useState(maxShown);
    const [rowCount, setRowCount] = useState(1);
    const [itemsPerRow, setItemsPerRow] = useState(maxShown);

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

    const isModified = usePinnedCommandsStore(state => state.isModified);
    
    useEffect(() => {
        if (loading || !initialLoadComplete || !isModified) {
            return;
        }

        const saveTimer = setTimeout(() => {
            console.log('Saving modified pinned commands to server');
            savePinnedCommands();
        }, 1000);

        return () => clearTimeout(saveTimer);
    }, [pinnedCommands, savePinnedCommands, loading, initialLoadComplete, isModified]);

    useEffect(() => {
        if (!autoFit || orientation === 'vertical' || !containerRef.current || !buttonRef.current) {
            return;
        }

        const calculateVisibleButtons = () => {
            const containerWidth = containerRef.current?.clientWidth || 0;
            const buttonWidth = 90 + 4;
            const dropdownWidth = 30;

            const availableWidth = containerWidth - dropdownWidth;
            const maxButtonsPerRow = Math.floor(availableWidth / buttonWidth);
            
            
            const effectiveRows = Math.min(maxRows, Math.ceil(pinnedCommands.length / maxButtonsPerRow));
            const newItemsPerRow = maxButtonsPerRow;
            const newRowCount = Math.min(effectiveRows, Math.ceil(pinnedCommands.length / newItemsPerRow));
            const newVisibleCount = Math.min(newItemsPerRow * newRowCount, pinnedCommands.length);

            if (newItemsPerRow !== itemsPerRow) {
                setItemsPerRow(newItemsPerRow);
            }
            
            if (newRowCount !== rowCount) {
                setRowCount(newRowCount);
            }
            
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
    }, [autoFit, orientation, pinnedCommands.length, visibleCount, itemsPerRow, rowCount, maxRows]);

    const handleDragEnd = (result: DropResult) => {
        if (!result.destination) return;

        const items = Array.from(pinnedCommands);

        const [reorderedItem] = items.splice(result.source.index, 1);
        items.splice(result.destination.index, 0, reorderedItem);

        const reorderedIds = items.map((cmd) => cmd.id);

        reorderPinnedCommands(reorderedIds);

        setUserModified(true);

        
    };

    const handleCommandClick = (commandId: string) => {
        useCommandStore.getState().executeCommand(commandId);
    };

    const handlePinCommand = (commandId: string, isCurrentlyPinned: boolean) => {
        setUserModified(true);

        if (isCurrentlyPinned) {
            removePinnedCommand(commandId);
            
            savePinnedCommands().catch(err => console.error('Error saving pinned commands after removal:', err));
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
                    section: command.section,
                });
            }
        }
    };

    const effectiveVisibleCount = autoFit && orientation === 'horizontal' ? visibleCount : maxShown;

    const visibleCommands = pinnedCommands.slice(0, effectiveVisibleCount);
    const hiddenCommands = pinnedCommands.slice(effectiveVisibleCount);
    const hasMoreCommands = pinnedCommands.length > effectiveVisibleCount;
    
    
    const commandRows: typeof pinnedCommands[] = [];
    if (orientation === 'horizontal' && autoFit && rowCount > 1) {
        for (let i = 0; i < rowCount; i++) {
            const startIdx = i * itemsPerRow;
            const endIdx = Math.min(startIdx + itemsPerRow, visibleCommands.length);
            commandRows.push(visibleCommands.slice(startIdx, endIdx));
        }
    } else {
        commandRows.push(visibleCommands);
    }

    if (pinnedCommands.length === 0) {
        return (
            <div
                className={cn(
                    'flex items-center justify-center',
                    orientation === 'vertical' ? 'flex-col h-full' : 'h-8',
                    className,
                )}
            >
                <div className="text-xs text-gray-500 flex items-center gap-1.5">
                    <Pin className="h-3 w-3" />
                    <span className="hidden sm:inline">Pin commands for quick access</span>
                </div>
            </div>
        );
    }

    if (error) {
        console.error('Error with pinned commands:', error);
    }

    return (
        <TooltipProvider>
            <DragDropContext onDragEnd={handleDragEnd}>
                <div
                    ref={containerRef}
                    className={cn(
                        'flex justify-center gap-1 overflow-x-auto',
                        orientation === 'vertical' ? 'flex-col items-center' : 'flex-col w-full',
                        className,
                    )}
                >
                    {orientation === 'horizontal' && autoFit && rowCount > 1 ? (
                        
                        commandRows.map((rowCommands, rowIndex) => (
                            <div key={`row-${rowIndex}`} className="flex flex-row items-center justify-center gap-1 w-full">
                                <Droppable
                                    droppableId={`pinned-commands-row-${rowIndex}`}
                                    direction="horizontal"
                                    isCombineEnabled={false}
                                    isDropDisabled={false}
                                    ignoreContainerClipping={false}
                                >
                                    {(provided) => (
                                        <div
                                            {...provided.droppableProps}
                                            ref={provided.innerRef}
                                            className="flex items-center gap-1 flex-row"
                                        >
                                            {rowCommands.map((command, index) => {
                                                
                                                const globalIndex = rowIndex * itemsPerRow + index;
                                                return (
                                                    <Draggable key={command.id} draggableId={command.id} index={globalIndex}>
                                                        {(provided, snapshot) => (
                                                            <div
                                                                ref={provided.innerRef}
                                                                {...provided.draggableProps}
                                                                className={cn('flex flex-col items-center group', snapshot.isDragging && 'opacity-70 z-50')}
                                                            >
                                                                <div
                                                                    {...provided.dragHandleProps}
                                                                    className="cursor-grab h-2 w-[90px] text-gray-500 hover:text-gray-300 flex items-center justify-center rounded hover:bg-gray-700 transition-colors opacity-0 group-hover:opacity-100"
                                                                >
                                                                    <div className="w-12 flex items-center justify-center">
                                                                        <div className="h-[3px] w-8 bg-current rounded-full"></div>
                                                                    </div>
                                                                </div>
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
                                                                            <div className="flex-shrink-0">{getIconForCommand(command.id, command.iconName)}</div>
                                                                            <span className="text-xs font-medium flex-1 text-left leading-tight break-words whitespace-normal overflow-hidden line-clamp-2">
                                                                                {command.name}
                                                                            </span>
                                                                        </Button>
                                                                    </TooltipTrigger>
                                                                    <TooltipContent side="bottom">
                                                                        <p>{command.name}</p>
                                                                        <p className="text-small-gray-400">
                                                                            Drag handle above button to reorder · Right-click to unpin
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </div>
                                                        )}
                                                    </Draggable>
                                                );
                                            })}
                                            {provided.placeholder}
                                        </div>
                                    )}
                                </Droppable>
                            </div>
                        ))
                    ) : (
                        
                        <Droppable
                            droppableId="pinned-commands"
                            direction={orientation === 'vertical' ? 'vertical' : 'horizontal'}
                            isCombineEnabled={false}
                            isDropDisabled={false}
                            ignoreContainerClipping={false}
                        >
                            {(provided) => (
                                <div
                                    {...provided.droppableProps}
                                    ref={provided.innerRef}
                                    className={cn('flex items-center justify-center gap-1', orientation === 'vertical' ? 'flex-col' : 'flex-row w-full')}
                                >
                                    {visibleCommands.map((command, index) => (
                                        <Draggable key={command.id} draggableId={command.id} index={index}>
                                            {(provided, snapshot) => (
                                                <div
                                                    ref={provided.innerRef}
                                                    {...provided.draggableProps}
                                                    className={cn('flex flex-col items-center group', snapshot.isDragging && 'opacity-70 z-50')}
                                                >
                                                    <div
                                                        {...provided.dragHandleProps}
                                                        className="cursor-grab h-2 w-[90px] text-gray-500 hover:text-gray-300 flex items-center justify-center rounded hover:bg-gray-700 transition-colors opacity-0 group-hover:opacity-100"
                                                    >
                                                        <div className="w-12 flex items-center justify-center">
                                                            <div className="h-[3px] w-8 bg-current rounded-full"></div>
                                                        </div>
                                                    </div>
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
                                                                <div className="flex-shrink-0">{getIconForCommand(command.id, command.iconName)}</div>
                                                                <span className="text-xs font-medium flex-1 text-left leading-tight break-words whitespace-normal overflow-hidden line-clamp-2">
                                                                    {command.name}
                                                                </span>
                                                            </Button>
                                                        </TooltipTrigger>
                                                        <TooltipContent side={orientation === 'vertical' ? 'right' : 'bottom'}>
                                                            <p>{command.name}</p>
                                                            <p className="text-small-gray-400">
                                                                Drag handle above button to reorder · Right-click to unpin
                                                            </p>
                                                        </TooltipContent>
                                                    </Tooltip>
                                                </div>
                                            )}
                                        </Draggable>
                                    ))}
                                    {provided.placeholder}
                                </div>

                            )}
                        </Droppable>)}

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
                                    {hiddenCommands.map((command) => (
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