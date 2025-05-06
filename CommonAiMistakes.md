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