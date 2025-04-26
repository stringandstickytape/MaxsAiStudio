// AiStudio4/Docs/Feature Designs/UserPromptAssociationWithSystemPrompt.md

# Feature Design: Associate User Prompts with System Prompts

## Overview
This feature enables users to associate a user prompt with a system prompt in the editor. When a system prompt is activated, any associated user prompt is also activated. The association is persisted on the server as part of the system prompt definition, surviving reloads and edits.

---

## 1. Data Model Changes

### System Prompt Definition (Backend)
- **Add field:** `associatedUserPromptId: string`
  - The ID of the associated user prompt (if any).
- **Example:**
  ```json
  {
    "id": "sys-prompt-123",
    "name": "Summarize & Analyze",
    "promptText": "...",
    "associatedTools": ["Summarizer", "Analyzer"],
    "associatedUserPromptId": "user-prompt-456"
  }
  ```

---

## 2. API Changes

### System Prompt Service
- **Add methods:**
  - `SetAssociatedUserPromptAsync(string systemPromptId, string userPromptId)`
  - `ClearAssociatedUserPromptAsync(string systemPromptId)`
  - `GetAssociatedUserPromptAsync(string systemPromptId)`

### System Prompt CRUD Endpoints
- **GET /system-prompts**
  - Response includes `associatedUserPromptId` for each prompt.
- **POST /system-prompts** and **PUT /system-prompts/{id}**
  - Accepts `associatedUserPromptId` in request body.
- **New API endpoints:**
  - **POST /api/setAssociatedUserPrompt**
    - Sets the associated user prompt for a system prompt.
  - **POST /api/clearAssociatedUserPrompt**
    - Clears the associated user prompt for a system prompt.
  - **POST /api/getAssociatedUserPrompt**
    - Gets the associated user prompt for a system prompt.

---

## 3. Frontend Changes

### System Prompt Editor UI
- **User Prompt Selector:**
  - Add a dropdown to select a user prompt to associate with the system prompt.
  - Fetch available user prompts from the backend.
  - Display current association when editing an existing prompt.
- **Persistence:**
  - On save, send the updated `associatedUserPromptId` to the backend.

### System Prompt Component
- When a system prompt is activated, check if it has an associated user prompt.
- If it does, also activate that user prompt by inserting it into the input field.

### Example UI Flow
1. User opens or creates a system prompt in the editor.
2. User sees a dropdown of available user prompts and selects one to associate.
3. User saves the prompt; the association is persisted.
4. When the user activates the system prompt, the associated user prompt is automatically inserted into the input field.

---

## 4. Activation Logic

- When a system prompt is activated (by user action or workflow):
  - The frontend retrieves the `associatedUserPromptId` for the prompt.
  - If an associated user prompt exists, it is activated by inserting it into the input field.
  - This happens after any associated tools are activated.

---

## 5. Implementation Notes

- **Extensibility:**
  - The design supports future expansion (e.g., multiple user prompts per system prompt).
- **Validation:**
  - Backend should validate that user prompt IDs in `associatedUserPromptId` are valid.
- **UI/UX:**
  - Consider showing a preview of the associated user prompt in the system prompt editor.
- **Testing:**
  - Test prompt creation, editing, activation, and persistence of user prompt associations.

---

## 6. Open Questions
- Should the user be able to override the associated user prompt at activation time?
- Should there be a visual indication that a system prompt has an associated user prompt?

---

## 7. Summary
This feature provides a seamless way to associate user prompts with system prompts, enhancing the user experience by automatically activating related prompts together.