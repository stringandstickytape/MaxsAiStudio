# Component Theming Guide

This guide explains how to apply global themes to components in the AiStudio application using the ThemeManager system.

## Overview

The AiStudio application uses a centralized theming system managed by `ThemeManager.ts`. This system defines global theme variables that can be applied consistently across all components. This guide will walk you through the process of theming a component that doesn't have any theming yet.

## Step 1: Make Your Component Discoverable by ThemeManager

First, you need to make your component discoverable by the ThemeManager by exporting a `themeableProps` object:

```tsx
// YourComponent.tsx
import React from 'react';

// Define themeable properties for the component
export const themeableProps = {};

export function YourComponent() {
  // Component implementation
}
```

Even if you don't define any component-specific theme properties, this empty object allows ThemeManager to discover your component during its theme discovery process.

## Step 2: Add the Component Class Name

Add a class name to your component's root element that matches the component name. This is how ThemeManager targets components for styling:

```tsx
return (
  <div className="YourComponent other-classes">
    {/* Component content */}
  </div>
);
```

## Step 3: Apply Global Theme Variables

Apply global theme variables using inline styles. Here's a comprehensive example:

```tsx
return (
  <div 
    className="YourComponent other-classes" 
    style={{
      backgroundColor: 'var(--global-background-color)',
      color: 'var(--global-text-color)',
      borderRadius: 'var(--global-border-radius)',
      fontFamily: 'var(--global-font-family)',
      fontSize: 'var(--global-font-size)',
      boxShadow: 'var(--global-box-shadow)',
      border: `1px solid var(--global-border-color)`,
      padding: '1rem'
    }}
  >
    {/* Component content */}
  </div>
);
```

## Step 4: Theme Child Elements

### Input Fields

Apply theme variables to input fields:

```tsx
<input
  type="text"
  style={{
    backgroundColor: 'var(--global-background-color)',
    borderColor: 'var(--global-border-color)',
    color: 'var(--global-text-color)'
  }}
/>
```

For error states, use the primary color:

```tsx
<input
  type="text"
  style={{
    borderColor: hasError ? 'var(--global-primary-color)' : 'var(--global-border-color)'
  }}
/>
```

### Textareas

Apply theme variables to textareas:

```tsx
<textarea
  style={{
    backgroundColor: 'var(--global-background-color)',
    borderColor: 'var(--global-border-color)',
    color: 'var(--global-text-color)'
  }}
></textarea>
```

### Buttons

Buttons require special attention to ensure they have the appropriate background color:

#### Primary Buttons

```tsx
<button
  style={{
    backgroundColor: 'var(--global-primary-color)',
    color: '#ffffff' // White text for contrast
  }}
>
  Submit
</button>
```

#### Secondary/Outline Buttons

```tsx
<button
  style={{
    backgroundColor: 'var(--global-background-color)',
    borderColor: 'var(--global-border-color)',
    color: 'var(--global-text-color)'
  }}
>
  Cancel
</button>
```

### Help Text and Secondary Text

Use the secondary color for help text and secondary information:

```tsx
<div 
  className="help-text" 
  style={{ color: 'var(--global-secondary-color)' }}
>
  This is some helpful information.
</div>
```

### Error Messages

Use the primary color for error messages:

```tsx
<div 
  className="error-message" 
  style={{ color: 'var(--global-primary-color)' }}
>
  This field is required.
</div>
```

## Step 5: Force Backgrounds on Containers

It's important to explicitly set backgrounds on container elements to ensure consistent theming:

```tsx
<div 
  className="container" 
  style={{ backgroundColor: 'var(--global-background-color)' }}
>
  {/* Container content */}
</div>
```

This is particularly important for:
- Modal backgrounds
- Card containers
- Form sections
- Button containers
- Dropdown menus

## Step 6: Handle Nested Components

If your component contains other components, make sure to pass appropriate styling props or wrap them in themed containers:

```tsx
<div style={{ backgroundColor: 'var(--global-background-color)' }}>
  <ChildComponent />
</div>
```

## Example: Complete Component Theming

Here's a complete example of a form component with theming applied:

```tsx
// MyForm.tsx
import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

// Define themeable properties for the component
export const themeableProps = {};

export function MyForm() {
  const [value, setValue] = useState('');
  const [error, setError] = useState('');
  
  const handleSubmit = (e) => {
    e.preventDefault();
    // Form submission logic
  };
  
  return (
    <form 
      onSubmit={handleSubmit} 
      className="MyForm space-y-4" 
      style={{
        backgroundColor: 'var(--global-background-color)',
        color: 'var(--global-text-color)',
        borderRadius: 'var(--global-border-radius)',
        fontFamily: 'var(--global-font-family)',
        fontSize: 'var(--global-font-size)',
        boxShadow: 'var(--global-box-shadow)',
        border: `1px solid var(--global-border-color)`,
        padding: '1rem'
      }}
    >
      <div className="space-y-2">
        <label>Input Field</label>
        <Input
          value={value}
          onChange={(e) => setValue(e.target.value)}
          style={{
            borderColor: error ? 'var(--global-primary-color)' : 'var(--global-border-color)',
            backgroundColor: 'var(--global-background-color)',
            color: 'var(--global-text-color)'
          }}
        />
        {error && (
          <div style={{ color: 'var(--global-primary-color)' }}>
            {error}
          </div>
        )}
      </div>
      
      <div className="space-y-2">
        <label>Description</label>
        <textarea
          style={{
            backgroundColor: 'var(--global-background-color)',
            borderColor: 'var(--global-border-color)',
            color: 'var(--global-text-color)',
            width: '100%',
            padding: '0.5rem',
            borderRadius: 'var(--global-border-radius)'
          }}
        ></textarea>
        <div style={{ color: 'var(--global-secondary-color)' }}>
          Enter a detailed description
        </div>
      </div>
      
      <div 
        className="pt-4 flex justify-end space-x-2" 
        style={{ backgroundColor: 'var(--global-background-color)' }}
      >
        <Button
          type="button"
          style={{
            backgroundColor: 'var(--global-background-color)',
            borderColor: 'var(--global-border-color)',
            color: 'var(--global-text-color)'
          }}
        >
          Cancel
        </Button>
        <Button
          type="submit"
          style={{
            backgroundColor: 'var(--global-primary-color)',
            color: '#ffffff'
          }}
        >
          Submit
        </Button>
      </div>
    </form>
  );
}
```

## Best Practices

1. **Always add the component class name** to the root element to ensure ThemeManager can target it.

2. **Use inline styles with CSS variables** rather than hardcoded color values.

3. **Be comprehensive** - theme all elements within your component, not just the container.

4. **Pay special attention to buttons** - ensure they have appropriate background colors based on their purpose (primary, secondary, etc.).

5. **Force backgrounds on containers** to ensure consistent theming throughout the component hierarchy.

6. **Use semantic color variables** - use primary color for important actions and highlights, secondary color for less important text, etc.

7. **Test with different themes** to ensure your component looks good with various color schemes.

## Available Global Theme Variables

Here are the global theme variables available in the ThemeManager:

- `--global-background-color`: Background color for the application
- `--global-text-color`: Text color for the application
- `--global-primary-color`: Primary accent color for the application
- `--global-secondary-color`: Secondary accent color for the application
- `--global-border-color`: Default border color for elements
- `--global-border-radius`: Default border radius for elements
- `--global-font-family`: Default font family for text
- `--global-font-size`: Base font size for the application
- `--global-box-shadow`: Default box shadow for elevated elements

By following this guide, you'll ensure that your components integrate seamlessly with the AiStudio theming system, providing a consistent user experience across the application.