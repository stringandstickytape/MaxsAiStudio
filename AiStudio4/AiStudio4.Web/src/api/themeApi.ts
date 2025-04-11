// AiStudio4.Web/src/api/themeApi.ts

import { Theme } from '../types/theme';
import { createApiRequest } from '../utils/apiUtils';

/**
 * Fetches all themes from the server.
 */
export const fetchThemes = createApiRequest<void, Theme[]>(
  '/api/themes',
  'GET',
  {
    transformResponse: (data) => data || []
  }
);

/**
 * Fetches a single theme by ID.
 */
export const fetchTheme = createApiRequest<string, Theme>(
  '/api/themes/{id}',
  'GET',
  {
    transformResponse: (data) => data
  }
);

/**
 * Adds a new theme to the library.
 */
export const addTheme = createApiRequest<Theme, Theme>(
  '/api/themes',
  'POST',
  {
    transformResponse: (data) => data
  }
);

/**
 * Updates an existing theme.
 */
export const updateTheme = createApiRequest<Theme, Theme>(
  '/api/themes/{id}',
  'PUT',
  {
    transformResponse: (data) => data
  }
);

/**
 * Deletes a theme by ID.
 */
export const deleteTheme = createApiRequest<string, void>(
  '/api/themes/{id}',
  'DELETE'
);

/**
 * Imports themes from a JSON string.
 */
export const importThemes = createApiRequest<string, Theme[]>(
  '/api/themes/import',
  'POST',
  {
    transformResponse: (data) => data || []
  }
);

/**
 * Exports themes as a JSON string.
 * If ids is not provided, exports all themes.
 */
export const exportThemes = createApiRequest<string[] | undefined, string>(
  '/api/themes/export',
  'POST',
  {
    transformResponse: (data) => data
  }
);

/**
 * Sets a theme as the default theme.
 */
export const setDefaultTheme = createApiRequest<string, void>(
  '/api/themes/{id}/default',
  'POST'
);

/**
 * Gets the current default theme.
 */
export const getDefaultTheme = createApiRequest<void, Theme>(
  '/api/themes/default',
  'GET',
  {
    transformResponse: (data) => data
  }
);