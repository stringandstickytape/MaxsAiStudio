// AiStudioClient/src/stores/modalHelpers.ts
import { useModalStore, ModalRegistry, ModalId } from './useModalStore';

/**
 * Type-safe helper to get current modal props with proper typing
 * @returns The current modal's props with proper type inference, or null if no modal is open
 */
export function useTypedModalProps<T extends ModalId>(
  expectedModalId: T
): ModalRegistry[T] | null {
  const { currentModal } = useModalStore();
  
  if (currentModal?.id === expectedModalId) {
    return currentModal.props as ModalRegistry[T];
  }
  
  return null;
}

/**
 * Type-safe helper to check if a specific modal is open
 * @param modalId The modal ID to check
 * @returns True if the specified modal is currently open
 */
export function useIsModalOpen(modalId: ModalId): boolean {
  const { currentModal } = useModalStore();
  return currentModal?.id === modalId;
}

/**
 * Type-safe helper to get current modal info
 * @returns Object with modal ID and properly typed props, or null if no modal is open
 */
export function useCurrentModal() {
  const { currentModal } = useModalStore();
  return currentModal;
}

/**
 * Example usage patterns for migrating from legacy modalProps:
 * 
 * // Legacy approach (still works but not type-safe):
 * const { openModalId, modalProps } = useModalStore();
 * const isOpen = openModalId === 'confirmation';
 * const props = isOpen ? (modalProps as ConfirmationProps) : null;
 * 
 * // New type-safe approach:
 * const confirmationProps = useTypedModalProps('confirmation');
 * const isConfirmationOpen = useIsModalOpen('confirmation');
 * 
 * // Or using the current modal directly:
 * const currentModal = useCurrentModal();
 * if (currentModal?.id === 'confirmation') {
 *   // TypeScript knows currentModal.props is ConfirmationProps here
 *   const { title, description, onConfirm } = currentModal.props;
 * }
 */