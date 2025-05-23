﻿// AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalHeader.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalHeaderProps extends React.HTMLAttributes<HTMLDivElement> {}

// Define themeable properties for the UnifiedModalHeader component
export const themeableProps = {};

export const UnifiedModalHeader: React.FC<UnifiedModalHeaderProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];
  
  // Get theme style overrides from window.theme if available
  const themeStyle = window?.theme?.UnifiedModalHeader?.style || {};

  return (
    <div 
      className={cn('UnifiedModalHeader', variantConfig.headerClassName, className)} 
      style={{
        backgroundColor: 'var(--unifiedmodalheader-bg, var(--global-background-color, transparent))',
        color: 'var(--unifiedmodalheader-text-color, var(--global-text-color, var(--foreground)))',
        borderColor: 'var(--unifiedmodalheader-border-color, var(--global-border-color, var(--border)))',
        fontSize: 'var(--unifiedmodalheader-font-size, var(--global-font-size, 1.25rem))',
        fontWeight: 'var(--unifiedmodalheader-font-weight, 600)',
        padding: 'var(--unifiedmodalheader-padding, inherit)',
        fontFamily: 'var(--unifiedmodalheader-font-family, var(--global-font-family, inherit))',
        borderRadius: 'var(--unifiedmodalheader-border-radius, var(--global-border-radius, inherit))',
        boxShadow: 'var(--unifiedmodalheader-box-shadow, var(--global-box-shadow, none))',
        ...themeStyle
      }}
      {...props}
    >
      {children}
    </div>
  );
};

UnifiedModalHeader.displayName = 'UnifiedModalHeader';