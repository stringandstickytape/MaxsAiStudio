# Model GUID Implementation

## Overview

Currently, the application identifies models by their names, which can lead to ambiguity when two different models from different providers have the same name but different parameters. This feature design outlines the changes needed to use model GUIDs instead of model names for identification throughout the application.

## Current Implementation

### Client Side
- The client receives model names from `/api/getConfig` endpoint
- Uses these names in `useChatConfigResource` hook
- Stores selected models by name in `useModelStore`
- Sends model names when making API calls

### Server Side
- `GeneralSettings` class stores `DefaultModel` and `SecondaryModel` as string names
- `GeneralSettingsService` has methods to update these model names
- `ConfigRequestHandler` returns model names to the client
- `ModelRequestHandler` handles setting default and secondary models by name

### Model Class
- Each model already has a `Guid` property
- The `Model` class in `SharedClasses` shows this is already implemented

## Required Changes

### 1. Server-Side Changes

#### GeneralSettings Class
- Rename `DefaultModel` to `DefaultModelGuid`
- Rename `SecondaryModel` to `SecondaryModelGuid`
- Add migration code to handle existing settings

#### GeneralSettingsService
- Update `UpdateDefaultModel` to accept and store GUIDs
- Update `UpdateSecondaryModel` to accept and store GUIDs
- Add helper methods to get model names from GUIDs if needed

#### ConfigRequestHandler
- Modify `HandleGetConfigRequest` to return both model GUIDs and names
- Update response format to include model objects with both properties

#### ModelRequestHandler
- Update `SetModel` method to accept model GUIDs
- Ensure all model-related operations use GUIDs for identification

### 2. Client-Side Changes

#### Resource Hooks
- Update `useChatConfigResource` to handle model objects with GUIDs
- Modify transformation functions to extract GUIDs

#### Model Store
- Change `selectedPrimaryModel` and `selectedSecondaryModel` to store GUIDs
- Add helper functions to get model names from GUIDs

#### API Calls
- Update all API calls to send model GUIDs instead of names
- Ensure backward compatibility during transition

#### UI Components
- Update components to display model names but use GUIDs internally
- Modify model selection logic to work with GUIDs

### 3. Migration Strategy

- Add code to migrate existing settings that use names to GUIDs
- During first load, match model names to GUIDs and update settings
- Provide fallback mechanism if a model name can't be matched to a GUID
- Log migration results for debugging

## Implementation Plan

### Phase 1: Server-Side Changes
1. Update `GeneralSettings` class to use GUIDs
2. Modify `GeneralSettingsService` methods
3. Update request handlers to work with GUIDs
4. Add migration code for existing settings

### Phase 2: Client-Side Changes
1. Update resource hooks and stores
2. Modify API call functions
3. Update UI components
4. Add helper functions for name/GUID conversion

### Phase 3: Testing and Validation
1. Test migration of existing settings
2. Verify model selection works correctly
3. Ensure chat functionality works with GUIDs
4. Validate backward compatibility

## Potential Issues and Mitigations

### Name Collisions
- **Issue**: Multiple models with the same name but different GUIDs
- **Mitigation**: During migration, if multiple models have the same name, prefer starred models or most recently used

### Missing Models
- **Issue**: Stored model name no longer exists in the model list
- **Mitigation**: Provide a fallback to a default model and log the issue

### Backward Compatibility
- **Issue**: Older clients expecting model names
- **Mitigation**: Temporarily support both name and GUID in API responses

## Conclusion

Implementing model identification by GUID instead of name will improve the robustness of the application by eliminating potential ambiguity. The migration strategy ensures a smooth transition for existing users, and the implementation plan provides a clear path forward for development.