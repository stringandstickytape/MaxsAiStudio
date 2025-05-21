# Consolidated Guide to Error Prevention

1.  **CSS Keyframes in React Inline Styles**
    *   Define keyframe animations in CSS files or inject them dynamically; do not use inline styles.

2.  **Theme Implementation**
    *   Understand theming requirements thoroughly; use global theme properties directly unless specified otherwise.
    *   Avoid unnecessary component-specific theming layers and ensure required exports like `themeableProps` are not removed.
    *   When encountering hardcoded styles that seem to conflict with global theming, first confirm if these styles are part of an intentional local theme or a fallback for specific scenarios (e.g., dark mode).
    *   Ask for more precise details about *what specific visual elements* of a component are not themed correctly (e.g., "text color is wrong," "border is missing," "hover state doesn't match") rather than a general "not themed correctly."
    *   Before removing styles, consider if the issue is that *other* theme variables are not being applied correctly to child elements or pseudo-states of the component.
    *   If a component has both hardcoded styles and is supposed to be themeable, investigate how the theme is intended to interact with or override those hardcoded styles.
    *   When applying themes, especially for interactive states like hover, always prioritize using existing global CSS variables or creating new ones if necessary, rather than hardcoding colors.
    *   Ensure that any removed classes (e.g., Tailwind hover classes) are either replaced with equivalent themed styles or are no longer needed.
    *   Always re-read the theming guidelines to ensure adherence to the established patterns (e.g., using `var(--global-accent-color)` for highlights and interactive elements).
    *   Ask clarifying questions when uncertain.

3.  **CSS Variable Naming Conventions**
    *   Follow documented naming conventions strictly; use kebab-case (e.g., `--global-background-color`).

4.  **File and Directory Access**
    *   **Always** confirm directory paths before access, **explicitly using `DirectoryTree` to understand the project structure and verify paths** before assuming file locations or attempting file operations.
    *   **Never** make repeated attempts on non-existent directories.

5.  **JSON Formatting for Tool Calls**
    *   Use properly structured JSON objects for tool calls; ensure arrays are direct JSON arrays, not strings.

6.  **CSS File Modification Restrictions**
    *   Do not modify global CSS files for specific styles if not allowed; use inline or component-specific styles instead.

7.  **Variable Scope in React Hooks**
    *   Avoid referencing variables from one effect scope in callbacks outside that scope; define shared variables at the hook level.

8.  **Undefined Variable References**
    *   Verify variable scope and availability before use; check if variables need to be imported or passed as props.

9.  **React.memo Syntax**
    *   Ensure the memo comparison function is passed correctly as the second argument to `React.memo` to maintain proper syntax.

10. **Syntax Errors in C# Code**
    *   Always review code for basic syntax errors before submission; specifically check for missing semicolons at the end of statements.


11. **Using Existing Event Systems Instead of CustomEvent**
    *   Before implementing any cross-component communication mechanism, thoroughly investigate the codebase for existing event handling patterns and utilities.
    *   Search for terms like 'event', 'service', 'bus', or 'emitter' to identify existing event systems.
    *   When multiple components communicate, trace how they do so rather than assuming a new mechanism is needed.
    *   Never implement direct `CustomEvent` dispatching without confirming that no existing event system is in place.
    *   Always prioritize using the application's existing event system (e.g., `windowEventService`) to maintain consistency and avoid redundant or conflicting event handling mechanisms.

12. **Incorrectly Removed UI Text**
    *   Carefully analyze which parts of a text block are labels versus dynamic content that should be preserved.
    *   Use more precise `oldContent` in `ModifyFiles` to target only the elements/text to be removed.
    *   When removing parts of a string, ensure the remaining parts are still syntactically and semantically correct.
    *   Double-check the user's request to ensure only the specified elements are targeted.
    *   Avoid overly broad selections that span too large a section, which can lead to removal of desired text and malformed HTML structure.
    *   Confirm that HTML structure remains valid after modifications, avoiding self-closing tags that conflict with later closing tags.

13. **ModifyFiles `oldContent` Too Short**
    *   When using `ModifyFiles`, ensure `oldContent` includes enough surrounding lines (3-5 lines before and after the change) to uniquely identify the target modification block.
    *   Avoid providing only a single line or too short a snippet that may appear multiple times or be ambiguous.
    *   Double-check the file content before and after the call to confirm changes have been applied.

14. **User Feedback and Tool Call Management**
    *   **Absolutely always** re-read and **thoroughly** confirm user feedback, especially negative feedback, before proceeding.
    *   If a user **unequivocally** rejects a change, **immediately** revert it and **proactively** propose a new approach.
    *   **Strictly avoid** repeating tool calls with identical parameters if the previous attempt was not successful or accepted by the user, as this can lead to tool loops.