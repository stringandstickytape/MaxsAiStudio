
import React from 'react';
import { Model, ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';

interface ModelFormProps {
  providers: ServiceProvider[];
  initialValues?: Model;
  onSubmit: (data: any) => Promise<void>;
  isProcessing: boolean;
}

export const ModelForm: React.FC<ModelFormProps> = ({ providers, initialValues, onSubmit, isProcessing }) => {
  
  const providerOptions = providers.map((provider) => ({
    value: provider.guid,
    label: provider.friendlyName,
  }));

  const fieldDefinitions = [
    ['friendlyName', 'Friendly Name', 'text', 'e.g., GPT-4 Turbo', undefined, undefined, undefined, 1],
    ['modelName', 'Model Name', 'text', 'e.g., gpt-4-turbo', undefined, undefined, undefined, 1],
    ['providerGuid', 'Service Provider', 'select', undefined, providerOptions, undefined, undefined, 2],
    ['input1MTokenPrice', 'Input Token Price (per 1M)', 'number', '0.00', undefined, '0.01', 0, 1],
    ['output1MTokenPrice', 'Output Token Price (per 1M)', 'number', '0.00', undefined, '0.01', 0, 1],
    ['userNotes', 'Notes', 'text', 'Any additional notes about this model', undefined, undefined, undefined, 2],
    [
      'additionalParams',
      'Additional Parameters (JSON)',
      'text',
      '{"temperature": 0.7}',
      undefined,
      undefined,
      'Optional JSON parameters to include with requests to this model',
      2,
    ],
    ['color', 'Color', 'color', undefined, undefined, undefined, undefined, 1],
    [
      'starred',
      'Mark as Favorite',
      'checkbox',
      undefined,
      undefined,
      undefined,
      'Prioritize this model in selection lists',
      1,
    ],
    [
      'supportsPrefill',
      'Supports Prefill',
      'checkbox',
      undefined,
      undefined,
      undefined,
      'This model supports prefilling content',
      2,
    ],
  ];

  const fields: FormFieldDefinition[] = fieldDefinitions.map(
    ([name, label, type, placeholder, options, step, description, colSpan]) => {
      const field: FormFieldDefinition = { name, label, type, colSpan } as FormFieldDefinition;

      if (placeholder) field.placeholder = placeholder;
      if (options) field.options = options;
      if (step) field.step = step;
      if (type === 'number') field.min = 0;
      if (description) field.description = description;

      return field;
    },
  );

  
  const defaultValues = initialValues || {
    guid: '',
    modelName: '',
    friendlyName: '',
    providerGuid: '',
    userNotes: '',
    input1MTokenPrice: 0,
    output1MTokenPrice: 0,
    color: '#4f46e5',
    starred: false,
    supportsPrefill: false,
  };

  return (
    <GenericForm
      fields={fields}
      initialValues={defaultValues}
      onSubmit={onSubmit}
      isProcessing={isProcessing}
      submitButtonText={initialValues ? 'Update Model' : 'Add Model'}
      layout="grid"
    />
  );
};


