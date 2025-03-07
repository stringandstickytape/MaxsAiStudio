import { ReactNode, useEffect, useRef } from 'react';
import { usePanelStore } from '@/stores/usePanelStore';
import { PanelPosition, PanelState } from '@/types/ui';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { X, Pin } from 'lucide-react';

interface PanelProps {
    id: string;
    children: ReactNode;
    position: PanelPosition;
    size?: string;
    minWidth?: string;
    maxWidth?: string;
    width?: string;
    minHeight?: string;
    maxHeight?: string;
    height?: string;
    zIndex?: number;
    title?: string;
    isOpen?: boolean;
    isPinned?: boolean;
    className?: string;
    onClose?: () => void;
    onTogglePinned?: () => void;
}

export const Panel = ({
    id,
    children,
    position,
    size = '300px',
    minWidth,
    maxWidth,
    width,
    minHeight,
    maxHeight,
    height,
    zIndex = 40,
    title,
    isOpen,
    isPinned,
    className,
    onClose,
    onTogglePinned
}: PanelProps) => {
    const panelStore = usePanelStore();
    const { registerPanel, togglePanel, togglePinned, getPanelState } = panelStore;

    // Register the panel with the store if not already registered
    useEffect(() => {
        // Create a panel state
        const panelState: PanelState = {
            id,
            isOpen: isOpen || false,
            isPinned: isPinned || false,
            position,
            size,
            zIndex,
            title: title || id
        };

        // Register the panel
        registerPanel(panelState);
    }, [id, isOpen, isPinned, position, size, zIndex, title, registerPanel]);

    // Get the panel state from the store
    const panelState = getPanelState(id);

    // If no panel state, don't render
    if (!panelState) return null;

    // Handle panel actions using the store if custom handlers are not provided
    const handleClose = onClose || (() => togglePanel(id));
    const handleTogglePinned = onTogglePinned || (() => togglePinned(id));

    // If not open and not pinned, don't render
    if (!panelState.isOpen && !panelState.isPinned) return null;

    // Determine panel position styles
    let positionStyles = {};
    switch (position) {
        case 'left':
            positionStyles = {
                left: 0,
                top: 0,
                bottom: 0,
                width: width || size,
                minWidth,
                maxWidth,
            };
            break;
        case 'right':
            positionStyles = {
                right: 0,
                top: 0,
                bottom: 0,
                width: width || size,
                minWidth,
                maxWidth,
            };
            break;
        case 'top':
            positionStyles = {
                top: 0,
                left: 0,
                right: 0,
                height: height || size,
                minHeight,
                maxHeight,
            };
            break;
        case 'bottom':
            positionStyles = {
                bottom: 0,
                left: 0,
                right: 0,
                height: height || size,
                minHeight,
                maxHeight,
            };
            break;
    }

    return (
        <div
            className={cn(
                "fixed z-[var(--z-index)] bg-gray-900 border border-gray-700 flex flex-col",
                panelState.isPinned ? "shadow-lg" : "shadow-xl",
                className
            )}
            style={{
                ...positionStyles,
                '--z-index': panelState.zIndex,
            } as React.CSSProperties}
        >
            {/* Panel header */}
            <div className="flex justify-between items-center p-3 border-b border-gray-700 bg-gray-800">
                <h3 className="font-medium text-gray-100 flex-1 truncate">{panelState.title}</h3>
                <div className="flex gap-1">
                    <Button
                        variant="ghost"
                        size="icon"
                        className={cn(
                            "h-8 w-8 text-gray-400 hover:text-gray-100",
                            panelState.isPinned && "text-blue-400 hover:text-blue-300"
                        )}
                        onClick={handleTogglePinned}
                    >
                        <Pin className={cn("h-4 w-4", panelState.isPinned && "fill-blue-400")} />
                    </Button>
                    {!panelState.isPinned && (
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-gray-400 hover:text-gray-100"
                            onClick={handleClose}
                        >
                            <X className="h-4 w-4" />
                        </Button>
                    )}
                </div>
            </div>

            {/* Panel content */}
            <div className="flex-1 overflow-auto">
                {children}
            </div>
        </div>
    );
};