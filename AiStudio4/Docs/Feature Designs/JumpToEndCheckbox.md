# Feature Design: Jump to End Checkbox

**Goal:** Replace the existing `StickToBottom` component and its behavior with a user-controlled "Jump to End" checkbox located in the input bar.

**Rationale:** The current automatic behavior of `StickToBottom` is often unsatisfactory, especially during rapid streaming or when the user wants to review previous messages. A manual checkbox provides explicit control over whether the view should automatically scroll to the latest message upon receiving new content.

**Implementation Details:**

1.  **Dependency Removal:**
    *   Remove the `use-stick-to-bottom` package from `package.json` and `pnpm-lock.yaml`.
    *   Run `pnpm install` to update dependencies.

2.  **Zustand Store:**
    *   Create a new zustand store: `AiStudio4/AiStudio4.Web/src/stores/useJumpToEndStore.ts`.
    *   Store State:
        *   `jumpToEndEnabled`: boolean (default: `true`)
    *   Store Actions:
        *   `setJumpToEndEnabled(enabled: boolean)`: Updates the state.

3.  **ConvView Component (`AiStudio4/AiStudio4.Web/src/components/ConvView.tsx`):**
    *   Remove all imports and usage of `StickToBottom`, `useStickToBottomContext`, and the `ScrollToBottom` button component.
    *   The main container (previously `StickToBottom`) should become a standard `div` with `overflow-y: auto` and a `ref` (e.g., `scrollContainerRef`).
    *   **Scrolling Logic:**
        *   Implement a function `scrollToBottom()` that uses `scrollContainerRef.current.scrollTop = scrollContainerRef.current.scrollHeight;`.
        *   Add an effect hook that listens for a custom event (e.g., `'jump-to-end'`) dispatched when a new token arrives and `jumpToEndEnabled` is true. Call `scrollToBottom()` when this event is received.
        *   Add scroll event listeners (`onScroll`, potentially debounced) to the `scrollContainerRef` div.
        *   Inside the scroll event handler, detect if the scroll was initiated by the user (not programmatically by `scrollToBottom`). This can be tricky; one approach is to set a flag before programmatic scrolling and check it in the handler. Another is to compare `scrollTop` before and after a potential programmatic scroll.
        *   If the scroll is determined to be manual (user wheel or scrollbar interaction), call `useJumpToEndStore.getState().setJumpToEndEnabled(false)`.

4.  **InputBar Component (`AiStudio4/AiStudio4.Web/src/components/InputBar.tsx`):**
    *   Import `useJumpToEndStore`.
    *   Add a `Checkbox` component (from `shadcn/ui`) and a `Label`.
    *   Position the checkbox and label to the right of the `SystemPromptComponent`, likely within the same flex container.
    *   Checkbox Label: "Jump to End"
    *   Checkbox `checked` state should be bound to `useJumpToEndStore(state => state.jumpToEndEnabled)`.
    *   Checkbox `onCheckedChange` handler should call `useJumpToEndStore.getState().setJumpToEndEnabled(newCheckedState)`.

5.  **Token Handling (`AiStudio4/AiStudio4.Web/src/hooks/useStreamTokens.ts` or `ConvView.tsx`):**
    *   Where new stream tokens are processed and added to the display:
        *   Import `useJumpToEndStore`.
        *   Check if `useJumpToEndStore.getState().jumpToEndEnabled` is true.
        *   If true, dispatch the custom event (e.g., `window.dispatchEvent(new CustomEvent('jump-to-end'))`) that `ConvView` listens for to trigger the scroll.

**Affected Files:**

*   `AiStudio4/AiStudio4.Web/src/components/ConvView.tsx` (Major changes)
*   `AiStudio4/AiStudio4.Web/src/components/InputBar.tsx` (Add checkbox)
*   `AiStudio4/AiStudio4.Web/src/hooks/useStreamTokens.ts` (Add check and event dispatch)
*   `AiStudio4/AiStudio4.Web/src/stores/useJumpToEndStore.ts` (New file)
*   `AiStudio4/AiStudio4.Web/package.json` (Remove dependency)
*   `AiStudio4/AiStudio4.Web/pnpm-lock.yaml` (Remove dependency)

**Testing:**

*   Verify `StickToBottom` is completely removed.
*   Verify the "Jump to End" checkbox appears correctly in the `InputBar`.
*   Verify the checkbox state is persisted (initially checked).
*   Verify checking/unchecking the box updates the store.
*   Verify that when checked, the view scrolls to the bottom as new tokens arrive.
*   Verify that when unchecked, the view does *not* scroll automatically.
*   Verify that manually scrolling the view (mouse wheel or scrollbar) automatically *unchecks* the checkbox.
*   Verify sending a new message re-enables the checkbox (or decide if this is desired behavior - current design keeps it unchecked after manual scroll).