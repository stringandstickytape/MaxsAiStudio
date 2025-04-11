// ThemeManager.ts
// Centralized theming system

interface ThemeableProperty {
  cssVar: string;
  description?: string;
  default?: string;
}

interface ComponentThemeSchema {
  [propName: string]: ThemeableProperty;
}

interface ThemeSchema {
  [componentName: string]: ComponentThemeSchema;
}

interface Theme {
  [componentName: string]: {
    [propName: string]: string;
  };
}

class ThemeManager {
  private static instance: ThemeManager;
  private schema: ThemeSchema = {};

  private constructor() {}

  public static getInstance(): ThemeManager {
    if (!ThemeManager.instance) {
      ThemeManager.instance = new ThemeManager();
    }
    return ThemeManager.instance;
  }

  /**
   * Discover themeable properties from all components dynamically.
   * Components should export either:
   * - `themeableProps` object
   * - or a `getThemeableProps()` function returning the object
   */
  public async discoverThemes(): Promise<void> {
    console.log('[ThemeManager] Starting theme discovery...');
    const modules = import.meta.glob('../components/**/*.tsx');
    for (const path in modules) {
      try {
        const mod: any = await modules[path]();
        const themeableProps =
          mod.themeableProps ||
          (typeof mod.getThemeableProps === 'function' && mod.getThemeableProps());
        if (themeableProps) {
          const componentName = this.extractComponentName(path);
          this.schema[componentName] = themeableProps;
          console.log(`[ThemeManager] Registered themeable props for component: ${componentName}`, themeableProps);
        } else {
          console.log(`[ThemeManager] No themeable props found for ${path}`);
        }
      } catch (error) {
        console.warn(`Theme discovery failed for ${path}:`, error);
      }
    }
    console.log('[ThemeManager] Theme discovery complete. Schema:', JSON.stringify(this.schema));
  }

  /**
   * Extract component name from file path.
   * Example: '../components/Button/Button.tsx' => 'Button'
   */
  private extractComponentName(path: string): string {
    const parts = path.split('/');
    const fileName = parts[parts.length - 1];
    return fileName.replace('.tsx', '');
  }

  /**
   * Get the current theme schema.
   */
  public getSchema(): ThemeSchema {
    return this.schema;
  }

  /**
   * Generate a default theme object based on schema defaults.
   */
  public generateDefaultTheme(): Theme {
    const theme: Theme = {};
    for (const component in this.schema) {
      theme[component] = {};
      const props = this.schema[component];
      for (const propName in props) {
        const defaultValue = props[propName].default ?? '';
        theme[component][propName] = defaultValue;
      }
    }
    return theme;
  }

  /**
   * Apply a theme by injecting CSS variables into the document.
   * Instead of global :root, inject component-scoped CSS vars for better specificity.
   */
  public applyTheme(theme: Theme): void {
    console.log('[ThemeManager] Applying theme:', theme);
    let css = '';
    for (const component in theme) {
      const compTheme = theme[component];
      const schemaProps = this.schema[component] || {};
      css += `.${component} {`;
      for (const prop in compTheme) {
        const value = compTheme[prop];
        const cssVar = schemaProps[prop]?.cssVar;
        if (cssVar) {
          css += `\n  ${cssVar}: ${value};`;
        } else {
          console.warn(`[ThemeManager] No cssVar found for ${component}.${prop}`);
        }
      }
      css += '\n}\n';
    }

    console.log('[ThemeManager] Injecting CSS:', css);
    this.injectStyle(css);
  }

  /**
   * Inject or update the <style> tag with the generated CSS.
   */
  private injectStyle(css: string): void {
    let styleTag = document.getElementById('dynamic-theme-style') as HTMLStyleElement | null;
    if (!styleTag) {
      styleTag = document.createElement('style');
      styleTag.id = 'dynamic-theme-style';
      document.head.appendChild(styleTag);
    }
    styleTag.innerHTML = css;
  }

  /**
   * Generate an LLM-compatible JSON schema describing theme properties.
   */
  public generateLLMToolSchema(): object {
    const properties: Record<string, any> = {};
    for (const component in this.schema) {
      const props = this.schema[component];
      for (const propName in props) {
        const key = `${component}-${propName}`;
        properties[key] = {
          type: 'string',
          description: props[propName].description || `Set ${propName} for ${component}`
        };
      }
    }

    return {
      name: 'set_theme_properties',
      description: 'Sets theme properties for UI components.',
      parameters: {
        type: 'object',
        properties,
        required: [] // Optionally populate required fields
      }
    };
  }
}

const themeManagerInstance = ThemeManager.getInstance();

// Expose schema generator globally for console access
// Usage: window.generateThemeLLMSchema()
if (typeof window !== 'undefined') {
  (window as any).generateThemeLLMSchema = () => themeManagerInstance.generateLLMToolSchema();
}

export default themeManagerInstance;