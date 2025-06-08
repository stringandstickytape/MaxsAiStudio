import React, { useState, useEffect } from 'react';
import { ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';
import { createApiRequest } from '@/utils/apiUtils';

interface ServiceProviderFormProps {
  initialValues?: ServiceProvider;
  onSubmit: (data: any) => Promise<void>;
  isProcessing: boolean;
}

export const ServiceProviderForm: React.FC<ServiceProviderFormProps> = ({ initialValues, onSubmit, isProcessing }) => {
  const [availableServiceProviders, setAvailableServiceProviders] = useState<{value: string, label: string}[]>([]);
  const [isLoadingProviders, setIsLoadingProviders] = useState(false);

  useEffect(() => {
    const fetchAvailableServiceProviders = async () => {
      setIsLoadingProviders(true);
      try {
        // This endpoint needs to be implemented on the server side
        const response = await createApiRequest('/api/getAvailableServiceProviders', 'POST')({});
        if (response.serviceProviders) {
          const providers = response.serviceProviders.map((provider: string) => ({
            value: provider,
            label: provider
          }));
          setAvailableServiceProviders(providers);
        }
      } catch (error) {
        console.error('Failed to fetch available service providers:', error);
        // Fallback to some common providers if the API fails
        setAvailableServiceProviders([
          { value: 'OpenAI', label: 'OpenAI' },
          { value: 'Claude', label: 'Claude' },
          { value: 'Gemini', label: 'Gemini' },
          { value: 'Mistral', label: 'Mistral' },
          { value: 'Llama', label: 'Llama' },
          { value: 'Custom', label: 'Custom' }
        ]);
      } finally {
        setIsLoadingProviders(false);
      }
    };

    fetchAvailableServiceProviders();
  }, []);
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
      type: 'select',
      description: 'Select the service provider type',
      options: availableServiceProviders,
      placeholder: isLoadingProviders ? 'Loading...' : 'Select a service provider',
      required: true,
      colSpan: 1,
    },
    {
      name: 'url',
      label: 'API URL',
      type: 'text',
      placeholder: 'https://api.example.com/v1/chat/completions',
      colSpan: 2,
    },
    // Charging Strategy Dropdown
    {
      name: 'chargingStrategy',
      label: 'Charging Strategy',
      type: 'select',
      description: 'Select the cost calculation model for this provider.',
      options: [
        { value: 'NoCaching', label: 'No Caching (Standard)' },
        { value: 'Claude', label: 'Claude Caching Model' },
        { value: 'OpenAI', label: 'OpenAI Caching Model' },
        { value: 'Gemini', label: 'Gemini Caching Model' },
      ],
      placeholder: 'Select a charging strategy',
      required: true,
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
    chargingStrategy: 'Claude', // Default to Claude per backend default
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