// src/components/ui/panel.tsx
import React, { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { X, Pin, PinOff } from 'lucide-react';
import { PanelPosition } from '@/types/ui';
import { usePanelManager } from '@/hooks/usePanelManager';

interface PanelProps {
  id: string;
  children: ReactNode;
  title: string;
  position: PanelPosition;
  size?: string;
  zIndex?: number;
  defaultOpen?: boolean;
  defaultPinned?: boolean;
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
  zIndex = 30,
  defaultOpen = false,
  defaultPinned = false,
  onClose,
  className,
  headerClassName,
  bodyClassName,
  showCloseButton = true,
  showPinButton = true
}: PanelProps) {
  const {
    isOpen,
    isPinned,
    toggle,
    close,
    togglePin
  } = usePanelManager({
    id,
    position,
    size,
    zIndex,
    title,
    defaultOpen,
    defaultPinned
  });

  if (!isOpen) return null;

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
    close();
    if (onClose) onClose();
  };

  return (
    <div
      className={cn(
        positionStyles,
        sizeStyles,
        'bg-gray-900 border-gray-700/50 shadow-xl transition-all duration-300',
        `z-${zIndex}`,
        className
      )}
    >
      <div className={cn(
        'flex justify-between p-3 border-b border-gray-700 bg-[#1f2937]',
        headerClassName
      )}>
        <div className="flex space-x-2">
          {showPinButton && (
            <Button
              variant="ghost"
              size="icon"
              onClick={togglePin}
              className="text-gray-400 hover:text-gray-100"
            >
              {isPinned ? (
                <PinOff className="h-4 w-4" />
              ) : (
                <Pin className="h-4 w-4" />
              )}
            </Button>
          )}
          {showCloseButton && !isPinned && (
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
        <h2 className="text-gray-100 text-lg font-semibold flex items-center">{title}</h2>
      </div>
      <div className={cn('h-[calc(100%-3.5rem)] overflow-auto', bodyClassName)}>
        {children}
      </div>
    </div>
  );
}
