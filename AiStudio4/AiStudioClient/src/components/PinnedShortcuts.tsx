import React, { useEffect, useRef, useState, memo, useCallback, useMemo } from 'react';
import { Pin } from 'lucide-react';
import { cn } from '@/lib/utils';
import { TooltipProvider } from '@/components/ui/tooltip';
import { useCommandStore } from '@/stores/useCommandStore';
import { usePinnedCommandsStore, PinnedCommand } from '@/stores/usePinnedCommandsStore';
import { CategoryPill } from './CategoryPill';
import { ShortcutPopup } from './ShortcutPopup';
import { extractCategories, groupCommandsByCategory, sortCategories } from './categoryUtils';

interface PinnedShortcutsProps {
    orientation?: 'horizontal' | 'vertical';
    maxShown?: number;
    className?: string;
    autoFit?: boolean;
    maxRows?: number;
}

function PinnedShortcutsBase({
    orientation = 'horizontal',
    maxShown = 10,
    className,
    autoFit = true,
    maxRows = 3,
}: PinnedShortcutsProps) {
    // Use selective selectors from the store to prevent unnecessary re-renders
    const pinnedCommands = usePinnedCommandsStore(state => state.pinnedCommands);
    const categoryOrder = usePinnedCommandsStore(state => state.categoryOrder);
    const loading = usePinnedCommandsStore(state => state.loading);
    const error = usePinnedCommandsStore(state => state.error);
    const fetchPinnedCommands = usePinnedCommandsStore(state => state.fetchPinnedCommands);
    const addPinnedCommand = usePinnedCommandsStore(state => state.addPinnedCommand);
    const removePinnedCommand = usePinnedCommandsStore(state => state.removePinnedCommand);
    const reorderCategories = usePinnedCommandsStore(state => state.reorderCategories);
    const reorderCommandsInCategory = usePinnedCommandsStore(state => state.reorderCommandsInCategory);
    const savePinnedCommands = usePinnedCommandsStore(state => state.savePinnedCommands);
    const setIsModified = usePinnedCommandsStore(state => state.setIsModified);

    const clientId = localStorage.getItem('clientId');

    const [initialLoadComplete, setInitialLoadComplete] = useState(false);
    const prevPinnedCommandsRef = useRef<string>('');
    const wsConnectedRef = useRef<boolean>(false);
    const containerRef = useRef<HTMLDivElement>(null);
    const buttonRef = useRef<HTMLButtonElement>(null);

    // Category and popup state
    const [activeCategory, setActiveCategory] = useState<string | null>(null);
    const [popupAnchor, setPopupAnchor] = useState<HTMLElement | null>(null);
    const [draggedCategory, setDraggedCategory] = useState<string | null>(null);
    
    // Track user modifications
    const isModified = usePinnedCommandsStore(state => state.isModified);

    // Initial load of pinned commands
    useEffect(() => {
        if (clientId) {
            fetchPinnedCommands();
        }
    }, [fetchPinnedCommands, clientId]);

    // WebSocket reconnection handler
    useEffect(() => {
        const handleWebSocketConnected = () => {
            if (wsConnectedRef.current && initialLoadComplete) {
                
                fetchPinnedCommands();
            }
            wsConnectedRef.current = true;
        };

        window.addEventListener('ws-connected', handleWebSocketConnected);

        return () => {
            window.removeEventListener('ws-connected', handleWebSocketConnected);
        };
    }, [fetchPinnedCommands, initialLoadComplete]);

    // Track initial load completion
    useEffect(() => {
        if (!loading && pinnedCommands.length > 0) {
            const currentCommandsString = JSON.stringify(pinnedCommands);
            prevPinnedCommandsRef.current = currentCommandsString;

            if (!initialLoadComplete) {
                setInitialLoadComplete(true);
                
            }
        }
    }, [loading, pinnedCommands, initialLoadComplete]);

    // Auto-save changes
    useEffect(() => {
        if (loading || !initialLoadComplete || !isModified) {
            return;
        }

        const saveTimer = setTimeout(() => {
            savePinnedCommands();
        }, 1000);

        return () => clearTimeout(saveTimer);
    }, [pinnedCommands, categoryOrder, savePinnedCommands, loading, initialLoadComplete, isModified]);

    // No longer needed - removed width calculation logic for old button layout

    // Group commands by category and sort
    const commandsByCategory = useMemo(() => {
        return groupCommandsByCategory(pinnedCommands);
    }, [pinnedCommands]);

    const availableCategories = useMemo(() => {
        const categories = extractCategories(pinnedCommands);
        return sortCategories(categories, categoryOrder);
    }, [pinnedCommands, categoryOrder]);

    // Handle category pill click
    const handleCategoryClick = useCallback((categoryId: string, anchorElement: HTMLElement) => {
        if (activeCategory === categoryId) {
            setActiveCategory(null);
            setPopupAnchor(null);
        } else {
            setActiveCategory(categoryId);
            setPopupAnchor(anchorElement);
        }
    }, [activeCategory]);

    // Handle popup close
    const handlePopupClose = useCallback(() => {
        setActiveCategory(null);
        setPopupAnchor(null);
    }, []);

    // Handle category drag and drop
    const handleCategoryDragStart = useCallback((e: React.DragEvent, categoryId: string) => {
        setDraggedCategory(categoryId);
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', categoryId);
    }, []);

    const handleCategoryDragEnd = useCallback(() => {
        setDraggedCategory(null);
    }, []);

    const handleCategoryDrop = useCallback((e: React.DragEvent, targetCategoryId: string) => {
        e.preventDefault();
        
        if (!draggedCategory || draggedCategory === targetCategoryId) {
            return;
        }

        const newOrder = [...availableCategories];
        const draggedIndex = newOrder.indexOf(draggedCategory);
        const targetIndex = newOrder.indexOf(targetCategoryId);
        
        if (draggedIndex !== -1 && targetIndex !== -1) {
            newOrder.splice(draggedIndex, 1);
            newOrder.splice(targetIndex, 0, draggedCategory);
            reorderCategories(newOrder);
        }
    }, [draggedCategory, availableCategories, reorderCategories]);

    // Handle command click - memoized to prevent recreation on each render
    const handleCommandClick = useCallback((commandId: string) => {
        useCommandStore.getState().executeCommand(commandId);
    }, []);

    // Handle command removal
    const handleRemoveCommand = useCallback((commandId: string) => {
        removePinnedCommand(commandId);
    }, [removePinnedCommand]);

    // Handle commands reorder within category
    const handleCommandsReorder = useCallback((categoryId: string, commandIds: string[]) => {
        reorderCommandsInCategory(categoryId, commandIds);
    }, [reorderCommandsInCategory]);

    // Handle pin/unpin command - memoized to prevent recreation on each render
    const handlePinCommand = useCallback((commandId: string, isCurrentlyPinned: boolean) => {
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
                    iconSet: 'lucide',
                    section: command.section,
                });
            }
        }
    }, [removePinnedCommand, savePinnedCommands, addPinnedCommand]);
    
    // Empty state
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

    // Log errors
    if (error) {
        console.error('Error with pinned commands:', error);
    }

    return (
        <TooltipProvider>
            <div
                ref={containerRef}
                className={cn(
                    'PinnedShortcuts flex justify-center gap-2 overflow-x-auto',
                    orientation === 'vertical' ? 'flex-col items-center' : 'flex-row w-full',
                    className,
                )}
                style={{
                    fontFamily: 'var(--global-font-family, inherit)',
                    fontSize: 'var(--global-font-size, inherit)',
                    color: 'var(--global-text-color, #e5e7eb)',
                    ...(window?.theme?.PinnedShortcuts?.style || {})
                }}
            >
                {/* Category Pills */}
                {availableCategories.map((categoryId) => {
                    const categoryCommands = commandsByCategory[categoryId] || [];
                    return (
                        <CategoryPill
                            key={categoryId}
                            categoryId={categoryId}
                            commandCount={categoryCommands.length}
                            commands={categoryCommands}
                            isActive={activeCategory === categoryId}
                            onClick={(e) => handleCategoryClick(categoryId, e.currentTarget)}
                            onDragStart={handleCategoryDragStart}
                            onDragEnd={handleCategoryDragEnd}
                            onDrop={handleCategoryDrop}
                            isDragging={draggedCategory === categoryId}
                        />
                    );
                })}
            </div>

            {/* Shortcut Popup */}
            {activeCategory && (
                <ShortcutPopup
                    isOpen={!!activeCategory}
                    categoryId={activeCategory}
                    commands={commandsByCategory[activeCategory] || []}
                    anchorElement={popupAnchor}
                    onClose={handlePopupClose}
                    onCommandClick={handleCommandClick}
                    onCommandsReorder={handleCommandsReorder}
                    onRemoveCommand={handleRemoveCommand}
                />
            )}
        </TooltipProvider>
    );
}

// Expose themeable properties for ThemeManager
export const themeableProps = {
};

// Memoize the component to prevent unnecessary re-renders
export const PinnedShortcuts = memo(PinnedShortcutsBase, (prevProps, nextProps) => {
    // Compare props to determine if re-render is needed
    return (
        prevProps.orientation === nextProps.orientation &&
        prevProps.maxShown === nextProps.maxShown &&
        prevProps.className === nextProps.className &&
        prevProps.autoFit === nextProps.autoFit &&
        prevProps.maxRows === nextProps.maxRows
    );
});