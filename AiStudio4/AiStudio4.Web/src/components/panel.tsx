// src/components/panel.tsx
import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { X, Pin, PinOff } from 'lucide-react';
import { PanelPosition } from '@/types/ui';
import { usePanelStore } from '@/stores/usePanelStore';

interface PanelProps {
    id: string;
    children: ReactNode;
    title: string;
    position: PanelPosition;
    size?: string;
    minWidth?: string;
    maxWidth?: string;
    minHeight?: string;
    maxHeight?: string;
    width?: string;
    height?: string;
    zIndex?: number;
    isOpen?: boolean;
    isPinned?: boolean;
    onClose?: () => void;
    className?: string;
    headerClassName?: string;
    bodyClassName?: string;
    showCloseButton?: boolean;
    showPinButton?: boolean;
}

export function Panel({
    id,
    children,
    title,
    position = 'right',
    size = '80',
    minWidth = '300px',
    maxWidth = '480px',
    minHeight = '200px',
    maxHeight = '100vh',
    width,
    height,
    zIndex = 30,
    isOpen,
    isPinned,
    onClose,
    className,
    headerClassName,
    bodyClassName,
    showCloseButton = true,
    showPinButton = true
}: PanelProps) {
    // Get the panel store functions for toggling
    const { togglePanel, togglePinned } = usePanelStore();

    // Get the panel state from the store
    const panelState = usePanelStore(state => state.panels[id]);

    // Use the prop values if provided, otherwise use values from the store
    const isVisible = isOpen !== undefined ? isOpen : (panelState?.isOpen || false);
    const isPinnedState = isPinned !== undefined ? isPinned : (panelState?.isPinned || false);

    if (!isVisible) return null;

    // Position-based styles
    const positionStyles = {
        left: 'fixed top-0 left-0 bottom-0 border-r',
        right: 'fixed top-0 right-0 bottom-0 border-l',
        top: 'fixed top-0 left-0 right-0 border-b',
        bottom: 'fixed bottom-0 left-0 right-0 border-t'
    }[position];

    // Size-based styles (width for left/right, height for top/bottom)
    const sizeStyles = {
        left: `w-${size}`,
        right: `w-${size}`,
        top: `h-${size}`,
        bottom: `h-${size}`
    }[position];

    const handleClose = () => {
        if (onClose) {
            onClose();
        } else {
            togglePanel(id);
        }
    };

    const handleTogglePin = () => {
        togglePinned(id);
    };

    // Prepare style object based on position and constraints
    const panelStyle: React.CSSProperties = {};

    if (position === 'left' || position === 'right') {
        if (width) {
            panelStyle.width = width;
        } else {
            panelStyle.minWidth = minWidth;
            panelStyle.maxWidth = maxWidth;
        }
    } else {
        if (height) {
            panelStyle.height = height;
        } else {
            panelStyle.minHeight = minHeight;
            panelStyle.maxHeight = maxHeight;
        }
    }

    return (
        <div
            className={cn(
                positionStyles,
                (position === 'left' || position === 'right') && !width ? sizeStyles : '',
                (position === 'top' || position === 'bottom') && !height ? sizeStyles : '',
                'bg-gray-900 border-gray-700/50 shadow-xl transition-all duration-300',
                `z-${zIndex}`,
                className
            )}
            style={panelStyle}
        >
            <div className={cn(
                'flex justify-between p-3 border-b border-gray-700 bg-[#1f2937]',
                headerClassName
            )}>
                <h2 className="text-gray-100 text-lg font-semibold flex items-center">{title}</h2>
                <div className="flex space-x-2">
                    {showPinButton && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={handleTogglePin}
                            className="text-gray-400 hover:text-gray-100"
                        >
                            {isPinnedState ? (
                                <PinOff className="h-4 w-4" />
                            ) : (
                                <Pin className="h-4 w-4" />
                            )}
                        </Button>
                    )}
                    {showCloseButton && !isPinnedState && (
                        <Button
                            variant="ghost"
                            size="icon"
                            onClick={handleClose}
                            className="text-gray-400 hover:text-gray-100"
                        >
                            <X className="h-4 w-4" />
                        </Button>
                    )}
                </div>
            </div>
            <div className={cn('h-[calc(100%-3.5rem)] overflow-auto', bodyClassName)}>
                {children}
            </div>
        </div>
    );
}