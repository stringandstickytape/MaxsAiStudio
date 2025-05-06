# Font CDN URL Implementation

## Overview

This document describes the implementation of a new feature that allows the AI to specify a CDN URL for a font as part of the theme schema. When specified, this font will be applied to the entire document.

## Implementation Details

### 1. Theme Model Updates

#### Backend (C#)
- Added a `FontCdnUrl` property to the `Theme` class in `Core/Models/Theme.cs`
- Updated `ThemeService.cs` to include the FontCdnUrl in the default theme
- Added a global fontCdnUrl property to the ThemeJson dictionary in the default theme

#### Frontend (TypeScript)
- Added an optional `fontCdnUrl` property to the `Theme` interface in `types/theme.ts`
- Added a `fontCdnUrl` property to the global theme schema in `ThemeManager.ts`

### 2. Font Loading Mechanism

The `ThemeManager.ts` class has been enhanced with the following features:

- Added a `fontLinkElement` property to track the current font link element
- Implemented a `loadFontFromCdn` method that:
  - Removes any existing font link element
  - Creates a new link element with the specified CDN URL
  - Adds the link element to the document head
- Updated the `applyTheme` method to check for and load fonts from CDN URLs
- Added CSS variables to the `:root` element for document-wide access to theme properties

### 3. LLM Schema Updates

The `generateLLMToolSchema` method in `ThemeManager.ts` now includes the font CDN URL property in the schema, allowing the AI to specify it when creating or modifying themes.

## Usage

### For AI Models

AI models can now specify a font CDN URL when creating or modifying themes by including the `global-fontCdnUrl` property in their theme response. For example:

```json
{
  "theme-name": "Custom Font Theme",
  "global-fontCdnUrl": "https://fonts.googleapis.com/css2?family=Roboto:wght@400;700&display=swap",
  "global-font-family": "'Roboto', sans-serif"
}
```

### For Developers

The font CDN URL can be accessed and modified through the theme management interface. When a theme with a font CDN URL is applied, the font will be automatically loaded and applied to the entire document.

## Security Considerations

- Only trusted font CDN URLs should be used to prevent potential security issues
- The application does not validate the content of the CDN URL, so it's important to ensure it points to a legitimate font resource

## Future Enhancements

- Add validation for font CDN URLs
- Support multiple font CDN URLs
- Add font preloading for improved performance
- Implement font fallback mechanisms
