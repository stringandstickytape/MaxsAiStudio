# Common AI Mistakes
This document records mistakes made by the AI to help avoid similar issues in the future.## Attempted to define CSS keyframes inline in React style object
*Recorded: 2025-05-05 19:25:53*

### Description
I tried to define CSS keyframes for the highlight pulse animation directly inside a React inline style object, which is not supported by CSS or React. Keyframes must be defined in CSS or injected into the document's stylesheet.

### Root Cause
Misunderstanding of CSS limitations regarding keyframe animations and inline styles in React.

### Prevention Strategy
Define keyframe animations in CSS files or dynamically inject styles into the document head. Avoid attempting to define keyframes inside React inline style objects.

---

## Overcomplicating Theme Implementation
*Recorded: 2025-05-05 20:15:39*

### Description
I added component-specific CSS variables and a themeableProps export when the requirement was simply to use existing global theme properties.

### Root Cause
I misinterpreted the implementation guide and assumed I needed to create a component-specific theming layer with fallbacks to global properties, rather than directly using the global properties.

### Prevention Strategy
When implementing theming, carefully read the requirements to determine if component-specific variables are needed or if global variables should be used directly. Ask clarifying questions before implementing complex solutions.

---

## Misinterpreting Theme Implementation Requirements
*Recorded: 2025-05-05 20:18:08*

### Description
I initially created component-specific theme variables with global fallbacks, then removed the themeableProps export entirely, when the requirement was to use global theme properties while still exporting themeableProps.

### Root Cause
I misunderstood the implementation pattern from the documentation. I first overcomplicated by adding component-specific variables, then overcompensated by removing the themeableProps export entirely.

### Prevention Strategy
Read implementation guides more carefully, focusing on the specific pattern to follow. When correcting a mistake, ensure I understand the full requirements before making changes.

---

## Using Incorrect CSS Variable Naming Convention
*Recorded: 2025-05-05 20:24:49*

### Description
Used camelCase for CSS variable names (e.g., --global-backgroundColor) instead of kebab-case (e.g., --global-background-color) as specified in the project documentation.

### Root Cause
Failed to carefully check the existing variable naming convention in the ComponentTheming.md documentation before implementation.

### Prevention Strategy
Always verify the exact naming conventions used in the project by checking documentation or existing code examples before implementing CSS variables. Pay special attention to hyphenation patterns in CSS variable names as kebab-case is the standard convention.

---

