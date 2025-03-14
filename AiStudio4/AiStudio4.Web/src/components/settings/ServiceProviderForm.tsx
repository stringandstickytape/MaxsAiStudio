// src/components/settings/ServiceProviderForm.tsx
import React from 'react';
import { ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';

interface ServiceProviderFormProps {
    initialValues?: ServiceProvider;
    onSubmit: (data: any) => Promise<void>;
    isProcessing: boolean;
}

export const ServiceProviderForm: React.FC<ServiceProviderFormProps> = ({
    initialValues,
    onSubmit,
    isProcessing
}) => {
    // Define form fields
    const fields: FormFieldDefinition[] = [
        {
            name: 'friendlyName',
            label: 'Friendly Name',
            type: 'text',
            placeholder: 'e.g., OpenAI',
            colSpan: 1
        },
        {
            name: 'serviceName',
            label: 'Service Name',
            type: 'text',
            placeholder: 'e.g., OpenAI',
            description: 'Internal service name (e.g., OpenAI, Claude, Gemini)',
            colSpan: 1
        },
        {
            name: 'url',
            label: 'API URL',
            type: 'text',
            placeholder: 'https://api.example.com/v1/chat/completions',
            colSpan: 2
        },
        {
            name: 'apiKey',
            label: 'API Key',
            type: 'password',
            placeholder: 'Your API key',
            description: 'Your API key will be stored securely',
            colSpan: 2
        }
    ];

    // Use default values if initialValues is not provided
    const defaultValues = initialValues || {
        guid: '',
        url: '',
        apiKey: '',
        friendlyName: '',
        serviceName: ''
    };

    return (
        <GenericForm
            fields={fields}
            initialValues={defaultValues}
            onSubmit={onSubmit}
            isProcessing={isProcessing}
            submitButtonText={initialValues ? 'Update Provider' : 'Add Provider'}
            layout="grid"
        />
    );
};