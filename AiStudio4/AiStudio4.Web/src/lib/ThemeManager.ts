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
  global?: ComponentThemeSchema;
  [componentName: string]: ComponentThemeSchema;
}

interface Theme {
  global?: {
    [propName: string]: string;
  };
  [componentName: string]: {
    [propName: string]: string;
  };
}

class ThemeManager {
  private static instance: ThemeManager;
  private schema: ThemeSchema = {};

  private constructor() {
    // Initialize global schema section
    this.schema.global = {
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
    };
  }

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
    
    // Add global defaults
    if (this.schema.global) {
      theme.global = {};
      for (const propName in this.schema.global) {
        const defaultValue = this.schema.global[propName].default ?? '';
        theme.global[propName] = defaultValue;
      }
    }
    
    // Add component defaults
    for (const component in this.schema) {
      if (component === 'global') continue; // Skip global, already handled
      
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
   * Global properties are injected to :root, component properties to their respective classes.
   */
  public applyTheme(theme: Theme): void {
    console.log('[ThemeManager] Applying theme:', theme);
    let css = '';
    
    // Handle global properties - inject to :root
    if (theme.global && this.schema.global) {
      css += ':root {\n';
      for (const prop in theme.global) {
        const value = theme.global[prop];
        const cssVar = this.schema.global[prop]?.cssVar;
        if (cssVar) {
          css += `  ${cssVar}: ${value};\n`;
        }
      }
      css += '}\n\n';
    }
    
    // Handle component-specific properties
    for (const component in theme) {
      if (component === 'global') continue; // Skip global, already handled
      
      const compTheme = theme[component];
      const schemaProps = this.schema[component] || {};
      css += `.${component} {\n`;
      for (const prop in compTheme) {
        const value = compTheme[prop];
        const cssVar = schemaProps[prop]?.cssVar;
        if (cssVar) {
          css += `  ${cssVar}: ${value};\n`;
        }
      }
      css += '}\n';
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
    
    // Add global properties
    if (this.schema.global) {
      for (const propName in this.schema.global) {
        const key = `global-${propName}`;
        properties[key] = {
          type: 'string',
          description: this.schema.global[propName].description || `Set global ${propName}`
        };
      }
    }
    
    // Add component properties
    for (const component in this.schema) {
      if (component === 'global') continue; // Skip global, already handled
      
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
      description: 'Sets theme properties for UI components and global application styles.',
      parameters: {
        type: 'object',
        properties,
        required: []
      }
    };
  }

  /**
   * Accepts a flat LLM theme response (e.g., {"Component-prop": value, ...} or {"global-prop": value, ...}),
   * converts it into nested Theme object, and applies it.
   */
  public applyLLMTheme(flatThemeObj: Record<string, string>): void {
    console.log('[ThemeManager] Applying LLM theme response:', flatThemeObj);
    console.log('[ThemeManager] Theme response type:', typeof flatThemeObj);
    console.log('[ThemeManager] Is array?', Array.isArray(flatThemeObj));
    
    if (typeof flatThemeObj !== 'object' || flatThemeObj === null) {
      console.error('[ThemeManager] Invalid theme object format:', flatThemeObj);
      return;
    }
    
    const nestedTheme: Theme = {};
    let processedKeys = 0;
    
    for (const flatKey in flatThemeObj) {
      processedKeys++;
      const value = flatThemeObj[flatKey];
      const sepIndex = flatKey.indexOf('-');
      if (sepIndex === -1) {
        console.warn(`[ThemeManager] Invalid theme key (missing dash): ${flatKey}`);
        continue;
      }
      const component = flatKey.substring(0, sepIndex);
      const prop = flatKey.substring(sepIndex + 1);
      
      if (!nestedTheme[component]) {
        nestedTheme[component] = {};
      }
      nestedTheme[component][prop] = value;
    }
    
    console.log('[ThemeManager] Parsed nested theme:', nestedTheme);
    
    if (processedKeys === 0) {
      console.warn('[ThemeManager] No valid theme properties found in the input');
    }
    
    this.applyTheme(nestedTheme);
  }
}

const themeManagerInstance = ThemeManager.getInstance();

// Expose schema generator globally for console access
// Usage: window.generateThemeLLMSchema()
if (typeof window !== 'undefined') {
  (window as any).generateThemeLLMSchema = () => themeManagerInstance.generateLLMToolSchema();
  (window as any).applyLLMTheme = (json: Record<string, string>) => themeManagerInstance.applyLLMTheme(json);
}

export default themeManagerInstance;