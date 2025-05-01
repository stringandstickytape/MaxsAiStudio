// AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalFooter.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalFooterProps extends React.HTMLAttributes<HTMLDivElement> {}

// Define themeable properties for the UnifiedModalFooter component
export const themeableProps = {
  backgroundColor: {
    cssVar: '--unifiedmodalfooter-bg',
    description: 'Background color of the modal footer',
    default: 'transparent',
  },
  textColor: {
    cssVar: '--unifiedmodalfooter-text-color',
    description: 'Text color of the modal footer',
    default: 'var(--foreground)',
  },
  borderColor: {
    cssVar: '--unifiedmodalfooter-border-color',
    description: 'Border color of the modal footer',
    default: 'var(--border)',
  },
  buttonSpacing: {
    cssVar: '--unifiedmodalfooter-button-spacing',
    description: 'Spacing between buttons in the footer',
    default: '0.5rem',
  },
  padding: {
    cssVar: '--unifiedmodalfooter-padding',
    description: 'Padding of the modal footer',
    default: 'inherit',
  },
  // Arbitrary style overrides
  style: {
    description: 'Arbitrary CSS style for the modal footer',
    default: {},
  },
  buttonStyle: {
    description: 'Arbitrary CSS style for buttons in the footer',
    default: {},
  },
};

export const UnifiedModalFooter: React.FC<UnifiedModalFooterProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];
  
  // Get theme style overrides from window.theme if available
  const themeStyle = window?.theme?.UnifiedModalFooter?.style || {};

  return (
    <div 
      className={cn('UnifiedModalFooter', variantConfig.footerClassName, className)} 
      style={{
        backgroundColor: 'var(--unifiedmodalfooter-bg, var(--global-backgroundColor, transparent))',
        color: 'var(--unifiedmodalfooter-text-color, var(--global-textColor, var(--foreground)))',
        borderColor: 'var(--unifiedmodalfooter-border-color, var(--global-borderColor, var(--border)))',
        padding: 'var(--unifiedmodalfooter-padding, inherit)',
        '--button-spacing': 'var(--unifiedmodalfooter-button-spacing, 0.5rem)',
        fontFamily: 'var(--unifiedmodalfooter-font-family, var(--global-fontFamily, inherit))',
        fontSize: 'var(--unifiedmodalfooter-font-size, var(--global-fontSize, inherit))',
        borderRadius: 'var(--unifiedmodalfooter-border-radius, var(--global-borderRadius, inherit))',
        boxShadow: 'var(--unifiedmodalfooter-box-shadow, var(--global-boxShadow, none))',
        ...themeStyle
      }}
      {...props}
    >
      {children}
    </div>
  );
};

UnifiedModalFooter.displayName = 'UnifiedModalFooter';