// AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalContent.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalContentProps extends React.HTMLAttributes<HTMLDivElement> {}

// Define themeable properties for the UnifiedModalContent component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--unifiedmodalcontent-bg',
    description: 'Background color of the modal content area',
    default: 'transparent',
  },
  textColor: {
    cssVar: '--unifiedmodalcontent-text-color',
    description: 'Text color of the modal content',
    default: 'var(--foreground)',
  },
  scrollbarThumbColor: {
    cssVar: '--unifiedmodalcontent-scrollbar-thumb',
    description: 'Color of the scrollbar thumb',
    default: 'rgba(255, 255, 255, 0.2)',
  },
  scrollbarTrackColor: {
    cssVar: '--unifiedmodalcontent-scrollbar-track',
    description: 'Color of the scrollbar track',
    default: 'transparent',
  },
  padding: {
    cssVar: '--unifiedmodalcontent-padding',
    description: 'Padding of the modal content area',
    default: 'inherit',
  },
  // Arbitrary style overrides
  style: {
    description: 'Arbitrary CSS style for the modal content area',
    default: {},
  },
};

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
        backgroundColor: 'var(--unifiedmodalcontent-bg, var(--global-background-color, transparent))',
        color: 'var(--unifiedmodalcontent-text-color, var(--global-text-color, var(--foreground)))',
        padding: 'var(--unifiedmodalcontent-padding, inherit)',
        '--scrollbar-thumb': 'var(--unifiedmodalcontent-scrollbar-thumb, rgba(255, 255, 255, 0.2))',
        '--scrollbar-track': 'var(--unifiedmodalcontent-scrollbar-track, transparent)',
        scrollbarColor: 'var(--scrollbar-thumb) var(--scrollbar-track)',
        scrollbarWidth: 'thin',
        fontFamily: 'var(--unifiedmodalcontent-font-family, var(--global-font-family, inherit))',
        fontSize: 'var(--unifiedmodalcontent-font-size, var(--global-font-size, inherit))',
        borderRadius: 'var(--unifiedmodalcontent-border-radius, var(--global-border-radius, inherit))',
        borderColor: 'var(--unifiedmodalcontent-border-color, var(--global-border-color, inherit))',
        boxShadow: 'var(--unifiedmodalcontent-box-shadow, var(--global-box-shadow, none))',
        ...themeStyle
      }}
      {...props}
    >
      {children}
    </div>
  );
};

UnifiedModalContent.displayName = 'UnifiedModalContent';