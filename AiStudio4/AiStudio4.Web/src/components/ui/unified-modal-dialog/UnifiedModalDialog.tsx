// AiStudio4.Web/src/components/ui/unified-modal-dialog/UnifiedModalDialog.tsx

import React, { useEffect, useMemo, useRef } from 'react'; // Added useRef
import * as DialogPrimitive from '@radix-ui/react-dialog';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { UnifiedModalDialogProps, UnifiedModalContextProps } from './types';
import { modalVariants, modalSizeClasses, modalHeightClasses, modalPositionClasses } from './variants';
import { UnifiedModalProvider } from './UnifiedModalContext';

// Define themeable properties for the UnifiedModalDialog component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--unifiedmodaldialog-bg',
    description: 'Background color of the modal',
    default: 'var(--background)',
  },
  borderColor: {
    cssVar: '--unifiedmodaldialog-border-color',
    description: 'Border color of the modal',
    default: 'var(--border)',
  },
  textColor: {
    cssVar: '--unifiedmodaldialog-text-color',
    description: 'Text color of the modal',
    default: 'var(--foreground)',
  },
  borderRadius: {
    cssVar: '--unifiedmodaldialog-border-radius',
    description: 'Border radius of the modal',
    default: '0.5rem',
  },
  boxShadow: {
    cssVar: '--unifiedmodaldialog-box-shadow',
    description: 'Box shadow of the modal',
    default: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
  },
  overlayColor: {
    cssVar: '--unifiedmodaldialog-overlay-color',
    description: 'Background color of the overlay',
    default: 'rgba(0, 0, 0, 0.8)',
  },
  closeButtonColor: {
    cssVar: '--unifiedmodaldialog-close-button-color',
    description: 'Color of the close button',
    default: 'var(--foreground)',
  },
  closeButtonHoverColor: {
    cssVar: '--unifiedmodaldialog-close-button-hover-color',
    description: 'Hover color of the close button',
    default: 'var(--foreground)',
  },
  closeButtonBgHover: {
    cssVar: '--unifiedmodaldialog-close-button-bg-hover',
    description: 'Background color of the close button on hover',
    default: 'var(--accent)',
  },
  // Arbitrary style overrides
  style: {
    description: 'Arbitrary CSS style for the modal container',
    default: {},
  },
  overlayStyle: {
    description: 'Arbitrary CSS style for the modal overlay',
    default: {},
  },
  closeButtonStyle: {
    description: 'Arbitrary CSS style for the close button',
    default: {},
  },
};

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
  
  // Get theme style overrides from window.theme if available
  const themeStyle = window?.theme?.UnifiedModalDialog?.style || {};

  // Radix Dialog handles basic focus management (initial focus, return focus).
  // FocusTrap enhances this by ensuring focus stays *within* the modal.
  return (
    <DialogPrimitive.Root open={open} onOpenChange={handleOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay
          className={cn(
            'UnifiedModalDialog fixed inset-0 z-50',
            'data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0',
            overlayClassName
          )}
          style={{
            backgroundColor: 'var(--unifiedmodaldialog-overlay-color, rgba(0, 0, 0, 0.8))',
            animationDuration: `${animationDuration ?? 150}ms`,
            ...(window?.theme?.UnifiedModalDialog?.overlayStyle || {})
          }}
        />
        {/* Use DialogPrimitive's built-in focus management instead of FocusTrap */}
        <div className={cn('UnifiedModalDialog fixed inset-0 z-50 flex overflow-auto', positionClass)}>
          <DialogPrimitive.Content
            id={id}
            onInteractOutside={handleInteractOutside}
            onEscapeKeyDown={handleEscapeKeyDown}
            aria-label={ariaLabel}
            aria-describedby={ariaDescribedBy ?? (id ? `${id}-description` : undefined)} // Auto-generate describedby if id exists
            aria-modal="true" // Explicitly mark as modal
            className={cn(
              'UnifiedModalDialog relative z-50 flex flex-col border shadow-lg',
              'm-4', // Add some margin to prevent touching edges unless fullScreen
              fullScreen ? 'w-full h-full m-0 border-0 rounded-none' :
              cn(
                size === 'custom' ? '' : sizeClass,
                height === 'custom' ? '' : heightClass
              ),
              animationClasses,
              className // User-provided class for the main dialog box
            )}
            style={{
              backgroundColor: 'var(--unifiedmodaldialog-bg, var(--background))',
              color: 'var(--unifiedmodaldialog-text-color, var(--foreground))',
              borderColor: 'var(--unifiedmodaldialog-border-color, var(--border))',
              borderRadius: 'var(--unifiedmodaldialog-border-radius, 0.5rem)',
              boxShadow: 'var(--unifiedmodaldialog-box-shadow, 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05))',
              animationDuration: `${animationDuration ?? 150}ms`,
              ...themeStyle
            }}
          >
            <UnifiedModalProvider value={contextValue}>
              {children}
            </UnifiedModalProvider>
            {showCloseButton && (
              <DialogPrimitive.Close
                className={cn(
                  'UnifiedModalDialog absolute right-4 top-4 rounded-sm opacity-70 ring-offset-background transition-opacity',
                  'hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
                  'disabled:pointer-events-none'
                )}
                onClick={() => handleOpenChange(false)}
                style={{
                  color: 'var(--unifiedmodaldialog-close-button-color, var(--foreground))',
                  '--hover-color': 'var(--unifiedmodaldialog-close-button-hover-color, var(--foreground))',
                  '--hover-bg': 'var(--unifiedmodaldialog-close-button-bg-hover, var(--accent))',
                  ...(window?.theme?.UnifiedModalDialog?.closeButtonStyle || {})
                }}
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