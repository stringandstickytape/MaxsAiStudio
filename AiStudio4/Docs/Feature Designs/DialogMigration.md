# Dialog Migration Phase: Completing the Unification

## Overview

This document outlines the plan for completing the migration of all existing dialogs to the new unified dialog system. Following the successful implementation of the Dialog Unification Phase 2, we now need to systematically identify and convert all remaining legacy dialog implementations to use the new components and patterns.

## Current State

The Dialog Unification Phase 2 has delivered:

1. **Enhanced Modal Store**: Type-safe registry with support for nested modals
2. **Specialized Modal Components**: ConfirmationModal, FormModal, and various content-specific modals
3. **Modal Hooks**: useConfirmationDialog and useFormDialog for simplified usage
4. **Updated Modal Manager**: Centralized management of all modal types
5. **Accessibility Improvements**: Proper keyboard navigation and screen reader support

Some dialogs have already been migrated:
- AppearanceTab now uses useConfirmationDialog instead of AlertDialog
- SystemPromptCard now uses useConfirmationDialog instead of AlertDialog
- UserPromptCard now uses useConfirmationDialog instead of AlertDialog

## Remaining Legacy Dialogs

Based on initial analysis, the following components still use legacy dialog patterns:

1. **AlertDialog Usage**: Various components still using the direct AlertDialog component
2. **Custom Modal Implementations**: Components with their own modal state management
3. **Tool Dialog**: Currently using its own state management
4. **Server Dialog**: Currently using its own state management

## Migration Strategy

### 1. Inventory and Classification

First, we'll conduct a comprehensive inventory of all remaining dialog implementations:

```typescript
// Example inventory structure
interface DialogInventoryItem {
  component: string;          // Component name
  path: string;              // File path
  dialogType: DialogType;    // AlertDialog, Custom, etc.
  complexity: 'Low' | 'Medium' | 'High';
  dependencies: string[];    // Other components it depends on
  migrationPath: MigrationType; // How it should be migrated
}

enum DialogType {
  AlertDialog,
  CustomModal,
  RadixDialog,
  Other
}

enum MigrationType {
  UseConfirmationDialog,
  UseFormDialog,
  SpecializedModal,
  Custom
}
```

### 2. Prioritization

We'll prioritize migrations based on:

1. **Usage Frequency**: Dialogs used frequently should be migrated first
2. **Complexity**: Start with simpler dialogs to build momentum
3. **Dependencies**: Migrate independent dialogs before dependent ones
4. **User Impact**: Prioritize dialogs in critical user flows

### 3. Implementation Approach

For each dialog type, we'll follow a specific migration pattern:

#### AlertDialog → useConfirmationDialog

```typescript
// Before
<AlertDialog open={isOpen} onOpenChange={setIsOpen}>
  <AlertDialogContent>
    <AlertDialogHeader>
      <AlertDialogTitle>Are you sure?</AlertDialogTitle>
      <AlertDialogDescription>This action cannot be undone.</AlertDialogDescription>
    </AlertDialogHeader>
    <AlertDialogFooter>
      <AlertDialogCancel>Cancel</AlertDialogCancel>
      <AlertDialogAction onClick={handleConfirm}>Continue</AlertDialogAction>
    </AlertDialogFooter>
  </AlertDialogContent>
</AlertDialog>

// After
const { confirm } = useConfirmationDialog();

const handleAction = async () => {
  const confirmed = await confirm({
    title: 'Are you sure?',
    description: 'This action cannot be undone.',
    confirmLabel: 'Continue',
    cancelLabel: 'Cancel'
  });
  
  if (confirmed) {
    // Handle confirmation
  }
};
```

#### Custom Form → useFormDialog

```typescript
// Before
const [isOpen, setIsOpen] = useState(false);
const [formData, setFormData] = useState(initialData);

// Form handling logic...

<Dialog open={isOpen} onOpenChange={setIsOpen}>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>Edit Item</DialogTitle>
    </DialogHeader>
    <form onSubmit={handleSubmit}>
      {/* Form fields */}
      <DialogFooter>
        <Button type="button" variant="outline" onClick={() => setIsOpen(false)}>Cancel</Button>
        <Button type="submit">Save</Button>
      </DialogFooter>
    </form>
  </DialogContent>
</Dialog>

// After
const { openForm } = useFormDialog<FormData>();

const handleEdit = async () => {
  const result = await openForm({
    title: 'Edit Item',
    initialData: item,
    submitLabel: 'Save',
    cancelLabel: 'Cancel',
    children: (
      <>
        {/* Form fields */}
      </>
    )
  });
  
  if (result) {
    // Handle form submission
  }
};
```

#### Complex Dialogs → Specialized Modal Components

For more complex dialogs like Tool Dialog and Server Dialog:

1. Create specialized modal components (ToolModal, ServerModal)
2. Register them in the ModalRegistry
3. Update the ModalManager to include them
4. Replace direct usage with useModalStore.openModal calls

### 4. Testing Strategy

For each migration:

1. **Unit Tests**: Verify the component renders correctly
2. **Integration Tests**: Ensure the dialog opens/closes properly
3. **Functional Tests**: Confirm all actions work as expected
4. **Accessibility Tests**: Verify keyboard navigation and screen reader support

## Implementation Plan

### Phase 1: Inventory and Analysis (1-2 days)

1. Scan the codebase for all dialog implementations
2. Classify each dialog according to the inventory structure
3. Create a prioritized migration list

### Phase 2: AlertDialog Migrations (2-3 days)

1. Identify all AlertDialog usages
2. Convert each to useConfirmationDialog
3. Test each conversion thoroughly

### Phase 3: Form Dialog Migrations (3-4 days)

1. Identify all custom form dialogs
2. Convert each to useFormDialog
3. Test each conversion thoroughly

### Phase 4: Complex Dialog Migrations (4-5 days)

1. Create specialized modal components for Tool Dialog and Server Dialog
2. Update the ModalRegistry and ModalManager
3. Replace direct usage with useModalStore.openModal calls
4. Test each conversion thoroughly

### Phase 5: Cleanup and Documentation (1-2 days)

1. Remove any unused dialog components
2. Update documentation with examples
3. Create a dialog usage guide for developers

## Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Breaking changes to dialog behavior | High | Medium | Thorough testing of each migration |
| Performance regression | Medium | Low | Performance testing before/after |
| Increased bundle size | Low | Low | Code splitting for modal components |
| Accessibility regressions | High | Low | Dedicated accessibility testing |

## Success Criteria

1. All dialogs use the unified system
2. No regressions in functionality
3. Improved accessibility across all dialogs
4. Simplified dialog implementation for developers
5. Consistent user experience across the application

## Future Enhancements

After completing the migration, we can consider:

1. **Wizard/Multi-step Modal**: For complex workflows
2. **Animation Improvements**: More sophisticated transitions
3. **Mobile Optimizations**: Better touch support and responsive layouts
4. **Analytics Integration**: Track dialog usage and conversion rates

## Conclusion

This migration will complete the dialog unification effort, resulting in a more consistent, accessible, and maintainable dialog system. By systematically converting all legacy implementations to the new pattern, we'll improve both the developer and user experience while setting the foundation for future enhancements.