import React from 'react';
import { ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';

interface ServiceProviderFormProps {
  initialValues?: ServiceProvider;
  onSubmit: (data: any) => Promise<void>;
  isProcessing: boolean;
}

export const ServiceProviderForm: React.FC<ServiceProviderFormProps> = ({ initialValues, onSubmit, isProcessing }) => {
  const fields: FormFieldDefinition[] = [
    {
      name: 'friendlyName',
      label: 'Friendly Name',
      type: 'text',
      placeholder: 'e.g., OpenAI',
      colSpan: 1,
    },
    {
      name: 'serviceName',
      label: 'Service Name',
      type: 'text',
      placeholder: 'e.g., OpenAI',
      description: 'Internal service name (e.g., OpenAI, Claude, Gemini)',
      colSpan: 1,
    },
    {
      name: 'url',
      label: 'API URL',
      type: 'text',
      placeholder: 'https://api.example.com/v1/chat/completions',
      colSpan: 2,
    },
    {
      name: 'apiKey',
      label: 'API Key',
      type: 'password',
      placeholder: 'Your API key',
      description: 'Your API key will be stored securely',
      colSpan: 2,
    },
    {
      name: 'iconName',
      label: 'Provider Icon',
      type: 'icon',
      description: 'Select an icon for this provider',
      colSpan: 2,
    },
  ];

  const defaultValues = initialValues || {
    guid: '',
    url: '',
    apiKey: '',
    friendlyName: '',
    serviceName: '',
    iconName: '',
  };

  const handleSubmit = async (data: any) => {
    await onSubmit(data);
  };

  return (
    <GenericForm
      fields={fields}
      initialValues={defaultValues}
      onSubmit={handleSubmit}
      isProcessing={isProcessing}
      submitButtonText={initialValues ? 'Update Provider' : 'Add Provider'}
      layout="grid"
    />
  );
};