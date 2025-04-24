// AiStudio4.Web/src/components/ui/unified-modal-dialog/UnifiedModalFooter.tsx

import React from 'react';
import { cn } from '@/lib/utils';
import { useUnifiedModalContext } from './UnifiedModalContext';
import { modalVariants } from './variants';

export interface UnifiedModalFooterProps extends React.HTMLAttributes<HTMLDivElement> {}

export const UnifiedModalFooter: React.FC<UnifiedModalFooterProps> = ({ className, children, ...props }) => {
  const { variant = 'default' } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant];

  return (
    <div className={cn(variantConfig.footerClassName, className)} {...props}>
      {children}
    </div>
  );
};

UnifiedModalFooter.displayName = 'UnifiedModalFooter';