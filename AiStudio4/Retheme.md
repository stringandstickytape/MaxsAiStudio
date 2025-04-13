
// ThemeSimplification.md

# Theme System Simplification – Implementation Instructions

## Overview

The AiStudio4 project currently uses a granular, component-specific theming system. Each major UI component exports a `themeableProps` object with many fine-grained properties (e.g., headerBackground, footerTextColor, etc.), and references these in their inline styles via CSS variables.

The goal is to **simplify** each component’s theming surface by reducing the number of themeable properties, while keeping them **component-specific** (e.g., `--sidebar-bg`, `--commandbar-bg`, etc.). This will make theming more manageable and maintainable, while still allowing per-component customization.

## Instructions

1. **For each of the components listed below:**
   - Replace the current `themeableProps` export with a simplified, component-specific object containing only:
     - `backgroundColor` (e.g., `--sidebar-bg`)
     - `textColor` (e.g., `--sidebar-text-color`)
     - `borderColor` (e.g., `--sidebar-border-color`)
     - `accentColor` (e.g., `--sidebar-accent-color`, used for highlights, status, or active items)
     - `style` (arbitrary style override for the root)
     - *(Optionally, add 1–2 extra props only if absolutely necessary for unique component features)*
     > **All CSS variable names must remain component-specific.**

2. **Update all style references in the component** to use only these new, less granular variables. Remove references to the old, more granular variables.
   - For example, in Sidebar.tsx, replace all uses of `--sidebar-header-bg`, `--sidebar-footer-bg`, etc., with `--sidebar-bg`.
   - For status indicators or highlights, use `--sidebar-accent-color`.
   - For any header/footer/content areas, use the general background/text/border variables.

3. **Do not change ThemeManager.ts** – it already supports this approach.

4. **Test** by applying a theme in `main.tsx` and verifying that components still look correct and style overrides still work.

## Key Principles

- **Keep all theming component-specific** (no generic `--bg` or `--text-color`).
- **Favor fewer, general properties** over many granular ones.
- **Only add extra themeable properties if absolutely necessary** for unique component features.
- **Update all inline style references** to match the new, simplified variables.

## Component Checklist

| Component                                                                 | Completed? |
|---------------------------------------------------------------------------|------------|
| AiStudio4.Web/src/components/Sidebar.tsx                                  |     Y      |
| AiStudio4.Web/src/components/AppHeader.tsx                                |     Y      |
| AiStudio4.Web/src/components/CommandBar.tsx                               |     Y      |
| AiStudio4.Web/src/components/ConvTreeView.tsx                             |     Y      |
| AiStudio4.Web/src/components/ConvView.tsx                                 |     Y      |
| AiStudio4.Web/src/components/HistoricalConvTreeList.tsx                   |     Y      |
| AiStudio4.Web/src/components/InputBar.tsx                                 |            |
| AiStudio4.Web/src/components/navigation/NavigationContainer.tsx            |            |
| AiStudio4.Web/src/components/SystemPrompt/SystemPromptComponent.tsx        |            |


## Example (Sidebar.tsx)

**Before:**
```typescript
export const themeableProps = {
  backgroundColor: { cssVar: '--sidebar-bg', ... },
  headerBackground: { cssVar: '--sidebar-header-bg', ... },
  footerBackground: { cssVar: '--sidebar-footer-bg', ... },
  // ...many more
}
```
**After:**
```typescript
export const themeableProps = {
  backgroundColor: { cssVar: '--sidebar-bg', ... },
  textColor: { cssVar: '--sidebar-text-color', ... },
  borderColor: { cssVar: '--sidebar-border-color', ... },
  accentColor: { cssVar: '--sidebar-accent-color', ... },
  style: { description: 'Arbitrary CSS style for Sidebar root', default: {} },
}
```
And update all style usages accordingly.

---

**Start with `AiStudio4.Web/src/components/Sidebar.tsx`.** Once complete, repeat for the other files in the list.

