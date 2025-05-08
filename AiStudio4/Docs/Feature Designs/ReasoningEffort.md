# Feature Design: ReasoningEffort Property for Model

## Overview

This feature adds a new property called `ReasoningEffort` to the `Model` class. This property indicates the desired level of reasoning effort for the model and can take one of four values: `none`, `low`, `medium`, or `high`. If set to `none`, the property is ignored by the backend; otherwise, it is used to set the `ReasoningEffortLevel` in `ChatCompletionOptions` in `NetOpenAi.cs`.

## Implementation Details

### Backend Changes

#### Model.cs
- Add a new property `ReasoningEffort` (string or enum, default 'none')
- Ensure proper serialization/deserialization

#### NetOpenAi.cs
- When constructing `ChatCompletionOptions`, if `ReasoningEffort` is not 'none', set `ReasoningEffortLevel` accordingly
- If `ReasoningEffort` is 'none', omit the property from `ChatCompletionOptions`
- Map string values ('low', 'medium', 'high') to the corresponding `ChatReasoningEffortLevel` enum values

### Frontend Changes

#### types/settings.ts
- Update the Model interface to include the new property

#### ModelForm.tsx
- Add a select/dropdown field for ReasoningEffort with options: none, low, medium, high
- Ensure the value is included in form submission and defaults to 'none'

## User Experience

In the model library editor, users will see a new dropdown labeled "Reasoning Effort" with options: None, Low, Medium, High. Selecting a value other than None will cause the backend to set the corresponding reasoning effort level for the model.

## Implementation Plan

1. Add the property to the Model class in C#
2. Update the Model interface in TypeScript
3. Add the UI element to the model form
4. Update NetOpenAi.cs to use the property
5. Test the feature end-to-end

## Testing

- Verify the property can be set and saved correctly
- Verify the property persists across application restarts
- Verify the UI correctly displays the current value of the property
- Verify the property defaults to 'none' for new models
- Verify that NetOpenAi.cs sets ReasoningEffortLevel only when appropriate

## Future Considerations

- The property could be surfaced elsewhere in the UI for filtering or display
- Additional reasoning effort levels could be added if supported by the backend