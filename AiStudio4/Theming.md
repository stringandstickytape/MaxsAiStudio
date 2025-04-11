# Theming.md - How to Add Flexible Theming Support to AiStudio4 Components

This guide explains how to make any React component in AiStudio4 **extensively themeable** using the project's ThemeManager system. It includes best practices and lessons learned.

---

## 1. Overview

- **Themes** are applied via CSS variables and inline styles.
- **ThemeManager** discovers themeable properties and injects CSS variables **scoped to a CSS class matching the component name**.
- Components use these CSS variables (with sensible defaults) and can also accept **arbitrary style overrides**.
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
    backgroundColor: 'var(--mycomponent-bg, #222)',
    color: 'var(--mycomponent-text-color, #eee)',
    borderRadius: 'var(--mycomponent-radius, 8px)',
    ...(window?.theme?.MyComponent?.style || {})
  }}
>
```

- For **popups, pills, edit areas**, do the same:

```tsx
<div
  className="MyComponent"
  style={{
    backgroundColor: 'var(--mycomponent-popup-bg, #333)',
    ...(window?.theme?.MyComponent?.popupStyle || {})
  }}
>

<Button
  style={{
    backgroundColor: 'var(--mycomponent-pill-bg, #444)',
    ...(window?.theme?.MyComponent?.pillStyle || {})
  }}
>

<Textarea
  style={{
    backgroundColor: 'var(--mycomponent-edit-bg, #555)',
    ...(window?.theme?.MyComponent?.editAreaStyle || {})
  }}
/>
```

### Step 3: Add the component class to **all relevant elements**

- ThemeManager injects CSS variables **scoped to `.MyComponent { ... }`**
- So **every element that needs those vars must have class `MyComponent`**
- This includes **portal content**:

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
- Injects CSS variables **scoped to a class selector matching the component name**.
- Example:

```css
.MyComponent {
  --mycomponent-bg: #222;
  --mycomponent-text-color: #eee;
  --mycomponent-radius: 8px;
}
```

- **Does NOT inject variables globally** (`:root`).
- So **elements outside the component class (e.g., portals)** won't inherit vars **unless you add the class**.

---

## 4. Lessons Learned & Best Practices

- **Always add `cssVar` keys** for all structured theme props.
- **Explicitly set theme props** in `applyTheme()` to ensure CSS vars are generated.
- **Add the component class to all portal content** to inherit CSS vars.
- **Use CSS vars with sensible fallbacks** in inline styles.
- **Merge arbitrary style overrides** for maximum flexibility.
- **Expose both structured props and arbitrary style objects** in `themeableProps`.
- **Test themes with unusual styles** (gradients, filters, transforms) to verify flexibility.
- **Aspire to include many specific, sensibly-chosen properties** (colors, fonts, borders, shadows, spacing, etc.) in the schema.
- This encourages LLMs or users to create **versatile, exciting, and creative themes** by providing clear options.
- The more granular and well-documented the properties, the more control and inspiration for theme designers.

---

## 5. Summary

- Use CSS variables for common props.
- Allow arbitrary style overrides.
- Scope CSS vars to component class.
- Add class to portals.
- Explicitly set theme props to ensure vars are injected.
- This enables **extremely flexible, LLM-driven theming**.

---

## 6. Example: SystemPromptComponent

See `SystemPromptComponent.tsx` and `main.tsx` for a full example of this approach.

---

## 7. LLM Integration for Theming (2024)

### LLM Schema Generation

- ThemeManager can generate an **LLM-compatible JSON schema** describing all themeable properties.
- This schema flattens properties as keys like `ComponentName-propertyName`.
- Example output:

```json
{
  "properties": {
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
  - Parses keys like `ComponentName-propertyName`
  - Builds nested theme object `{ ComponentName: { propertyName: value } }`
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

## 8. Future Improvements

- Patch ThemeManager to optionally inject **global** CSS vars if needed.
- Add runtime theme editing UI.
- Support CSS animations and transitions via theme.
- Add validation and error reporting for LLM theme inputs.
- Support partial theme updates without overriding entire components.

---

### Important: LLM Theme Key Naming Constraints

- The flattened keys used in LLM JSON (e.g., `ComponentName-propertyName`) **must only contain letters, digits, hyphens (`-`), and underscores (`_`)**.
- **No spaces or other special characters** are allowed.
- The **total key length must be no longer than 64 characters**.
- **Plan component and property names carefully** to avoid collisions or truncation.
- Example of a valid key:

```
SystemPromptComponent-pillInactiveBg
```

- Example of an **invalid** key:

```
System Prompt Component-pill inactive bg
```

Keep names concise, descriptive, and within these limits for maximum compatibility.

---

# End of Theming.md