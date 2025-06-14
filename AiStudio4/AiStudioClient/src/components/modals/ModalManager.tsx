// AiStudioClient/src/components/modals/ModalManager.tsx
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
import { InfoModal } from './InfoModal';
import { ModelFormModal } from './ModelFormModal';
import { ProviderFormModal } from './ProviderFormModal';
import { ThemeFormModal } from './ThemeFormModal';

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
      <InfoModal />
      <ModelFormModal />
      <ProviderFormModal />
      <ThemeFormModal />
      {/* Add other modal components here as they are created */}
    </>
  );
}