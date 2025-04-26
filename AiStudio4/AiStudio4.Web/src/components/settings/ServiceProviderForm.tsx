import React from 'react';
import { ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';
import { IconSelector } from './IconSelector';

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
  ];

  const [iconName, setIconName] = React.useState(initialValues?.iconName || '');
  
  const defaultValues = initialValues || {
    guid: '',
    url: '',
    apiKey: '',
    friendlyName: '',
    serviceName: '',
    iconName: '',
  };

  const handleSubmit = async (data: any) => {
    // Combine form data with the selected icon name
    await onSubmit({
      ...data,
      iconName,
    });
  };

  return (
    <div className="space-y-6">
      <GenericForm
        fields={fields}
        initialValues={defaultValues}
        onSubmit={handleSubmit}
        isProcessing={isProcessing}
        submitButtonText={initialValues ? 'Update Provider' : 'Add Provider'}
        layout="grid"
      />
      
      {/* Icon Selector */}
      <div className="space-y-2">
        <label className="form-label text-gray-200">Provider Icon</label>
        <p className="form-description text-gray-400 mb-2">Select an icon for this provider</p>
        <IconSelector 
          value={iconName} 
          onChange={setIconName} 
          disabled={isProcessing} 
        />
      </div>
    </div>
  );
};