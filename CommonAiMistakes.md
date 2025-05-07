# Consolidated Guide to Error Prevention

1. **CSS Keyframes in React Inline Styles**  
   - Do not define CSS keyframes inside React inline style objects; this is unsupported.  
   - Define keyframe animations in CSS files or inject them dynamically into the document's stylesheet.

2. **Theme Implementation**  
   - Carefully read and understand theming requirements before implementation.  
   - Use global theme properties directly unless component-specific variables are explicitly required.  
   - Avoid unnecessary component-specific theming layers or removing required exports like `themeableProps`.  
   - When uncertain, ask clarifying questions and review implementation guides thoroughly.

3. **CSS Variable Naming Conventions**  
   - Follow the project’s documented naming conventions strictly.  
   - Use kebab-case (e.g., `--global-background-color`) for CSS variables, not camelCase.  
   - Verify naming conventions by consulting documentation or existing code before implementation.

4. **File and Directory Access**  
   - Confirm directory paths before attempting to read or search them.  
   - Avoid repeated attempts to access non-existent or inaccessible directories to prevent delays.

5. **JSON Formatting for Tool Calls (ModifyFiles Tool)**  
   - Use properly structured JSON objects, not string-escaped JSON, when calling tools like ModifyFiles.  
   - Ensure arrays such as the modifications array are direct JSON arrays, not strings.  
   - Prefer using the tool’s function call interface over manual JSON construction to avoid formatting errors.

6. **CSS File Modification Restrictions**  
   - Do not modify global CSS files to add styles for search matches, highlighted messages, or tree view elements if not allowed.  
   - Use inline styles or component-specific styling approaches instead.  
   - For D3 visualizations, apply styles directly through D3's API or SVG attributes rather than modifying CSS files.

7. **Variable Scope in React Hooks**  
   - Avoid referencing variables defined inside one effect scope from functions or callbacks outside that scope.  
   - Define shared variables at the hook level (outside any effects), pass them as dependencies to `useCallback` functions, or recreate the values where needed.  
   - Always verify that variables used in callbacks or functions are accessible in their scope to prevent runtime errors.

8. **Undefined Variable References**  
   - Always verify variable scope and availability before using variables in code modifications.  
   - Check if variables need to be imported, fetched from an API, or passed as props before using them in component logic.  
   - Do not assume variables used in one component are available in another without explicit definition or import.

9. **React.memo Syntax**  
   - When wrapping components with `React.memo`, ensure the memo comparison function is passed as the second argument to `React.memo`.  
   - Maintain proper syntax by keeping the component function structure intact and placing the comparison function correctly to avoid syntax errors.