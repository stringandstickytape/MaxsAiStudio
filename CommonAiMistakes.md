# Consolidated Guide to Error Prevention

1. **CSS Keyframes in React Inline Styles**  
   - Define keyframe animations in CSS files or inject them dynamically; do not use inline styles.

2. **Theme Implementation**  
   - Understand theming requirements thoroughly; use global theme properties directly unless specified otherwise.  
   - Avoid unnecessary component-specific theming layers and ensure required exports like `themeableProps` are not removed.  
   - Ask clarifying questions when uncertain.

3. **CSS Variable Naming Conventions**  
   - Follow documented naming conventions strictly; use kebab-case (e.g., `--global-background-color`).

4. **File and Directory Access**  
   - Confirm directory paths before access; avoid repeated attempts on non-existent directories.

5. **JSON Formatting for Tool Calls**  
   - Use properly structured JSON objects for tool calls; ensure arrays are direct JSON arrays, not strings.

6. **CSS File Modification Restrictions**  
   - Do not modify global CSS files for specific styles if not allowed; use inline or component-specific styles instead.

7. **Variable Scope in React Hooks**  
   - Avoid referencing variables from one effect scope in callbacks outside that scope; define shared variables at the hook level.

8. **Undefined Variable References**  
   - Verify variable scope and availability before use; check if variables need to be imported or passed as props.

9. **React.memo Syntax**  
   - Ensure the memo comparison function is passed correctly as the second argument to `React.memo` to maintain proper syntax.

10. **Syntax Errors in C# Code**  
    - Always review code for basic syntax errors before submission; specifically check for missing semicolons at the end of statements. Use an IDE or code linter when possible to catch these errors.

11. **Using Existing Event Systems Instead of CustomEvent**  
    - Before implementing any cross-component communication mechanism, thoroughly investigate the codebase for existing event handling patterns and utilities.  
    - Search for terms like 'event', 'service', 'bus', or 'emitter' to identify existing event systems.  
    - When multiple components communicate, trace how they do so rather than assuming a new mechanism is needed.  
    - Never implement direct `CustomEvent` dispatching without confirming that no existing event system is in place.  
    - Always prioritize using the application's existing event system (e.g., `windowEventService`) to maintain consistency and avoid redundant or conflicting event handling mechanisms.

12. **Incorrectly Removed UI Text**  
    - Carefully analyze which parts of a text block are labels versus dynamic content that should be preserved.  
    - Use more precise `oldContent` in `ModifyFiles` to target only the elements/text to be removed.  
    - When removing parts of a string, ensure the remaining parts are still syntactically and semantically correct.  
    - Double-check the user's request to ensure only the specified elements are targeted.  
    - Avoid overly broad selections that span too large a section, which can lead to removal of desired text and malformed HTML structure.  
    - Confirm that HTML structure remains valid after modifications, avoiding self-closing tags that conflict with later closing tags.