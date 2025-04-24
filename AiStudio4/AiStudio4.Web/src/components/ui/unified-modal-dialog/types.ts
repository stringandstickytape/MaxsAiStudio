// AiStudio4.Web/src/components/ui/unified-modal-dialog/types.ts

import React from 'react';

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '4xl' | 'full' | 'custom';
export type ModalHeight = 'auto' | 'sm' | 'md' | 'lg' | 'xl' | 'full' | 'custom';
export type ModalVariant = 'default' | 'settings' | 'library' | 'form' | 'confirmation';
export type ModalAnimation = 'fade' | 'zoom' | 'slide' | 'none';
export type ModalPosition = 'center' | 'top' | 'right' | 'bottom' | 'left';

export interface UnifiedModalDialogProps {
  // Required props
  open: boolean;
  onOpenChange: (open: boolean) => void;

  // Optional props with defaults
  id?: string; // For useModalStore integration or direct targeting
  variant?: ModalVariant;
  size?: ModalSize;
  height?: ModalHeight;
  position?: ModalPosition;
  fullScreen?: boolean;
  preventClose?: boolean; // Prevent closing on escape or outside click
  closeOnClickOutside?: boolean; // Default true
  showCloseButton?: boolean; // Default true
  className?: string; // Applied to the content container
  contentClassName?: string; // Applied to the scrollable content area within the container
  overlayClassName?: string;
  children: React.ReactNode;

  // Animation options
  animation?: ModalAnimation;
  animationDuration?: number; // in ms

  // Accessibility
  ariaLabel?: string;
  ariaDescribedBy?: string;

  // Events
  onClose?: () => void; // Called just before closing animation starts
  afterClose?: () => void; // Called after closing animation finishes
  beforeOpen?: () => void; // Called just before opening animation starts
  afterOpen?: () => void; // Called after opening animation finishes
}

export interface ModalVariantConfig {
  size: ModalSize;
  height: ModalHeight;
  contentClassName: string;
  headerClassName: string;
  footerClassName: string;
  showCloseButton: boolean;
  animation: ModalAnimation;
  position: ModalPosition;
}

export interface UnifiedModalContextProps {
  variant?: ModalVariant;
  size?: ModalSize;
  height?: ModalHeight;
  showCloseButton?: boolean;
  onClose?: () => void; // Function to trigger close from sub-components if needed
}