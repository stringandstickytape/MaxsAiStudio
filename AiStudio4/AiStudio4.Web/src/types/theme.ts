// AiStudio4.Web/src/types/theme.ts

export interface Theme {
  guid: string;
  name: string;
  description: string;
  author?: string;
  previewColors: string[];
  themeJson: Record<string, Record<string, string>>;
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