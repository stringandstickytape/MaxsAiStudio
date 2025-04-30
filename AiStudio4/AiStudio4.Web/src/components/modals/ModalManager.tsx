// AiStudio4.Web/src/components/modals/ModalManager.tsx
import React from 'react';

// Import all specialized modal components
import { ModelsModal } from './ModelsModal';
import { ProvidersModal } from './ProvidersModal';
import { AppearanceModal } from './AppearanceModal';
import { SystemPromptModal } from './SystemPromptModal';
import { UserPromptModal } from './UserPromptModal';
import { ToolModal } from './ToolModal';
import { ToolEditorModal } from './ToolEditorModal';
import { ServerModal } from './ServerModal';
import { ThemeModal } from './ThemeModal';
import { ConfirmationModal } from './ConfirmationModal';
import { FormModal } from './FormModal';

export function ModalManager() {
  // Each modal component now internally checks the store
  // and renders itself if its corresponding modal ID is active.
  return (
    <>
      <ModelsModal />
      <ProvidersModal />
      <AppearanceModal />
      <SystemPromptModal />
      <UserPromptModal />
      <ToolModal />
      <ToolEditorModal />
      <ServerModal />
      <ThemeModal />
      <ConfirmationModal />
      <FormModal />
      {/* Add other modal components here as they are created */}
    </>
  );
}