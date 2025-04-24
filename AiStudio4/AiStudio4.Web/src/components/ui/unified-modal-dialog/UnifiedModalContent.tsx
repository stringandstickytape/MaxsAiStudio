// AiStudio4.Web/src/components/ui/unified-modal-dialog/UnifiedModalContent.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalContentProps extends React.HTMLAttributes<HTMLDivElement> {}

export const UnifiedModalContent: React.FC<UnifiedModalContentProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];

  // Base classes ensure flex-grow and overflow handling
  const baseClasses = 'flex-1 min-h-0 overflow-auto';

  return (
    <div className={cn(baseClasses, variantConfig.contentClassName, className)} {...props}>
      {children}
    </div>
  );
};

UnifiedModalContent.displayName = 'UnifiedModalContent';