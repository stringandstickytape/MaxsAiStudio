// AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalContent.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalContentProps extends React.HTMLAttributes<HTMLDivElement> {}

// Define themeable properties for the UnifiedModalContent component
export const themeableProps = {};

export const UnifiedModalContent: React.FC<UnifiedModalContentProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];

  // Base classes ensure flex-grow and overflow handling
  const baseClasses = 'flex-1 min-h-0 overflow-auto';
  
  // Get theme style overrides from window.theme if available
  const themeStyle = window?.theme?.UnifiedModalContent?.style || {};

  return (
    <div 
      className={cn('UnifiedModalContent', baseClasses, variantConfig.contentClassName, className)} 
      style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        padding: 'inherit',
        '--scrollbar-thumb': 'rgba(255, 255, 255, 0.2)',
        '--scrollbar-track': 'transparent',
        scrollbarColor: 'var(--scrollbar-thumb) var(--scrollbar-track)',
        scrollbarWidth: 'thin',
        fontFamily: 'var(--global-font-family)',
        fontSize: 'var(--global-font-size)',
        borderRadius: 'var(--global-border-radius)',
        borderColor: 'var(--global-border-color)',
        boxShadow: 'var(--global-box-shadow)',
        ...themeStyle
      }}
      {...props}
    >
      {children}
    </div>
  );
};

UnifiedModalContent.displayName = 'UnifiedModalContent';