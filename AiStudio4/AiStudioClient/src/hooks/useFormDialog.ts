// AiStudioClient/src/hooks/useFormDialog.ts
import React from 'react';
import { useModalStore, ModalRegistry } from '@/stores/useModalStore';

// Define FormOptions more specifically, expecting children for form fields
type FormOptions<TFormData> = Omit<ModalRegistry['form'], 'onSubmit' | 'onCancel' | 'children'> & {
  children: React.ReactNode; // Make children mandatory for the hook usage
  initialData?: TFormData;
};

export function useFormDialog<TFormData>() {
  const { openModal, openNestedModal, closeModal } = useModalStore();

  const openForm = (options: FormOptions<TFormData>) => {
    return new Promise<TFormData | null>((resolve) => {
      const modalProps: ModalRegistry['form'] = {
        ...options,
        onSubmit: (data: TFormData) => {
          // closeModal(); // Let the modal handle its closing
          resolve(data);
        },
        onCancel: () => {
          // closeModal(); // Let the modal handle its closing
          resolve(null);
        },
        children: options.children, // Pass children through
        initialData: options.initialData, // Pass initialData
      };
      openModal('form', modalProps);
    });
  };

  const openNestedForm = (options: FormOptions<TFormData>) => {
    return new Promise<TFormData | null>((resolve) => {
      const modalProps: ModalRegistry['form'] = {
        ...options,
        onSubmit: (data: TFormData) => {
          // closeModal(); // Let the modal handle its closing
          resolve(data);
        },
        onCancel: () => {
          // closeModal(); // Let the modal handle its closing
          resolve(null);
        },
        children: options.children,
        initialData: options.initialData,
      };
      openNestedModal('form', modalProps);
    });
  };

  return { openForm, openNestedForm };
}