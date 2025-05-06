# Custom Property Removal Feature Design

## Overview
This feature provides a UI component listing all TSX files that export `themeableProps`. Each file will be displayed with its full path and an empty checkbox next to it. The user can select which themeable properties to remove from the theme.

## Purpose
- Allow users to selectively remove custom themeable properties from components.
- Provide a clear overview of all components with themeable properties.

## List of TSX Files with `themeableProps` Export

- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/AppHeader.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/HistoricalConvTreeList.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/MarkdownPane.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/PinnedShortcuts.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/Sidebar.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/SlashDropdown.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/common/GenericForm.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ConvTreeView/ConvTreeView.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ConvView/ConvView.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/InputBar/InputBar.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/modals/ServerModal.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/modals/ToolEditorModal.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/navigation/NavigationContainer.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/servers/ServerForm.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/SystemPrompt/SystemPromptComponent.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/tools/ToolEditor.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ui/switch.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalContent.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalDialog.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalFooter.tsx
- [ ] C:/Users/maxhe/source/repos/MaxsAiStudio/AiStudio4/AiStudioClient/src/components/ui/unified-modal-dialog/UnifiedModalHeader.tsx

## Next Steps
- Implement the UI component to render this list with checkboxes.
- Add functionality to remove selected themeable properties.
- Integrate with the existing theming system.

---

This design document serves as a starting point for the custom property removal feature.