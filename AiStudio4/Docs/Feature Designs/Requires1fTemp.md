# Feature Design: Requires1fTemp Property for Model

## Overview

This feature adds a new boolean property called `Requires1fTemp` to the `Model` class. This property will indicate whether a model requires the use of 1f temperature setting. The property will default to false and will be exposed in the front-end UI as a checkbox in the model library editor.

## Implementation Details

### Backend Changes

#### Model.cs
- Add a new boolean property `Requires1fTemp` with a default value of false
- Ensure it's properly serialized/deserialized with JSON

### Frontend Changes

#### Types/settings.ts
- Update the Model interface to include the new property

#### ModelForm.tsx
- Add a new checkbox field for the Requires1fTemp property
- Include appropriate label and description

#### Default Values
- Ensure the property defaults to false in all relevant places

## User Experience

In the model library editor, users will see a new checkbox labeled "Requires 1.0 Temperature" with a description explaining that this setting indicates the model requires a temperature setting of 1.0 to function properly. When checked, this information will be stored with the model and can be used by the application to automatically set the appropriate temperature when using this model.

## Implementation Plan

1. Add the property to the Model class in C#
2. Update the Model interface in TypeScript
3. Add the UI element to the model form
4. Test the feature end-to-end

## Testing

- Verify the property can be set and saved correctly
- Verify the property persists across application restarts
- Verify the UI correctly displays the current value of the property
- Verify the property defaults to false for new models

## Future Considerations

In the future, this property could be used to:
- Automatically set the temperature to 1.0 when using models with this flag
- Display warnings if users attempt to use a different temperature with these models
- Filter or sort models based on this property