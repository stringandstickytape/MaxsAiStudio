// AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalHeader.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalHeaderProps extends React.HTMLAttributes<HTMLDivElement> {}

// Define themeable properties for the UnifiedModalHeader component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--unifiedmodalheader-bg',
    description: 'Background color of the modal header',
    default: 'transparent',
  },
  textColor: {
    cssVar: '--unifiedmodalheader-text-color',
    description: 'Text color of the modal header',
    default: 'var(--foreground)',
  },
  borderColor: {
    cssVar: '--unifiedmodalheader-border-color',
    description: 'Border color of the modal header',
    default: 'var(--border)',
  },
  fontSize: {
    cssVar: '--unifiedmodalheader-font-size',
    description: 'Font size of the modal header',
    default: '1.25rem',
  },
  fontWeight: {
    cssVar: '--unifiedmodalheader-font-weight',
    description: 'Font weight of the modal header',
    default: '600',
  },
  padding: {
    cssVar: '--unifiedmodalheader-padding',
    description: 'Padding of the modal header',
    default: 'inherit',
  },
  // Arbitrary style overrides
  style: {
    description: 'Arbitrary CSS style for the modal header',
    default: {},
  },
};

export const UnifiedModalHeader: React.FC<UnifiedModalHeaderProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];
  
  // Get theme style overrides from window.theme if available
  const themeStyle = window?.theme?.UnifiedModalHeader?.style || {};

  return (
    <div 
      className={cn('UnifiedModalHeader', variantConfig.headerClassName, className)} 
      style={{
        backgroundColor: 'var(--unifiedmodalheader-bg, var(--global-backgroundColor, transparent))',
        color: 'var(--unifiedmodalheader-text-color, var(--global-textColor, var(--foreground)))',
        borderColor: 'var(--unifiedmodalheader-border-color, var(--global-borderColor, var(--border)))',
        fontSize: 'var(--unifiedmodalheader-font-size, var(--global-fontSize, 1.25rem))',
        fontWeight: 'var(--unifiedmodalheader-font-weight, 600)',
        padding: 'var(--unifiedmodalheader-padding, inherit)',
        fontFamily: 'var(--unifiedmodalheader-font-family, var(--global-fontFamily, inherit))',
        borderRadius: 'var(--unifiedmodalheader-border-radius, var(--global-borderRadius, inherit))',
        boxShadow: 'var(--unifiedmodalheader-box-shadow, var(--global-boxShadow, none))',
        ...themeStyle
      }}
      {...props}
    >
      {children}
    </div>
  );
};

UnifiedModalHeader.displayName = 'UnifiedModalHeader';