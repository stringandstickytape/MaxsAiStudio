// AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalFooter.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalFooterProps extends React.HTMLAttributes<HTMLDivElement> {}

// Define themeable properties for the UnifiedModalFooter component
export const themeableProps = {};

export const UnifiedModalFooter: React.FC<UnifiedModalFooterProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];
  
  // Get theme style overrides from window.theme if available
  const themeStyle = window?.theme?.UnifiedModalFooter?.style || {};

  return (
    <div 
      className={cn('UnifiedModalFooter', variantConfig.footerClassName, className)} 
      style={{
        backgroundColor: 'var(--unifiedmodalfooter-bg, var(--global-background-color, transparent))',
        color: 'var(--unifiedmodalfooter-text-color, var(--global-text-color, var(--foreground)))',
        borderColor: 'var(--unifiedmodalfooter-border-color, var(--global-border-color, var(--border)))',
        padding: 'var(--unifiedmodalfooter-padding, inherit)',
        '--button-spacing': 'var(--unifiedmodalfooter-button-spacing, 0.5rem)',
        fontFamily: 'var(--unifiedmodalfooter-font-family, var(--global-font-family, inherit))',
        fontSize: 'var(--unifiedmodalfooter-font-size, var(--global-font-size, inherit))',
        borderRadius: 'var(--unifiedmodalfooter-border-radius, var(--global-border-radius, inherit))',
        boxShadow: 'var(--unifiedmodalfooter-box-shadow, var(--global-box-shadow, none))',
        ...themeStyle
      }}
      {...props}
    >
      {children}
    </div>
  );
};

UnifiedModalFooter.displayName = 'UnifiedModalFooter';