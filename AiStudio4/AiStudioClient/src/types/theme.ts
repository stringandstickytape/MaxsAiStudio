// AiStudioClient/src/types/theme.ts

export interface Theme {
  guid: string;
  name: string;
  description: string;
  author?: string;
  previewColors: string[];
  themeJson: Record<string, Record<string, string>>;
  fontCdnUrl?: string;
  created: string;
  lastModified: string;
}

export interface ThemeCategory {
  name: string;
  colors: Record<string, string>;
}

export interface ThemeColors {
  [category: string]: Record<string, string>;
}

// Add this to the global Window interface
declare global {
  interface Window {
    generateThemeLLMSchema: () => object;
    applyLLMTheme: (json: Record<string, string>) => void;
    getCurrentThemeName: () => string;
  }
}