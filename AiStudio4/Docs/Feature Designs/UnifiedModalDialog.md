# Unified Modal Dialog Design Document

## Overview

This document outlines the design for a unified modal dialog component that will replace the various modal implementations currently used throughout the AiStudio4 application. The goal is to create a consistent, flexible, and maintainable modal system that can handle all current and future dialog requirements.

## Current State Analysis

The application currently uses multiple implementations of modal dialogs for different features:

1. **Settings Dialog** - Fixed size (900px width, 700px height), tabbed interface
2. **System/User Prompt Dialogs** - Fixed size (900px width, 700px height), library view with editor
3. **Tool Dialog** - Larger size (max-w-4xl, 80vh height), panel-based interface
4. **Theme Library Dialog** - Large size (max-w-4xl, 90vh max height), grid layout
5. **MCP Servers Dialog** - Medium size (max-w-3xl, 70vh height), list with toggles
6. **Model/Provider Management Dialogs** - Smaller size (max-w-xl), form-based interface
7. **Confirmation Dialogs** - Small size, simple question with action buttons

These implementations share common patterns but have inconsistencies in sizing, structure, state management, and styling.

### Existing Modal Implementation Files

The current modal implementations are spread across multiple files:

1. **Base UI Components:**
   - `src/components/ui/dialog.tsx` - Base Dialog component from shadcn/ui
   - `src/components/ui/alert-dialog.tsx` - Alert Dialog component from shadcn/ui

2. **Modal Dialogs:**
   - `src/components/modals/SettingsDialog.tsx` - Settings dialog
   - `src/components/SystemPrompt/SystemPromptDialog.tsx` - System prompt dialog
   - `src/components/UserPrompt/UserPromptDialog.tsx` - User prompt dialog
   - `src/components/tools/ToolDialog.tsx` - Tool dialog
   - `src/components/ThemeLibrary.tsx` - Theme library dialog
   - `src/components/servers/ServerDialog.tsx` - MCP servers dialog

3. **Settings Tabs:**
   - `src/components/settings/AppearanceTab.tsx` - Appearance settings
   - `src/components/settings/ModelManagement.tsx` - Model management
   - `src/components/settings/ServiceProviderManagement.tsx` - Service provider management
   - `src/components/settings/ThemeManagement.tsx` - Theme management

4. **State Management:**
   - `src/stores/useModalStore.ts` - Zustand store for modal state management
   - `src/types/ui.ts` - Contains ModalId type definition

## Requirements

### Functional Requirements

1. Support all existing dialog use cases without loss of functionality
2. Provide consistent UI/UX across all dialog types
3. Support different content types (forms, lists, grids, tabs)
4. Support different sizes and layouts
5. Support nested dialogs (e.g., confirmation dialogs within settings)
6. Maintain or improve accessibility

### Technical Requirements

1. Integrate with existing state management (useModalStore)
2. Support both global and local state management
3. Maintain TypeScript type safety
4. Follow React best practices (composition over inheritance)
5. Ensure responsive behavior
6. Maintain or improve performance

## Component Design

### Component Structure

```
UnifiedModalDialog/
├── index.tsx - Main export
├── UnifiedModalDialog.tsx - Core component
├── UnifiedModalContent.tsx - Content wrapper
├── UnifiedModalHeader.tsx - Header component
├── UnifiedModalFooter.tsx - Footer component
├── UnifiedModalContext.tsx - Context for internal state
├── types.ts - TypeScript types
└── variants.ts - Predefined size and style variants
```

### Core Component API

```typescript
export interface UnifiedModalDialogProps {
  // Required props
  open: boolean;
  onOpenChange: (open: boolean) => void;
  
  // Optional props with defaults
  id?: string; // For useModalStore integration
  variant?: ModalVariant; // 'default' | 'settings' | 'library' | 'form' | 'confirmation'
  size?: ModalSize; // 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '4xl' | 'full'
  height?: ModalHeight; // 'auto' | 'sm' | 'md' | 'lg' | 'xl' | 'full'
  position?: 'center' | 'top' | 'right' | 'bottom' | 'left';
  fullScreen?: boolean;
  preventClose?: boolean; // Prevent closing on escape or outside click
  closeOnClickOutside?: boolean;
  showCloseButton?: boolean;
  className?: string;
  contentClassName?: string;
  overlayClassName?: string;
  children: React.ReactNode;
  
  // Animation options
  animation?: 'fade' | 'zoom' | 'slide';
  animationDuration?: number;
  
  // Accessibility
  ariaLabel?: string;
  ariaDescribedBy?: string;
  
  // Events
  onClose?: () => void;
  afterClose?: () => void;
  beforeOpen?: () => void;
  afterOpen?: () => void;
}
```

### Size Variants

```typescript
export type ModalSize = 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '3xl' | '4xl' | 'full' | 'custom';

export const modalSizeClasses: Record<ModalSize, string> = {
  sm: 'w-[400px] max-w-[95vw]',
  md: 'w-[600px] max-w-[95vw]',
  lg: 'w-[800px] max-w-[95vw]',
  xl: 'w-[900px] max-w-[95vw]',
  '2xl': 'w-[1024px] max-w-[95vw]',
  '3xl': 'w-[1280px] max-w-[95vw]',
  '4xl': 'w-[1536px] max-w-[95vw]',
  full: 'w-[95vw]',
  custom: '', // No default classes, use className prop
};

export type ModalHeight = 'auto' | 'sm' | 'md' | 'lg' | 'xl' | 'full' | 'custom';

export const modalHeightClasses: Record<ModalHeight, string> = {
  auto: 'max-h-[90vh]',
  sm: 'h-[300px] max-h-[90vh]',
  md: 'h-[500px] max-h-[90vh]',
  lg: 'h-[700px] max-h-[90vh]',
  xl: 'h-[800px] max-h-[90vh]',
  full: 'h-[90vh]',
  custom: '', // No default classes, use className prop
};
```

### Predefined Variants

```typescript
export type ModalVariant = 'default' | 'settings' | 'library' | 'form' | 'confirmation';

export interface ModalVariantConfig {
  size: ModalSize;
  height: ModalHeight;
  contentClassName: string;
  headerClassName: string;
  footerClassName: string;
  showCloseButton: boolean;
  animation: 'fade' | 'zoom' | 'slide';
}

export const modalVariants: Record<ModalVariant, ModalVariantConfig> = {
  default: {
    size: 'md',
    height: 'auto',
    contentClassName: 'p-6',
    headerClassName: 'px-6 pt-6 pb-2',
    footerClassName: 'px-6 py-4 flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'fade',
  },
  settings: {
    size: 'xl',
    height: 'lg',
    contentClassName: 'p-0 flex flex-col',
    headerClassName: 'px-6 pt-6 pb-2 border-b border-gray-700',
    footerClassName: 'px-6 py-4 border-t border-gray-700 flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'fade',
  },
  library: {
    size: 'xl',
    height: 'lg',
    contentClassName: 'p-0 flex flex-col',
    headerClassName: 'px-6 pt-6 pb-2 border-b border-gray-700',
    footerClassName: 'px-6 py-4 border-t border-gray-700 flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'fade',
  },
  form: {
    size: 'md',
    height: 'auto',
    contentClassName: 'p-6',
    headerClassName: 'mb-4',
    footerClassName: 'mt-6 flex justify-end space-x-2',
    showCloseButton: true,
    animation: 'zoom',
  },
  confirmation: {
    size: 'sm',
    height: 'auto',
    contentClassName: 'p-6',
    headerClassName: 'mb-2',
    footerClassName: 'mt-6 flex justify-end space-x-2',
    showCloseButton: false,
    animation: 'zoom',
  },
};
```

### Integration with useModalStore

The component will integrate with the existing useModalStore for global state management:

```typescript
// Example usage with useModalStore
import { useModalStore, ModalId } from '@/stores/useModalStore';

interface ModalManagerProps {
  children: React.ReactNode;
}

export function ModalManager({ children }: ModalManagerProps) {
  const { openModalId, modalProps, closeModal } = useModalStore();
  
  return (
    <>
      {children}
      
      <UnifiedModalDialog
        open={openModalId === 'settings'}
        onOpenChange={(open) => !open && closeModal()}
        variant="settings"
        {...modalProps}
      >
        <SettingsContent />
      </UnifiedModalDialog>
      
      <UnifiedModalDialog
        open={openModalId === 'systemPrompt'}
        onOpenChange={(open) => !open && closeModal()}
        variant="library"
        {...modalProps}
      >
        <SystemPromptContent />
      </UnifiedModalDialog>
      
      {/* Other modals */}
    </>
  );
}
```

### Sub-Components

#### UnifiedModalHeader

```typescript
export interface UnifiedModalHeaderProps {
  className?: string;
  children: React.ReactNode;
}

export function UnifiedModalHeader({ className, children }: UnifiedModalHeaderProps) {
  const { variant } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant || 'default'];
  
  return (
    <div className={cn(variantConfig.headerClassName, className)}>
      {children}
    </div>
  );
}
```

#### UnifiedModalFooter

```typescript
export interface UnifiedModalFooterProps {
  className?: string;
  children: React.ReactNode;
}

export function UnifiedModalFooter({ className, children }: UnifiedModalFooterProps) {
  const { variant } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant || 'default'];
  
  return (
    <div className={cn(variantConfig.footerClassName, className)}>
      {children}
    </div>
  );
}
```

#### UnifiedModalContent

```typescript
export interface UnifiedModalContentProps {
  className?: string;
  children: React.ReactNode;
}

export function UnifiedModalContent({ className, children }: UnifiedModalContentProps) {
  const { variant } = useUnifiedModalContext();
  const variantConfig = modalVariants[variant || 'default'];
  
  return (
    <div className={cn('flex-1 overflow-auto', variantConfig.contentClassName, className)}>
      {children}
    </div>
  );
}
```

## Implementation Plan

### Phase 1: Core Component

1. Create the base UnifiedModalDialog component
   - Create file: `src/components/ui/unified-modal-dialog/UnifiedModalDialog.tsx`
   - Create file: `src/components/ui/unified-modal-dialog/types.ts`
   - Create file: `src/components/ui/unified-modal-dialog/variants.ts`
2. Implement size and variant configurations
3. Ensure accessibility features
4. Add animation options

### Phase 2: Sub-Components

1. Create Header, Content, and Footer components
   - Create file: `src/components/ui/unified-modal-dialog/UnifiedModalHeader.tsx`
   - Create file: `src/components/ui/unified-modal-dialog/UnifiedModalContent.tsx`
   - Create file: `src/components/ui/unified-modal-dialog/UnifiedModalFooter.tsx`
   - Create file: `src/components/ui/unified-modal-dialog/UnifiedModalContext.tsx`
2. Implement context for sharing state between components
3. Add styling and layout options
4. Create barrel file: `src/components/ui/unified-modal-dialog/index.ts`

### Phase 3: Integration

1. Update useModalStore to support the new component
   - Modify file: `src/stores/useModalStore.ts`
   - Modify file: `src/types/ui.ts` (if needed)
2. Create a ModalManager component for global modals
   - Create file: `src/components/modals/ModalManager.tsx`
3. Update existing modal implementations to use the new component
   - Modify file: `src/components/modals/SettingsDialog.tsx`
   - Modify file: `src/components/SystemPrompt/SystemPromptDialog.tsx`
   - Modify file: `src/components/UserPrompt/UserPromptDialog.tsx`
   - Modify file: `src/components/tools/ToolDialog.tsx`
   - Modify file: `src/components/ThemeLibrary.tsx`
   - Modify file: `src/components/servers/ServerDialog.tsx`

### Phase 4: Testing and Refinement

1. Test all existing use cases
2. Gather feedback and refine the implementation
3. Document the component API and usage patterns
   - Create file: `src/components/ui/unified-modal-dialog/README.md`

## Migration Strategy

1. Create the new component without changing existing implementations
2. Migrate one dialog type at a time, starting with simpler ones:
   - First: Confirmation dialogs (simplest)
   - Second: Form dialogs (Model/Provider management)
   - Third: Library dialogs (System/User prompts, Theme library)
   - Fourth: Settings dialog (most complex with tabs)
   - Fifth: Tool dialog and MCP servers dialog
3. Update the useModalStore integration as needed
4. Remove old implementations once all dialogs are migrated

## Usage Examples

### Basic Usage

```tsx
import { useState } from 'react';
import { 
  UnifiedModalDialog, 
  UnifiedModalHeader, 
  UnifiedModalContent, 
  UnifiedModalFooter 
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';

export function ExampleDialog() {
  const [open, setOpen] = useState(false);
  
  return (
    <>
      <Button onClick={() => setOpen(true)}>Open Dialog</Button>
      
      <UnifiedModalDialog 
        open={open} 
        onOpenChange={setOpen}
        variant="default"
      >
        <UnifiedModalHeader>
          <h2 className="text-xl font-semibold">Example Dialog</h2>
        </UnifiedModalHeader>
        
        <UnifiedModalContent>
          <p>This is an example dialog using the unified component.</p>
        </UnifiedModalContent>
        
        <UnifiedModalFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
          <Button onClick={() => setOpen(false)}>Confirm</Button>
        </UnifiedModalFooter>
      </UnifiedModalDialog>
    </>
  );
}
```

### Settings Dialog

```tsx
import { useModalStore } from '@/stores/useModalStore';
import { 
  UnifiedModalDialog, 
  UnifiedModalHeader, 
  UnifiedModalContent 
} from '@/components/ui/unified-modal-dialog';
import { SettingsPanel } from '@/components/SettingsPanel';

export function SettingsDialog() {
  const { openModalId, closeModal } = useModalStore();
  const isOpen = openModalId === 'settings';
  
  return (
    <UnifiedModalDialog 
      open={isOpen} 
      onOpenChange={(open) => !open && closeModal()}
      variant="settings"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">Settings</h2>
      </UnifiedModalHeader>
      
      <UnifiedModalContent>
        {isOpen && <SettingsPanel />}
      </UnifiedModalContent>
    </UnifiedModalDialog>
  );
}
```

### Confirmation Dialog

```tsx
import { 
  UnifiedModalDialog, 
  UnifiedModalHeader, 
  UnifiedModalContent, 
  UnifiedModalFooter 
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';

interface ConfirmationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel?: () => void;
  danger?: boolean;
}

export function ConfirmationDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  onConfirm,
  onCancel,
  danger = false,
}: ConfirmationDialogProps) {
  const handleCancel = () => {
    onOpenChange(false);
    onCancel?.();
  };
  
  const handleConfirm = () => {
    onOpenChange(false);
    onConfirm();
  };
  
  return (
    <UnifiedModalDialog 
      open={open} 
      onOpenChange={onOpenChange}
      variant="confirmation"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>
      
      <UnifiedModalContent>
        <p className="text-gray-200">{description}</p>
      </UnifiedModalContent>
      
      <UnifiedModalFooter>
        <Button variant="outline" onClick={handleCancel}>
          {cancelLabel}
        </Button>
        <Button 
          variant={danger ? 'destructive' : 'default'} 
          onClick={handleConfirm}
        >
          {confirmLabel}
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}
```

## Accessibility Considerations

1. Keyboard navigation (Tab, Escape)
2. Focus management (trap focus within modal)
3. ARIA attributes (role, aria-modal, aria-labelledby, aria-describedby)
4. Screen reader announcements
5. Color contrast for all themes

## Performance Considerations

1. Lazy loading of modal content
2. Optimized animations
3. Efficient state management
4. Proper cleanup on unmount

## Conclusion

The UnifiedModalDialog component will provide a consistent, flexible, and accessible solution for all modal dialog needs in the AiStudio4 application. By standardizing the modal implementation, we can improve the user experience, reduce code duplication, and make future maintenance easier.

## File Structure After Implementation

After implementation, the file structure for modals will be:

```
src/
├── components/
│   ├── modals/
│   │   ├── ModalManager.tsx         # New component to manage all modals
│   │   ├── SettingsDialog.tsx       # Updated to use UnifiedModalDialog
│   │   └── ... (other specific modal implementations)
│   ├── ui/
│   │   ├── unified-modal-dialog/    # New unified modal component
│   │   │   ├── index.ts             # Barrel file exporting all components
│   │   │   ├── README.md            # Documentation
│   │   │   ├── types.ts             # TypeScript types
│   │   │   ├── variants.ts          # Size and style variants
│   │   │   ├── UnifiedModalContext.tsx  # Context for state sharing
│   │   │   ├── UnifiedModalDialog.tsx   # Main component
│   │   │   ├── UnifiedModalHeader.tsx   # Header component
│   │   │   ├── UnifiedModalContent.tsx  # Content component
│   │   │   └── UnifiedModalFooter.tsx   # Footer component
│   │   ├── dialog.tsx               # Original shadcn/ui dialog (can be kept for reference)
│   │   └── alert-dialog.tsx         # Original shadcn/ui alert dialog (can be kept for reference)
│   ├── SystemPrompt/
│   │   └── SystemPromptDialog.tsx   # Updated to use UnifiedModalDialog
│   ├── UserPrompt/
│   │   └── UserPromptDialog.tsx     # Updated to use UnifiedModalDialog
│   ├── tools/
│   │   └── ToolDialog.tsx           # Updated to use UnifiedModalDialog
│   ├── servers/
│   │   └── ServerDialog.tsx         # Updated to use UnifiedModalDialog
│   └── ThemeLibrary.tsx             # Updated to use UnifiedModalDialog
└── stores/
    └── useModalStore.ts             # Updated to support UnifiedModalDialog
```

This structure maintains the existing organization while introducing the new unified components in a dedicated directory.