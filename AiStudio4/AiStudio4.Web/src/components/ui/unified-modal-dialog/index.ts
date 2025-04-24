// AiStudio4.Web/src/components/ui/unified-modal-dialog/index.ts

export * from './types';
export * from './variants';
export * from './UnifiedModalDialog';
export * from './UnifiedModalHeader';
export * from './UnifiedModalContent';
export * from './UnifiedModalFooter';
export * from './UnifiedModalContext'; // Exporting context related items might be optional

// Export themeableProps from all components for ThemeManager discovery
export { themeableProps as UnifiedModalDialogThemeableProps } from './UnifiedModalDialog';
export { themeableProps as UnifiedModalHeaderThemeableProps } from './UnifiedModalHeader';
export { themeableProps as UnifiedModalContentThemeableProps } from './UnifiedModalContent';
export { themeableProps as UnifiedModalFooterThemeableProps } from './UnifiedModalFooter';