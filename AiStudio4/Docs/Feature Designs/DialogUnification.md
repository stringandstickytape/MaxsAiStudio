# Dialog Unification Phase 2: Standardization and Enhancement

## Overview

This document outlines the next phase of dialog unification for AiStudio4, building upon the successful implementation of the UnifiedModalDialog component. The goal is to further standardize modal patterns, create specialized components for common use cases, enhance the modal registry, and improve accessibility across all dialogs.

## Current State Analysis

The UnifiedModalDialog component has been successfully implemented with the following features:

1. **Core Components**:
   - `UnifiedModalDialog`: Main container component
   - `UnifiedModalHeader`: Standardized header component
   - `UnifiedModalContent`: Content area with overflow handling
   - `UnifiedModalFooter`: Footer with action buttons

2. **Variant System**:
   - Predefined variants: 'default', 'settings', 'library', 'form', 'confirmation'
   - Configurable sizes, heights, and positions
   - Animation options

3. **Integration with State Management**:
   - Works with useModalStore for global modal management
   - Supports local state for component-specific modals

4. **Migration Progress**:
   - Several dialogs have been migrated to the unified system
   - Some legacy implementations still exist

## Remaining Challenges

1. **Inconsistent Modal Patterns**: While the base components are standardized, the way they're used varies across the application.

2. **Modal Registry Limitations**: The current useModalStore is basic and doesn't provide type safety for modal props or handle complex modal workflows.

3. **Duplicate Code**: Common patterns like confirmation dialogs, form dialogs, etc. are reimplemented across components.

4. **Accessibility Gaps**: Some modals may not fully implement accessibility best practices.

5. **Nested Modal Management**: Handling of nested modals (e.g., confirmation dialog within settings) is not standardized.

## Requirements

### Functional Requirements

1. Create standardized patterns for common modal types (confirmation, form, wizard, etc.)
2. Improve modal state management with better typing and workflow support
3. Support nested modals with proper focus management
4. Standardize modal transitions and animations
5. Provide consistent error handling within modals

### Technical Requirements

1. Enhance useModalStore with TypeScript generics for type-safe modal props
2. Create higher-level modal components that encapsulate common patterns
3. Implement focus trapping and proper keyboard navigation
4. Ensure all modals meet WCAG 2.1 AA accessibility standards
5. Support dynamic content loading and lazy initialization

## Design Approach

### 1. Enhanced Modal Registry

Improve the modal store to provide better typing and management capabilities:

```typescript
// Enhanced modal store with type safety
import { create } from 'zustand';

// Define all possible modal types and their props
export interface ModalRegistry {
  settings: {};
  systemPrompt: { convId?: string; editPromptId?: string; createNew?: boolean };
  userPrompt: {};
  tool: {};
  theme: {};
  confirmation: {
    title: string;
    description: string;
    onConfirm: () => void;
    onCancel?: () => void;
    confirmLabel?: string;
    cancelLabel?: string;
    danger?: boolean;
  };
  form: {
    title: string;
    description?: string;
    onSubmit: (data: any) => void;
    onCancel?: () => void;
    initialData?: any;
    submitLabel?: string;
    cancelLabel?: string;
  };
  // Add other modal types here
}

export type ModalId = keyof ModalRegistry;

type ModalState = {
  openModalId: ModalId | null;
  modalProps: any; // Will be typed based on the modal ID
  modalStack: { id: ModalId; props: any }[]; // For nested modals
  openModal: <T extends ModalId>(id: T, props: ModalRegistry[T]) => void;
  closeModal: () => void;
  closeAllModals: () => void;
  openNestedModal: <T extends ModalId>(id: T, props: ModalRegistry[T]) => void;
  closeNestedModal: () => void;
};

export const useModalStore = create<ModalState>((set, get) => ({
  openModalId: null,
  modalProps: {},
  modalStack: [],
  
  openModal: (id, props) => set({ openModalId: id, modalProps: props }),
  
  closeModal: () => set({ openModalId: null, modalProps: {} }),
  
  closeAllModals: () => set({ openModalId: null, modalProps: {}, modalStack: [] }),
  
  openNestedModal: (id, props) => {
    const { openModalId, modalProps } = get();
    if (openModalId) {
      set({
        modalStack: [...get().modalStack, { id: openModalId, props: modalProps }],
        openModalId: id,
        modalProps: props,
      });
    } else {
      set({ openModalId: id, modalProps: props });
    }
  },
  
  closeNestedModal: () => {
    const { modalStack } = get();
    if (modalStack.length > 0) {
      const lastModal = modalStack[modalStack.length - 1];
      set({
        openModalId: lastModal.id,
        modalProps: lastModal.props,
        modalStack: modalStack.slice(0, -1),
      });
    } else {
      set({ openModalId: null, modalProps: {} });
    }
  },
}));
```

### 2. Specialized Modal Components

Create higher-level components for common modal patterns:

#### Confirmation Modal

```typescript
// src/components/modals/ConfirmationModal.tsx
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

type ConfirmationProps = ModalRegistry['confirmation'];

export function ConfirmationModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'confirmation';
  const props = modalProps as ConfirmationProps;
  
  if (!isOpen) return null;
  
  const {
    title,
    description,
    onConfirm,
    onCancel,
    confirmLabel = 'Confirm',
    cancelLabel = 'Cancel',
    danger = false,
  } = props;
  
  const handleCancel = () => {
    closeModal();
    onCancel?.();
  };
  
  const handleConfirm = () => {
    closeModal();
    onConfirm();
  };
  
  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && handleCancel()}
      variant="confirmation"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>
      
      <UnifiedModalContent>
        <p className="text-muted-foreground">{description}</p>
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

#### Form Modal

```typescript
// src/components/modals/FormModal.tsx
import { useForm } from 'react-hook-form';
import {
  UnifiedModalDialog,
  UnifiedModalHeader,
  UnifiedModalContent,
  UnifiedModalFooter,
} from '@/components/ui/unified-modal-dialog';
import { Button } from '@/components/ui/button';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

type FormProps = ModalRegistry['form'];

export function FormModal() {
  const { openModalId, modalProps, closeModal } = useModalStore();
  const isOpen = openModalId === 'form';
  const props = modalProps as FormProps;
  
  if (!isOpen) return null;
  
  const {
    title,
    description,
    onSubmit,
    onCancel,
    initialData,
    submitLabel = 'Submit',
    cancelLabel = 'Cancel',
  } = props;
  
  const form = useForm({
    defaultValues: initialData || {},
  });
  
  const handleCancel = () => {
    closeModal();
    onCancel?.();
  };
  
  const handleSubmit = form.handleSubmit((data) => {
    closeModal();
    onSubmit(data);
  });
  
  return (
    <UnifiedModalDialog
      open={isOpen}
      onOpenChange={(open) => !open && handleCancel()}
      variant="form"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>
      
      <UnifiedModalContent>
        {description && <p className="text-muted-foreground mb-4">{description}</p>}
        <form id="modal-form" onSubmit={handleSubmit}>
          {/* Form fields would be passed as children or rendered based on a schema */}
          {props.children}
        </form>
      </UnifiedModalContent>
      
      <UnifiedModalFooter>
        <Button variant="outline" onClick={handleCancel}>
          {cancelLabel}
        </Button>
        <Button type="submit" form="modal-form">
          {submitLabel}
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}
```

### 3. Modal Manager Enhancement

Enhance the ModalManager to handle all modal types and support nested modals:

```typescript
// src/components/modals/ModalManager.tsx
import { useModalStore, ModalId } from '@/stores/useModalStore';

// Import all modal components
import { SettingsModal } from './SettingsModal';
import { SystemPromptModal } from './SystemPromptModal';
import { UserPromptModal } from './UserPromptModal';
import { ToolModal } from './ToolModal';
import { ThemeModal } from './ThemeModal';
import { ConfirmationModal } from './ConfirmationModal';
import { FormModal } from './FormModal';

export function ModalManager() {
  // Each modal component handles its own state based on the store
  return (
    <>
      <SettingsModal />
      <SystemPromptModal />
      <UserPromptModal />
      <ToolModal />
      <ThemeModal />
      <ConfirmationModal />
      <FormModal />
      {/* Add other modal components here */}
    </>
  );
}
```

### 4. Modal Hooks for Common Operations

Create hooks to simplify modal operations:

```typescript
// src/hooks/useConfirmationDialog.ts
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

type ConfirmationOptions = Omit<ModalRegistry['confirmation'], 'onConfirm'>;

export function useConfirmationDialog() {
  const { openModal, openNestedModal } = useModalStore();
  
  const confirm = (options: ConfirmationOptions) => {
    return new Promise<boolean>((resolve) => {
      const modalProps = {
        ...options,
        onConfirm: () => resolve(true),
        onCancel: () => resolve(false),
      };
      
      openModal('confirmation', modalProps);
    });
  };
  
  const confirmNested = (options: ConfirmationOptions) => {
    return new Promise<boolean>((resolve) => {
      const modalProps = {
        ...options,
        onConfirm: () => resolve(true),
        onCancel: () => resolve(false),
      };
      
      openNestedModal('confirmation', modalProps);
    });
  };
  
  return { confirm, confirmNested };
}
```

```typescript
// src/hooks/useFormDialog.ts
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

type FormOptions<T> = Omit<ModalRegistry['form'], 'onSubmit'> & {
  children?: React.ReactNode;
};

export function useFormDialog<T>() {
  const { openModal, openNestedModal } = useModalStore();
  
  const openForm = (options: FormOptions<T>) => {
    return new Promise<T | null>((resolve) => {
      const modalProps = {
        ...options,
        onSubmit: (data: T) => resolve(data),
        onCancel: () => resolve(null),
      };
      
      openModal('form', modalProps);
    });
  };
  
  const openNestedForm = (options: FormOptions<T>) => {
    return new Promise<T | null>((resolve) => {
      const modalProps = {
        ...options,
        onSubmit: (data: T) => resolve(data),
        onCancel: () => resolve(null),
      };
      
      openNestedModal('form', modalProps);
    });
  };
  
  return { openForm, openNestedForm };
}
```

### 5. Accessibility Enhancements

Implement focus management and keyboard navigation:

```typescript
// src/components/ui/unified-modal-dialog/UnifiedModalDialog.tsx
// Add to existing component

import { useEffect, useRef } from 'react';
import FocusTrap from 'focus-trap-react';

// Inside the UnifiedModalDialog component
const contentRef = useRef<HTMLDivElement>(null);

// Focus management
useEffect(() => {
  if (open && contentRef.current) {
    // Find the first focusable element
    const focusableElements = contentRef.current.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    
    if (focusableElements.length > 0) {
      (focusableElements[0] as HTMLElement).focus();
    } else {
      contentRef.current.focus();
    }
    
    // Save previously focused element
    const previouslyFocused = document.activeElement as HTMLElement;
    
    return () => {
      // Restore focus when dialog closes
      if (previouslyFocused) {
        previouslyFocused.focus();
      }
    };
  }
}, [open]);

// Wrap content with FocusTrap
return (
  <DialogPrimitive.Root open={open} onOpenChange={handleOpenChange}>
    <DialogPrimitive.Portal>
      <DialogPrimitive.Overlay
        className={/* existing classes */}
      />
      <FocusTrap active={open}>
        <div className={/* existing classes */}>
          <DialogPrimitive.Content
            ref={contentRef}
            tabIndex={-1}
            aria-labelledby={`${id}-title`}
            aria-describedby={ariaDescribedBy || `${id}-description`}
            {/* other existing props */}
          >
            {/* existing content */}
          </DialogPrimitive.Content>
        </div>
      </FocusTrap>
    </DialogPrimitive.Portal>
  </DialogPrimitive.Root>
);
```

## Implementation Plan

### Phase 1: Enhanced Modal Registry

1. Update the useModalStore with TypeScript generics and nested modal support
   - Modify file: `src/stores/useModalStore.ts`
   - Update file: `src/types/ui.ts` (if needed)

2. Create modal hooks for common operations
   - Create file: `src/hooks/useConfirmationDialog.ts`
   - Create file: `src/hooks/useFormDialog.ts`

### Phase 2: Specialized Modal Components

1. Create higher-level modal components
   - Create file: `src/components/modals/ConfirmationModal.tsx`
   - Create file: `src/components/modals/FormModal.tsx`
   - Create file: `src/components/modals/SettingsModal.tsx`
   - Create file: `src/components/modals/SystemPromptModal.tsx`
   - Create file: `src/components/modals/UserPromptModal.tsx`
   - Create file: `src/components/modals/ToolModal.tsx`
   - Create file: `src/components/modals/ThemeModal.tsx`

2. Update the ModalManager to use the new components
   - Update file: `src/components/modals/ModalManager.tsx`

### Phase 3: Accessibility Enhancements

1. Implement focus management in UnifiedModalDialog
   - Update file: `src/components/ui/unified-modal-dialog/UnifiedModalDialog.tsx`

2. Add keyboard navigation support
   - Update relevant modal components

3. Ensure proper ARIA attributes
   - Review and update all modal components

### Phase 4: Migration and Cleanup

1. Migrate remaining legacy modals to use the new system
   - Update any remaining modal implementations

2. Remove deprecated modal implementations
   - Clean up unused code

3. Update documentation
   - Update file: `src/components/ui/unified-modal-dialog/README.md`

## Usage Examples

### Using Confirmation Dialog

```tsx
import { useConfirmationDialog } from '@/hooks/useConfirmationDialog';

function DeleteButton({ itemId, itemName }) {
  const { confirm } = useConfirmationDialog();
  
  const handleDelete = async () => {
    const confirmed = await confirm({
      title: 'Delete Item',
      description: `Are you sure you want to delete ${itemName}?`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      danger: true,
    });
    
    if (confirmed) {
      // Perform delete operation
      await deleteItem(itemId);
    }
  };
  
  return (
    <Button onClick={handleDelete} variant="destructive">
      Delete
    </Button>
  );
}
```

### Using Form Dialog

```tsx
import { useFormDialog } from '@/hooks/useFormDialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface UserFormData {
  name: string;
  email: string;
}

function EditUserButton({ user }) {
  const { openForm } = useFormDialog<UserFormData>();
  
  const handleEdit = async () => {
    const formData = await openForm({
      title: 'Edit User',
      initialData: {
        name: user.name,
        email: user.email,
      },
      submitLabel: 'Save Changes',
      children: (
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Name</Label>
            <Input id="name" name="name" defaultValue={user.name} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input id="email" name="email" type="email" defaultValue={user.email} />
          </div>
        </div>
      ),
    });
    
    if (formData) {
      // Update user with form data
      await updateUser(user.id, formData);
    }
  };
  
  return (
    <Button onClick={handleEdit}>
      Edit User
    </Button>
  );
}
```

### Using Nested Modals

```tsx
import { useModalStore } from '@/stores/useModalStore';
import { useConfirmationDialog } from '@/hooks/useConfirmationDialog';

function SettingsButton() {
  const { openModal } = useModalStore();
  const { confirmNested } = useConfirmationDialog();
  
  const handleSettingsClick = () => {
    openModal('settings', {
      onSave: async (settings) => {
        // Check if settings have changed
        if (settingsHaveChanged(settings)) {
          const confirmed = await confirmNested({
            title: 'Save Changes',
            description: 'You have unsaved changes. Do you want to save them?',
            confirmLabel: 'Save',
            cancelLabel: 'Discard',
          });
          
          if (confirmed) {
            await saveSettings(settings);
          }
        }
      },
    });
  };
  
  return (
    <Button onClick={handleSettingsClick}>
      Settings
    </Button>
  );
}
```

## Accessibility Considerations

1. **Keyboard Navigation**:
   - All modals must be fully navigable using Tab, Shift+Tab, and Escape
   - Focus should be trapped within the modal when open
   - Focus should return to the triggering element when closed

2. **Screen Reader Support**:
   - All modals must have proper ARIA attributes (role, aria-labelledby, aria-describedby)
   - Modal open/close state should be announced to screen readers
   - Error messages should be properly associated with form fields

3. **Visual Considerations**:
   - All text must meet contrast requirements (4.5:1 for normal text, 3:1 for large text)
   - Interactive elements must have visible focus indicators
   - Animations should respect user preferences (prefers-reduced-motion)

## Performance Considerations

1. **Lazy Loading**:
   - Modal content should be loaded only when needed
   - Heavy components should use React.lazy and Suspense

2. **Render Optimization**:
   - Avoid rendering hidden modals
   - Use memo and useMemo for expensive computations

3. **Animation Performance**:
   - Use CSS transitions instead of JavaScript animations when possible
   - Optimize for GPU acceleration (transform, opacity)

## Conclusion

This second phase of dialog unification will build upon the solid foundation of the UnifiedModalDialog component to create a more comprehensive, type-safe, and accessible modal system. By standardizing common patterns, enhancing the modal registry, and improving accessibility, we can provide a better user experience while making it easier for developers to create and maintain modals throughout the application.

The enhanced modal system will support complex workflows, nested modals, and specialized use cases while maintaining consistency and accessibility. This will reduce code duplication, improve maintainability, and ensure a high-quality user experience across the application.