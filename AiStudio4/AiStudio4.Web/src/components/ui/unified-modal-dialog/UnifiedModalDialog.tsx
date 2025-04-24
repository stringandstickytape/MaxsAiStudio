// AiStudio4.Web/src/components/ui/unified-modal-dialog/UnifiedModalDialog.tsx

import React, { useEffect, useMemo, useRef } from 'react'; // Added useRef
import * as DialogPrimitive from '@radix-ui/react-dialog';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { UnifiedModalDialogProps, UnifiedModalContextProps } from './types';
import { modalVariants, modalSizeClasses, modalHeightClasses, modalPositionClasses } from './variants';
import { UnifiedModalProvider } from './UnifiedModalContext';

export const UnifiedModalDialog: React.FC<UnifiedModalDialogProps> = ({
  open,
  onOpenChange,
  id,
  variant = 'default',
  size: sizeProp,
  height: heightProp,
  position: positionProp,
  fullScreen = false,
  preventClose = false,
  closeOnClickOutside = true,
  showCloseButton: showCloseButtonProp,
  className,
  contentClassName, // Note: This applies to the scrollable area, not the main dialog box
  overlayClassName,
  children,
  animation: animationProp,
  animationDuration,
  ariaLabel,
  ariaDescribedBy,
  onClose,
  afterClose,
  beforeOpen,
  afterOpen,
}) => {
  const variantConfig = modalVariants[variant];

  const size = sizeProp ?? variantConfig.size;
  const height = heightProp ?? variantConfig.height;
  const position = positionProp ?? variantConfig.position;
  const showCloseButton = showCloseButtonProp ?? variantConfig.showCloseButton;
  const animation = animationProp ?? variantConfig.animation;

  const sizeClass = size === 'custom' ? '' : modalSizeClasses[size];
  const heightClass = height === 'custom' ? '' : modalHeightClasses[height];
  const positionClass = modalPositionClasses[position];

  const handleOpenChange = (newOpenState: boolean) => {
    if (!newOpenState) {
      onClose?.();
    }
    onOpenChange(newOpenState);
  };

  const handleInteractOutside = (event: Event) => {
    if (preventClose || !closeOnClickOutside) {
      event.preventDefault();
    }
  };

  const handleEscapeKeyDown = (event: KeyboardEvent) => {
    if (preventClose) {
      event.preventDefault();
    }
  };

  useEffect(() => {
    if (open) {
      beforeOpen?.();
      // Use setTimeout to ensure 'afterOpen' fires after mount/animation
      const timer = setTimeout(() => afterOpen?.(), animationDuration ?? 150);
      return () => clearTimeout(timer);
    } else {
      // onClose is called in handleOpenChange
      const timer = setTimeout(() => afterClose?.(), animationDuration ?? 150);
      return () => clearTimeout(timer);
    }
  }, [open, beforeOpen, afterOpen, afterClose, animationDuration]);

  const contextValue: UnifiedModalContextProps = useMemo(() => ({
    variant,
    size,
    height,
    showCloseButton,
    onClose: () => handleOpenChange(false),
  }), [variant, size, height, showCloseButton, handleOpenChange]);

  // Animation classes based on Radix state
  const animationClasses = {
    fade: 'data-[state=open]:animate-fade-in data-[state=closed]:animate-fade-out',
    zoom: 'data-[state=open]:animate-zoom-in data-[state=closed]:animate-zoom-out',
    slide: 'data-[state=open]:animate-slide-in data-[state=closed]:animate-slide-out',
    none: '',
  }[animation];

  // Radix Dialog handles basic focus management (initial focus, return focus).
  // FocusTrap enhances this by ensuring focus stays *within* the modal.
  return (
    <DialogPrimitive.Root open={open} onOpenChange={handleOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay
          className={cn(
            'fixed inset-0 z-50 bg-black/80',
            'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
            overlayClassName
          )}
          style={{ animationDuration: `${animationDuration ?? 150}ms` }}
        />
        {/* Use DialogPrimitive's built-in focus management instead of FocusTrap */}
        <div className={cn('fixed inset-0 z-50 flex overflow-auto', positionClass)}>
          <DialogPrimitive.Content
            id={id}
            onInteractOutside={handleInteractOutside}
            onEscapeKeyDown={handleEscapeKeyDown}
            aria-label={ariaLabel}
            aria-describedby={ariaDescribedBy ?? (id ? `${id}-description` : undefined)} // Auto-generate describedby if id exists
            aria-modal="true" // Explicitly mark as modal
            className={cn(
              'relative z-50 flex flex-col border bg-background text-foreground shadow-lg',
              'm-4', // Add some margin to prevent touching edges unless fullScreen
              fullScreen ? 'w-full h-full m-0 border-0 rounded-none' :
              cn(
                'rounded-lg',
                size === 'custom' ? '' : sizeClass,
                height === 'custom' ? '' : heightClass
              ),
              animationClasses,
              className // User-provided class for the main dialog box
            )}
            style={{ animationDuration: `${animationDuration ?? 150}ms` }}
          >
            <UnifiedModalProvider value={contextValue}>
              {children}
            </UnifiedModalProvider>
            {showCloseButton && (
              <DialogPrimitive.Close
                className={cn(
                  'absolute right-4 top-4 rounded-sm opacity-70 ring-offset-background transition-opacity',
                  'hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
                  'disabled:pointer-events-none data-[state=open]:bg-accent data-[state=open]:text-muted-foreground'
                )}
                onClick={() => handleOpenChange(false)}
              >
                <X className="h-4 w-4" />
                <span className="sr-only">Close</span>
              </DialogPrimitive.Close>
            )}
          </DialogPrimitive.Content>
        </div>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
};

UnifiedModalDialog.displayName = 'UnifiedModalDialog';