// AiStudioClient/src/components/ui/unified-modal-dialog/variants.ts

import { ModalSize, ModalHeight, ModalVariant, ModalVariantConfig, ModalPosition, ModalAnimation } from './types';

export const modalSizeClasses: Record<Exclude<ModalSize, 'custom'>, string> = {
  sm: 'w-[400px] max-w-[95vw]',
  md: 'w-[600px] max-w-[95vw]',
  lg: 'w-[800px] max-w-[95vw]',
  xl: 'w-[900px] max-w-[95vw]',
  '2xl': 'w-[1024px] max-w-[95vw]',
  '3xl': 'w-[1280px] max-w-[95vw]',
  '4xl': 'w-[1536px] max-w-[95vw]',
  full: 'w-[95vw]',
};

export const modalHeightClasses: Record<Exclude<ModalHeight, 'custom'>, string> = {
  auto: 'max-h-[90vh]',
  sm: 'h-[300px] max-h-[90vh]',
  md: 'h-[500px] max-h-[90vh]',
  lg: 'h-[700px] max-h-[90vh]',
  xl: 'h-[800px] max-h-[90vh]',
  full: 'h-[90vh]',
};

export const modalPositionClasses: Record<ModalPosition, string> = {
  center: 'items-center justify-center',
  top: 'items-start justify-center',
  bottom: 'items-end justify-center',
  left: 'items-center justify-start',
  right: 'items-center justify-end',
};

export const modalVariants: Record<ModalVariant, ModalVariantConfig> = {
  default: {
    size: 'md',
    height: 'auto',
    contentClassName: 'p-6',
    headerClassName: 'px-6 pt-6 pb-2',
    footerClassName: 'px-6 py-4 flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'fade',
    position: 'center',
  },
  settings: {
    size: 'xl',
    height: 'lg',
    contentClassName: 'p-0 flex flex-col',
    headerClassName: 'px-6 pt-6 pb-2 border-b border-border',
    footerClassName: 'px-6 py-4 border-t border-border flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'fade',
    position: 'center',
  },
  library: {
    size: 'xl',
    height: 'lg',
    contentClassName: 'p-0 flex flex-col',
    headerClassName: 'px-6 pt-6 pb-2 border-b border-border',
    footerClassName: 'px-6 py-4 border-t border-border flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'fade',
    position: 'center',
  },
  form: {
    size: 'md',
    height: 'auto',
    contentClassName: 'p-6',
    headerClassName: 'mb-4',
    footerClassName: 'mt-6 flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'zoom',
    position: 'center',
  },
  confirmation: {
    size: 'sm',
    height: 'auto',
    contentClassName: 'p-6',
    headerClassName: 'mb-2',
    footerClassName: 'mt-6 flex justify-end space-x-2',
    showCloseButton: false,
    animation: 'zoom',
    position: 'center',
  },
};