// AiStudioClient/src/components/modals/FormModal.tsx
import React from 'react';
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
  const props = isOpen ? (modalProps as FormProps) : null;

  // Use a unique key based on modal open state to reset form when modal reopens
  const formKey = isOpen ? `form-${Date.now()}` : 'form-closed';

  const form = useForm({
    // Reset form values when the modal opens with new initialData
    values: props?.initialData || {},
  });

  React.useEffect(() => {
    if (isOpen && props?.initialData) {
      form.reset(props.initialData);
    }
    // Reset form when modal closes or initialData changes
  }, [isOpen, props?.initialData, form.reset]);

  if (!isOpen || !props) return null;

  const {
    title,
    description,
    onSubmit,
    onCancel,
    // initialData is handled by useForm
    submitLabel = 'Submit',
    cancelLabel = 'Cancel',
    children, // Expect children to contain form fields
  } = props;

  const handleCancel = () => {
    // closeModal(); // Let onOpenChange handle closing
    onCancel?.();
  };

  const handleSubmit = form.handleSubmit((data) => {
    // closeModal(); // Let onOpenChange handle closing
    onSubmit(data);
  });

  return (
    <UnifiedModalDialog
      key={formKey} // Ensure form resets properly
      open={isOpen}
      // Use closeModal which handles nested logic correctly
      onOpenChange={(open) => !open && closeModal()}
      variant="form"
    >
      <UnifiedModalHeader>
        <h2 className="text-xl font-semibold">{title}</h2>
      </UnifiedModalHeader>

      <UnifiedModalContent>
        {description && <p className="text-muted-foreground mb-4">{description}</p>}
        {/* Form fields are passed as children */} 
        {/* Wrap children in form tag to associate submit button */} 
        <form id="modal-form" onSubmit={handleSubmit}>
          {children}
        </form>
      </UnifiedModalContent>

      <UnifiedModalFooter>
        <Button variant="outline" onClick={handleCancel}>
          {cancelLabel}
        </Button>
        {/* Trigger form submission via the form id */} 
        <Button type="submit" form="modal-form">
          {submitLabel}
        </Button>
      </UnifiedModalFooter>
    </UnifiedModalDialog>
  );
}