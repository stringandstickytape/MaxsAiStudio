// AiStudio4.Web/src/api/themeApi.ts

import { Theme } from '../types/theme';
import { createApiRequest } from '../utils/apiUtils';

// IMPORTANT NOTE FOR ALL AI ASSISTANTS: 
// All API endpoints MUST include the 'api/' prefix!
// The UiRequestBroker.cs handles routes like 'themes/add' internally,
// but the actual HTTP endpoints must be prefixed with 'api/'.
// DO NOT remove the 'api/' prefix from any endpoint!

/**
 * Fetches all themes from the server.
 */
export const fetchThemes = createApiRequest<void, Theme[]>(
  'api/themes/getAll',
  'POST',
  {
    transformResponse: (data) => {
      console.log('[ThemeAPI Debug] Transforming themes response:', data);
      // Check if data has a themes property (API returns {success: true, themes: Array})
      return (data && data.themes) ? data.themes : (Array.isArray(data) ? data : []);
    }
  }
);

/**
 * Fetches a single theme by ID.
 */
export const fetchTheme = createApiRequest<string, Theme>(
  'api/themes/getById',
  'POST',
  {
    transformResponse: (data) => data
  }
);

/**
 * Adds a new theme to the library.
 * IMPORTANT: Must use 'api/themes/add' as the endpoint!
 */
export const addTheme = async (theme: Theme): Promise<Theme> => {
  console.log('[ThemeAPI Debug] addTheme called with:', theme);
  try {
    const result = await createApiRequest<Theme, Theme>(
      'api/themes/add',
      'POST',
      {
        transformResponse: (data) => {
          console.log('[ThemeAPI Debug] addTheme response:', data);
          return data;
        }
      }
    )(theme);
    console.log('[ThemeAPI Debug] addTheme successful, returned:', result);
    return result;
  } catch (error) {
    console.error('[ThemeAPI Debug] addTheme error:', error);
    throw error;
  }
};

/**
 * Deletes a theme by ID.
 */
export const deleteTheme = createApiRequest<string, void>(
  'api/themes/delete',
  'POST'
);

/**
 * Imports themes from a JSON string.
 */
export const importThemes = createApiRequest<string, Theme[]>(
  'api/themes/import',
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
  'api/themes/export',
  'POST',
  {
    transformResponse: (data) => data
  }
);

/**
 * Updates an existing theme.
 */
export const updateTheme = createApiRequest<Theme, Theme>(
  'api/themes/update',
  'PUT',
  {
    transformResponse: (data) => data
  }
);

/**
 * Sets a theme as the default theme.
 */
export const setDefaultTheme = async (themeId: string): Promise<void> => {
  console.log('[ThemeAPI Debug] setDefaultTheme called with ID:', themeId);
  try {
    const endpoint = `api/themes/setDefault/${themeId}`;
    console.log('[ThemeAPI Debug] Using endpoint:', endpoint);
    
    await createApiRequest<void, void>(
      endpoint,
      'POST'
    )();
    
    console.log('[ThemeAPI Debug] Theme successfully set as default');
  } catch (error) {
    console.error('[ThemeAPI Debug] setDefaultTheme error:', error);
    throw error;
  }
};

/**
 * Gets the current default theme.
 */
export const getDefaultTheme = createApiRequest<void, Theme>(
  'api/themes/default',
  'POST',
  {
    transformResponse: (data) => data
  }
);