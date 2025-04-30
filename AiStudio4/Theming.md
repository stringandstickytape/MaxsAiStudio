# Theming.md - How to Add Flexible Theming Support to AiStudio4 Components

This guide explains how to make any React component in AiStudio4 **extensively themeable** using the project's ThemeManager system. It includes best practices and lessons learned.

---

## 1. Overview

- **Themes** are applied via CSS variables and inline styles.
- **ThemeManager** discovers themeable properties and injects CSS variables **scoped to a CSS class matching the component name**.
- **Global theme properties** are injected to the `:root` element and can be used across all components.
- Components use these CSS variables (with sensible defaults) and can also accept **arbitrary style overrides**.
- **All relevant elements must have the component class (e.g., `Sidebar`, `HistoricalConvTreeList`, `ConvTreeView`) for CSS vars to apply.**
- **D3 or canvas-based visualizations should read theme CSS vars at render time and use them for drawing.**
- This enables both structured, documented theming and maximum flexibility.

---

## 2. Step-by-step: Making a Component Themeable

### Step 1: Export a `themeableProps` schema

In your component file (e.g., `MyComponent.tsx`), export an object:

```ts
export const themeableProps = {
  backgroundColor: {
    cssVar: '--mycomponent-bg',
    description: 'Background color',
    default: '#222',
  },
  textColor: {
    cssVar: '--mycomponent-text-color',
    description: 'Text color',
    default: '#eee',
  },
  borderRadius: {
    cssVar: '--mycomponent-radius',
    description: 'Border radius',
    default: '8px',
  },
  // ... add as many structured props as desired

  // Arbitrary style overrides
  style: {
    description: 'Arbitrary CSS style for root',
    default: {},
  },
  popupStyle: {
    description: 'Arbitrary CSS style for popup panel',
    default: {},
  },
  pillStyle: {
    description: 'Arbitrary CSS style for pills/buttons',
    default: {},
  },
  editAreaStyle: {
    description: 'Arbitrary CSS style for edit textarea',
    default: {},
  },
};
```

**Important:**
- For **every structured property**, include a `cssVar` key.
- This ensures ThemeManager injects CSS variables.

### Step 2: Use CSS variables + style overrides in JSX

In your component JSX:

- For the **root container**, merge:

```tsx
<div
  className="MyComponent"
  style={{
    backgroundColor: 'var(--mycomponent-bg, var(--global-background-color, #222))',
    color: 'var(--mycomponent-text-color, var(--global-text-color, #eee))',
    borderRadius: 'var(--mycomponent-radius, var(--global-border-radius, 8px))',
    ...(window?.theme?.MyComponent?.style || {})
  }}
>
```

- For **popups, pills, edit areas**, do the same:

```tsx
<div
  className="MyComponent"
  style={{
    backgroundColor: 'var(--mycomponent-popup-bg, var(--global-background-color, #333))',
    ...(window?.theme?.MyComponent?.popupStyle || {})
  }}
>

<Button
  style={{
    backgroundColor: 'var(--mycomponent-pill-bg, var(--global-primary-color, #444))',
    ...(window?.theme?.MyComponent?.pillStyle || {})
  }}
>

<Textarea
  style={{
    backgroundColor: 'var(--mycomponent-edit-bg, var(--global-background-color, #555))',
    ...(window?.theme?.MyComponent?.editAreaStyle || {})
  }}
/>
```

### Step 3: Add the component class to **all relevant elements**

- ThemeManager injects CSS variables **scoped to `.MyComponent { ... }`**
- So **every element that needs those vars must have class `MyComponent`**
- This includes **portal content** and **all nested elements that require CSS vars**.
- For complex components (e.g., D3 visualizations), ensure the SVG or canvas root has the class, and read CSS vars in JS if needed.

```tsx
createPortal(
  <div className="fixed z-50 MyComponent" style={...}>
    ...
  </div>,
  document.body
)
```

### Step 4: Update the theme in `main.tsx` for testing

```ts
ThemeManager.applyTheme({
  global: {
    backgroundColor: '#f5f5f5',
    textColor: '#333333',
    primaryColor: '#007acc',
  },
  MyComponent: {
    backgroundColor: '#123456',
    textColor: '#abcdef',
    borderRadius: '12px',
    style: { filter: 'brightness(1.2)' },
    popupStyle: { backdropFilter: 'blur(4px)' },
    pillStyle: { fontWeight: 'bold' },
    editAreaStyle: { fontFamily: 'monospace' },
  },
});
```

---

## 3. How ThemeManager Works

- Discovers `themeableProps` exports dynamically.
- Injects global CSS variables to the `:root` element.
- Injects component-specific CSS variables **scoped to a class selector matching the component name**.
- Example:

```css
:root {
  --global-background-color: #f5f5f5;
  --global-text-color: #333333;
  --global-primary-color: #007acc;
}

.MyComponent {
  --mycomponent-bg: #222;
  --mycomponent-text-color: #eee;
  --mycomponent-radius: 8px;
}
```

- Components can reference global variables as fallbacks in their styles.
- This allows for consistent theming across the application while still allowing component-specific overrides.

---

## 4. Using Global Theme Properties

Global theme properties provide application-wide styling that components can reference. The ThemeManager includes these default global properties:

```ts
global: {
  backgroundColor: {
    cssVar: '--global-background-color',
    description: 'Global background color for the application',
    default: '#ffffff'
  },
  textColor: {
    cssVar: '--global-text-color',
    description: 'Global text color for the application',
    default: '#333333'
  },
  primaryColor: {
    cssVar: '--global-primary-color',
    description: 'Primary accent color for the application',
    default: '#007acc'
  },
  secondaryColor: {
    cssVar: '--global-secondary-color',
    description: 'Secondary accent color for the application',
    default: '#6e6e6e'
  },
  borderColor: {
    cssVar: '--global-border-color',
    description: 'Default border color for elements',
    default: '#dddddd'
  },
  borderRadius: {
    cssVar: '--global-border-radius',
    description: 'Default border radius for elements',
    default: '4px'
  },
  fontFamily: {
    cssVar: '--global-font-family',
    description: 'Default font family for text',
    default: '"Segoe UI", "Noto Sans", sans-serif'
  },
  fontSize: {
    cssVar: '--global-font-size',
    description: 'Base font size for the application',
    default: '14px'
  },
  boxShadow: {
    cssVar: '--global-box-shadow',
    description: 'Default box shadow for elevated elements',
    default: '0 2px 5px rgba(0,0,0,0.1)'
  }
}
```

To use global properties in your components:

1. **Reference them as fallbacks in your CSS variable usage:**

```css
background-color: var(--mycomponent-bg, var(--global-background-color, #default));
color: var(--mycomponent-text-color, var(--global-text-color, #default));
```

2. **Set global properties in your theme:**

```ts
ThemeManager.applyTheme({
  global: {
    backgroundColor: '#f5f5f5',
    textColor: '#333333',
    primaryColor: '#007acc',
  },
  // Component-specific overrides
  MyComponent: { ... }
});
```

---

## 5. Lessons Learned & Best Practices

- **Always add `cssVar` keys** for all structured theme props.
- **Explicitly set theme props** in `applyTheme()` to ensure CSS vars are generated.
- **Add the component class to all portal content** to inherit CSS vars.
- **Use CSS vars with sensible fallbacks** in inline styles, referencing global variables when appropriate.
- **Merge arbitrary style overrides** for maximum flexibility.
- **Expose both structured props and arbitrary style objects** in `themeableProps`.
- **Test themes with unusual styles** (gradients, filters, transforms) to verify flexibility.
- **Aspire to include many specific, sensibly-chosen properties** (colors, fonts, borders, shadows, spacing, etc.) in the schema.
- **Explicitly include button styling in your theming design.** This means:
  - Button background color, hover background color
  - Button border color
  - Button icon color
  - Button disabled opacity
  - And any other button-specific styles (e.g., font, border radius, shadow)
- For example, the **InputBar** component now exposes CSS variables and theme props for **all four of its key buttons** (send/cancel, voice input, user prompts, file attach), so they can be themed independently from the background or text.
- **Favor granular, component-specific override slots over generic ones.**
  - Instead of just `style` and `popupStyle`, expose overrides like `dropdownStyle`, `inputStyle`, `buttonStyle`, `pinButtonStyle`, `sectionHeaderStyle`, `commandItemStyle` (as done in CommandBar).
  - This enables **fine-grained control** over every part of the component.
  - Arbitrary override slots can be added **without reusing generic names**.
- **Add the component CSS class to all relevant elements, including portals, dropdowns, and all nested containers, so CSS vars apply everywhere.**
- **For D3/canvas/JS-rendered visualizations, read CSS vars at render time and use them for drawing.**
- **Use descriptive `description` fields** in `themeableProps` to help LLMs and users understand each property.
- This encourages LLMs or users to create **versatile, exciting, and creative themes** by providing clear options.
- The more granular and well-documented the properties, the more control and inspiration for theme designers.

---

## 6. Summary

- Use CSS variables for common props.
- Allow arbitrary style overrides.
- Scope CSS vars to component class.
- Use global CSS variables for application-wide styling.
- Reference global variables as fallbacks in component styles.
- Add class to portals and all relevant nested elements.
- For D3/canvas/JS-rendered elements, read CSS vars in JS and apply them to drawing.
- Explicitly set theme props to ensure vars are injected.
- This enables **extremely flexible, LLM-driven theming**.

---

## 7. Example: SystemPromptComponent

See `SystemPromptComponent.tsx` and `main.tsx` for a full example of this approach.

---

## 8. LLM Integration for Theming (2024)

### LLM Schema Generation

- ThemeManager can generate an **LLM-compatible JSON schema** describing all themeable properties.
- This schema flattens properties as keys like `ComponentName-propertyName` or `global-propertyName`.
- Example output:

```json
{
  "properties": {
    "global-backgroundColor": "#f5f5f5",
    "global-textColor": "#333333",
    "global-primaryColor": "#007acc",
    "SystemPromptComponent-backgroundColor": "#f5f5f5",
    "SystemPromptComponent-borderColor": "#3498db",
    "SystemPromptComponent-borderRadius": "8px",
    "SystemPromptComponent-boxShadow": "0 2px 10px rgba(0,0,0,0.1)",
    "SystemPromptComponent-textColor": "#333333",
    "SystemPromptComponent-fontSize": "16px",
    "SystemPromptComponent-fontFamily": "Arial, sans-serif",
    "SystemPromptComponent-pillActiveBg": "#3498db",
    "SystemPromptComponent-pillInactiveBg": "#e0e0e0",
    "InputBar-backgroundColor": "#ffffff"
  }
}
```

### Applying LLM Theme Responses

- ThemeManager now exposes a method:

```ts
window.applyLLMTheme(json)
```

- Pass in an LLM-generated flat JSON object (as above)
- It will **parse this into nested theme structure** and apply it live
- This enables **interactive console testing** and **easy LLM-driven theme updates**

### How it works internally

- The method `applyLLMTheme(flatThemeObj)`
  - Parses keys like `ComponentName-propertyName` or `global-propertyName`
  - Builds nested theme object `{ global: { propertyName: value }, ComponentName: { propertyName: value } }`
  - Calls `applyTheme()` with the nested theme

### Summary

- You can generate a schema for LLMs via:

```ts
window.generateThemeLLMSchema()
```

- And apply an LLM-generated theme via:

```ts
window.applyLLMTheme(yourJson)
```

- This makes **LLM-powered UI theming** straightforward.

---

## 9. Future Improvements

- Add runtime theme editing UI.
- Support CSS animations and transitions via theme.
- Add validation and error reporting for LLM theme inputs.
- Support partial theme updates without overriding entire components.

---

### Important: LLM Theme Key Naming Constraints

- The flattened keys used in LLM JSON (e.g., `ComponentName-propertyName` or `global-propertyName`) **must only contain letters, digits, hyphens (`-`), and underscores (`_`)**.
- **No spaces or other special characters** are allowed.
- The **total key length must be no longer than 64 characters**.
- **Plan component and property names carefully** to avoid collisions or truncation.
- Example of valid keys:

```
SystemPromptComponent-pillInactiveBg
global-backgroundColor
```

- Example of **invalid** keys:

```
System Prompt Component-pill inactive bg
global background-color
```

Keep names concise, descriptive, and within these limits for maximum compatibility.

---

# End of Theming.md