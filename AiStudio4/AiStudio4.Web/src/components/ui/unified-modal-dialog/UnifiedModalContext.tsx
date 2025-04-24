// AiStudio4.Web/src/components/ui/unified-modal-dialog/UnifiedModalContext.tsx

import React, { createContext, useContext } from 'react';
import { UnifiedModalContextProps } from './types';

const UnifiedModalContext = createContext<UnifiedModalContextProps | undefined>(undefined);

export const UnifiedModalProvider: React.FC<{ value: UnifiedModalContextProps; children: React.ReactNode }> = ({ value, children }) => {
  return (
    <UnifiedModalContext.Provider value={value}>
      {children}
    </UnifiedModalContext.Provider>
  );
};

export const useUnifiedModalContext = (): UnifiedModalContextProps => {
  const context = useContext(UnifiedModalContext);
  if (!context) {
    throw new Error('useUnifiedModalContext must be used within a UnifiedModalProvider');
  }
  return context;
};