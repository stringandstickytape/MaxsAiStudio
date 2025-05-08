# Tool Extra Properties UI Feature Design

## Overview

This feature will provide a user interface for viewing and editing the extra properties of built-in tools in AiStudio4. Currently, these properties are stored in `%APPDATA%/AiStudio4/builtinToolExtraProps.json` and managed by the `IBuiltInToolExtraPropertiesService`, but there is no UI for users to modify them directly.

## Goals

- Allow users to view all available extra properties for each built-in tool
- Provide an intuitive interface for editing these properties
- Support different property types (strings, CSV lists, booleans, etc.)
- Persist changes using the existing `IBuiltInToolExtraPropertiesService`
- Provide immediate feedback when properties are changed

## User Experience

1. Users access the Tool Properties UI from a dedicated menu item or button in the main interface
2. A dialog or panel displays a list of all built-in tools
3. Selecting a tool shows its available extra properties with current values
4. Properties are presented with appropriate input controls based on their type:
   - Text fields for simple strings
   - Multi-select or tag inputs for CSV lists
   - Checkboxes for boolean values
5. Changes are saved automatically or via a save button
6. A reset option allows reverting to default values

## Technical Design

### Frontend Components

- **ToolPropertiesDialog.tsx**: Main dialog component for the feature
- **ToolSelector.tsx**: Component for selecting which tool to configure
- **PropertyEditor.tsx**: Generic component for editing different property types
- **PropertyTypeDetector.ts**: Utility to determine appropriate editor for each property

### State Management

- Add a new Zustand store: `useToolPropertiesStore`
- Store will maintain:
  - List of tools and their properties
  - Current property values
  - Editing state
  - Validation status

### Backend Integration

- Extend `WebSocketMessageHandler.cs` to handle tool property requests/updates
- Add new message types:
  - `GET_TOOL_PROPERTIES`: Request all tool properties
  - `UPDATE_TOOL_PROPERTY`: Update a specific property
  - `RESET_TOOL_PROPERTIES`: Reset properties to defaults

### Data Flow

1. On UI initialization, frontend requests tool properties via WebSocket
2. Backend retrieves properties from `IBuiltInToolExtraPropertiesService`
3. Frontend displays properties in the UI
4. When user makes changes, updates are sent to backend
5. Backend persists changes and broadcasts updates to all connected clients

## Implementation Plan

### Phase 1: Backend Preparation

1. Extend WebSocket message handlers for tool property operations
2. Add methods to expose tool property metadata (descriptions, types, defaults)
3. Implement validation logic for property values

### Phase 2: Frontend Implementation

1. Create the basic UI components
2. Implement the Zustand store
3. Connect UI to WebSocket for data retrieval/updates

### Phase 3: Refinement

1. Add property type detection and specialized editors
2. Implement validation feedback
3. Add help text and tooltips
4. Polish the UI design

## Future Enhancements

- Property grouping by category
- Search/filter for properties
- Ability to export/import property configurations
- Presets for common configurations
- Visual indicators for non-default property values

## Dependencies

- Existing `IBuiltInToolExtraPropertiesService`
- WebSocket notification system
- Frontend component library (likely shadcn/ui based on project structure)