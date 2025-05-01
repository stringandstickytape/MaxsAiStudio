# Global Theme Styles Implementation Guide

## Overview

This document provides detailed instructions for implementing global theme styles across all TSX components in the AiStudio4 project. The goal is to ensure consistent styling by applying global theme properties from the ThemeManager to all components.

## Global Theme Properties to Apply

The following global theme properties should be applied to all TSX components:

- `global-backgroundColor` - Background color for components
- `global-textColor` - Text color for components
- `global-primaryColor` - Primary accent color
- `global-secondaryColor` - Secondary accent color
- `global-borderColor` - Border color for elements
- `global-borderRadius` - Border radius for elements
- `global-fontFamily` - Font family for text
- `global-fontSize` - Base font size
- `global-boxShadow` - Box shadow for elevated elements

## Implementation Pattern

The implementation pattern follows the example found in `ConvView.tsx`, which already applies the `global-backgroundColor` property:

```tsx
style={{
  backgroundColor: 'var(--convview-bg, var(--global-background-color, transparent))',
  ...(window?.theme?.ConvView?.style || {})
}}
```

This pattern uses CSS variables with fallbacks:
1. First tries to use the component-specific variable (e.g., `--convview-bg`)
2. Falls back to the global variable (e.g., `--global-background-color`)
3. Finally falls back to a default value (e.g., `transparent`)

## Implementation Steps

1. **Identify all TSX components** that need styling updates
2. For each component:
   - Locate the root element or styled container
   - Apply the appropriate global styles using the CSS variable pattern
   - Ensure component-specific styles take precedence over global styles
   - Update the `themeableProps` export to include these properties

## Example Implementation

Here's how to update a component:

```tsx
// Before
return (
  <div className="MyComponent">
    {/* Component content */}
  </div>
);

// After
return (
  <div 
    className="MyComponent"
    style={{
      backgroundColor: 'var(--mycomponent-bg, var(--global-background-color, #222))',
      color: 'var(--mycomponent-text-color, var(--global-text-color, #fff))',
      fontFamily: 'var(--mycomponent-font-family, var(--global-font-family, "Segoe UI"))',
      fontSize: 'var(--mycomponent-font-size, var(--global-font-size, 14px))',
      // Add other global styles as needed
      ...(window?.theme?.MyComponent?.style || {})
    }}
  >
    {/* Component content */}
  </div>
);
```

And update the `themeableProps` export:

```tsx
export const themeableProps = {
  backgroundColor: {
    cssVar: '--mycomponent-bg',
    description: 'Background color for the component',
    default: '#222',
  },
  textColor: {
    cssVar: '--mycomponent-text-color',
    description: 'Text color for the component',
    default: '#fff',
  },
  // Add other properties
};
```

## Special Considerations

1. **Nested components**: Some components may have nested elements that need styling. Apply global styles to appropriate containers.
2. **Dynamic styling**: For components with dynamic styling, ensure global styles are properly integrated with the dynamic logic.
3. **Shadcn components**: When using Shadcn UI components, you may need to use their styling API rather than direct style props.

## Implementation Checklist

Track progress by checking off components as they are updated:

- [ ] AppHeader.tsx
- [ ] AttachmentPreview.tsx
- [ ] ChatContainer.tsx
- [ ] ChatSpace.tsx
- [ ] CommandBar.tsx
- [ ] CommandInitializer.tsx
- [ ] GenericForm.tsx
- [ ] ConvTreeView.tsx
- [ ] ConvTreeView/ConvTreeView.tsx
- [ ] ConvTreeView/EmptyTreeView.tsx
- [ ] ConvTreeView/TreeControls.tsx
- [ ] ConvView.tsx (already implemented)
- [ ] ConvView/ConversationControls.tsx
- [ ] ConvView/MessageActions.tsx
- [ ] ConvView/MessageEditor.tsx
- [ ] ConvView/MessageItem.tsx
- [ ] ConvView/MessageMetadata.tsx
- [ ] ConvView/ScrollManager.tsx
- [ ] ConvView/StreamingMessage.tsx
- [ ] DragDropZone.tsx
- [ ] FileAttachment.tsx
- [ ] FontSizeProvider.tsx
- [ ] HistoricalConvTree.tsx
- [ ] HistoricalConvTreeList.tsx
- [ ] IconSelector.tsx
- [ ] InputBar.tsx
- [ ] InputBar/ActionButtons.tsx
- [ ] InputBar/AttachmentSection.tsx
- [ ] InputBar/MessageInputArea.tsx
- [ ] InputBar/ModelStatusSection.tsx
- [ ] InputBar/StatusSection.tsx
- [ ] InputBar/SystemPromptSection.tsx
- [ ] InputBar/ToolsSection.tsx
- [ ] LiveStreamToken.tsx
- [ ] LoadingTimer.tsx
- [ ] MarkdownPane.tsx
- [ ] MessageAttachments.tsx
- [ ] ModalManager.tsx
- [ ] ModelStatusBar.tsx
- [ ] NavigationContainer.tsx
- [ ] PanelContainerLayout.tsx
- [ ] PanelManager.tsx
- [ ] PinnedShortcutButton.tsx
- [ ] PinnedShortcutDropdown.tsx
- [ ] PinnedShortcutRow.tsx
- [ ] PinnedShortcuts.tsx
- [ ] RenameShortcutDialog.tsx
- [ ] ServerDialog.tsx
- [ ] Sidebar.tsx
- [ ] StatusMessage.tsx
- [ ] SystemPromptCard.tsx
- [ ] SystemPromptComponent.tsx
- [ ] SystemPromptEditor.tsx
- [ ] SystemPromptLibrary.tsx
- [ ] ToolDialog.tsx
- [ ] ToolEditor.tsx
- [ ] ToolPanel.tsx
- [ ] ToolSelector.tsx

## Implementation Notes

As you implement these changes, add notes here about any challenges, patterns, or insights discovered:

- 

## Testing

After implementing these changes:

1. Test with different themes to ensure styles are properly applied
2. Verify that component-specific styles still override global styles when specified
3. Check for any styling regressions or inconsistencies

## Completion

Once all components have been updated, perform a final review to ensure consistency across the application.