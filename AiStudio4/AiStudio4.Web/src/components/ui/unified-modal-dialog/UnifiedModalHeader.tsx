// AiStudio4.Web/src/components/ui/unified-modal-dialog/UnifiedModalHeader.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalHeaderProps extends React.HTMLAttributes<HTMLDivElement> {}

export const UnifiedModalHeader: React.FC<UnifiedModalHeaderProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];

  return (
    <div className={cn(variantConfig.headerClassName, className)} {...props}>
      {children}
    </div>
  );
};

UnifiedModalHeader.displayName = 'UnifiedModalHeader';