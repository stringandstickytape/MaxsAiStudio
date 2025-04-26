// AiStudio4/Docs/Feature Designs/ToolAssociationWithSystemPrompt.md

# Feature Design: Associate Tools with System Prompts

## Overview
This feature enables users to associate one or more available tools with a system prompt in the editor. When a system prompt is activated, the associated tools are also activated. The association is persisted on the server as part of the system prompt definition, surviving reloads and edits.

---

## 1. Data Model Changes

### System Prompt Definition (Backend)
- **Add field:** `associatedTools: string[]`
  - An array of tool IDs or names (depending on how tools are uniquely identified in the system).
- **Example:**
  ```json
  {
    "id": "sys-prompt-123",
    "name": "Summarize & Analyze",
    "promptText": "...",
    "associatedTools": ["Summarizer", "Analyzer"]
  }
  ```

---

## 2. API Changes

### System Prompt CRUD Endpoints
- **GET /system-prompts**
  - Response includes `associatedTools` for each prompt.
- **POST /system-prompts** and **PUT /system-prompts/{id}**
  - Accepts `associatedTools` array in request body.
- **Migration:**
  - Existing prompts default to `associatedTools: []` if not present.

---

## 3. Frontend Changes

### Prompt Editor UI
- **Tool Selector:**
  - Add a multi-select dropdown or checklist for available tools in the system prompt editor.
  - Fetch available tools from the backend or a static list.
  - Display current associations when editing an existing prompt.
- **Persistence:**
  - On save, send the updated `associatedTools` array to the backend.

### Example UI Flow
1. User opens or creates a system prompt in the editor.
2. User sees a list of available tools and selects one or more to associate.
3. User saves the prompt; the association is persisted.
4. On reload, the editor shows the correct tool associations.

---

## 4. Activation Logic

- When a system prompt is activated (by user action or workflow):
  - The backend retrieves the `associatedTools` array for the prompt.
  - Each tool in the array is activated (e.g., loaded, enabled, or started as appropriate for the tool type).
  - Activation order and error handling should be defined (e.g., fail fast, continue on error, etc.).

---

## 5. Implementation Notes

- **Extensibility:**
  - The design supports future expansion (e.g., tool configuration per prompt).
- **Validation:**
  - Backend should validate that tool IDs/names in `associatedTools` are valid.
- **UI/UX:**
  - Consider tool descriptions or icons for better usability.
- **Testing:**
  - Test prompt creation, editing, activation, and persistence of tool associations.

---

## 6. Open Questions
- How are tools uniquely identified (ID, name, etc.)?
- Should tool activation be synchronous or asynchronous?
- Are there tool-specific configuration options per prompt?

---

## 7. Summary
This feature provides a robust, extensible way to associate tools with system prompts, ensuring seamless activation and persistence across the application lifecycle.