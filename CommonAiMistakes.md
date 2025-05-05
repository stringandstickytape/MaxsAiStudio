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

