// AiStudio4.Web/src/api/themeApi.ts

import { Theme } from '../types/theme';
import { createApiRequest } from '../utils/apiUtils';

/**
 * Fetches all themes from the server.
 */
export const fetchThemes = createApiRequest<void, Theme[]>(
  'themes/getAll',
  'GET',
  {
    transformResponse: (data) => data || []
  }
);

/**
 * Fetches a single theme by ID.
 */
export const fetchTheme = createApiRequest<string, Theme>(
  'themes/getById',
  'GET',
  {
    transformResponse: (data) => data
  }
);

/**
 * Adds a new theme to the library.
 */
export const addTheme = createApiRequest<Theme, Theme>(
  'api/themes/add',
  'POST',
  {
    transformResponse: (data) => data
  }
);

/**
 * Deletes a theme by ID.
 */
export const deleteTheme = createApiRequest<string, void>(
  'themes/delete',
  'DELETE'
);

/**
 * Imports themes from a JSON string.
 */
export const importThemes = createApiRequest<string, Theme[]>(
  'themes/import',
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
  'themes/export',
  'POST',
  {
    transformResponse: (data) => data
  }
);

/**
 * Updates an existing theme.
 */
export const updateTheme = createApiRequest<Theme, Theme>(
  'themes/update',
  'PUT',
  {
    transformResponse: (data) => data
  }
);

/**
 * Sets a theme as the default theme.
 */
export const setDefaultTheme = createApiRequest<string, void>(
  (themeId) => `themes/setDefault/${themeId}`,
  'POST'
);

/**
 * Gets the current default theme.
 */
export const getDefaultTheme = createApiRequest<void, Theme>(
  'themes/default',
  'GET',
  {
    transformResponse: (data) => data
  }
);