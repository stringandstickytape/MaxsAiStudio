// AiStudioClient/src/components/settings/ThemeForm.tsx

import React from 'react';
import { Theme } from '@/types/theme';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';

interface ThemeFormProps {
  initialValues?: Theme;
  onSubmit: (data: any) => Promise<void>;
  isProcessing: boolean;
}

export const ThemeForm: React.FC<ThemeFormProps> = ({ initialValues, onSubmit, isProcessing }) => {
  const fieldDefinitions = [
    ['name', 'Theme Name', 'text', 'e.g., Dark Ocean', undefined, undefined, undefined, 1],
    ['description', 'Description', 'text', 'A brief description of this theme', undefined, undefined, undefined, 2],
    ['author', 'Author', 'text', 'Your name or username', undefined, undefined, undefined, 1],
    [
      'previewColors',
      'Preview Colors (comma-separated hex)',
      'text',
      '#4f46e5,#2563eb,#7c3aed',
      undefined,
      undefined,
      'Comma-separated hex colors for theme preview',
      2,
    ],
    [
      'themeJson',
      'Theme JSON',
      'textarea',
      '{"Button-background":"#1e293b","Button-text":"#f8fafc"}',
      undefined,
      undefined,
      'JSON object containing theme properties',
      2,
    ],
  ];

  const fields: FormFieldDefinition[] = fieldDefinitions.map(
    ([name, label, type, placeholder, options, step, description, colSpan]) => {
      const field: FormFieldDefinition = { name, label, type, colSpan } as FormFieldDefinition;

      if (placeholder) field.placeholder = placeholder;
      if (options) field.options = options;
      if (step) field.step = step;
      if (description) field.description = description;

      return field;
    },
  );

  // Create default values for new themes
  const defaultValues = initialValues || {
    guid: '',
    name: '',
    description: '',
    author: '',
    previewColors: ['#4f46e5', '#2563eb', '#7c3aed'],
    themeJson: {},
    created: new Date().toISOString(),
    lastModified: new Date().toISOString(),
  };

  // Process previewColors for form display if it's an array
  const processedValues = {
    ...defaultValues,
    previewColors: Array.isArray(defaultValues.previewColors)
      ? defaultValues.previewColors.join(',')
      : defaultValues.previewColors,
    themeJson: typeof defaultValues.themeJson === 'object'
      ? JSON.stringify(defaultValues.themeJson, null, 2)
      : defaultValues.themeJson,
  };

  // Process form data before submission
  const processFormData = (data: any) => {
    // Convert comma-separated colors to array
    const processedData = {
      ...data,
      previewColors: data.previewColors.split(',').map((color: string) => color.trim()),
      themeJson: typeof data.themeJson === 'string'
        ? JSON.parse(data.themeJson)
        : data.themeJson,
    };
    return onSubmit(processedData);
  };

  return (
    <GenericForm
      fields={fields}
      initialValues={processedValues}
      onSubmit={processFormData}
      isProcessing={isProcessing}
      submitButtonText={initialValues ? 'Update Theme' : 'Add Theme'}
      layout="grid"
    />
  );
};
