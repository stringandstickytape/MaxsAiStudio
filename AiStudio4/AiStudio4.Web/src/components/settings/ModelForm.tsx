// src/components/settings/ModelForm.tsx
import React from 'react';
import { Model, ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';

interface ModelFormProps {
    providers: ServiceProvider[];
    initialValues?: Model;
    onSubmit: (data: any) => Promise<void>;
    isProcessing: boolean;
}

export const ModelForm: React.FC<ModelFormProps> = ({
    providers,
    initialValues,
    onSubmit,
    isProcessing
}) => {
    // Create provider options for select field
    const providerOptions = providers.map(provider => ({
        value: provider.guid,
        label: provider.friendlyName
    }));

    // Define form fields
    const fields: FormFieldDefinition[] = [
        {
            name: 'friendlyName',
            label: 'Friendly Name',
            type: 'text',
            placeholder: 'e.g., GPT-4 Turbo',
            colSpan: 1
        },
        {
            name: 'modelName',
            label: 'Model Name',
            type: 'text',
            placeholder: 'e.g., gpt-4-turbo',
            colSpan: 1
        },
        {
            name: 'providerGuid',
            label: 'Service Provider',
            type: 'select',
            options: providerOptions,
            colSpan: 2
        },
        {
            name: 'input1MTokenPrice',
            label: 'Input Token Price (per 1M)',
            type: 'number',
            placeholder: '0.00',
            step: '0.01',
            min: 0,
            colSpan: 1
        },
        {
            name: 'output1MTokenPrice',
            label: 'Output Token Price (per 1M)',
            type: 'number',
            placeholder: '0.00',
            step: '0.01',
            min: 0,
            colSpan: 1
        },
        {
            name: 'userNotes',
            label: 'Notes',
            type: 'text',
            placeholder: 'Any additional notes about this model',
            colSpan: 2
        },
        {
            name: 'additionalParams',
            label: 'Additional Parameters (JSON)',
            type: 'text',
            placeholder: '{"temperature": 0.7}',
            description: 'Optional JSON parameters to include with requests to this model',
            colSpan: 2
        },
        {
            name: 'color',
            label: 'Color',
            type: 'color',
            colSpan: 1
        },
        {
            name: 'starred',
            label: 'Mark as Favorite',
            type: 'checkbox',
            description: 'Prioritize this model in selection lists',
            colSpan: 1
        },
        {
            name: 'supportsPrefill',
            label: 'Supports Prefill',
            type: 'checkbox',
            description: 'This model supports prefilling content',
            colSpan: 2
        }
    ];

    // Use default values if initialValues is not provided
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
        supportsPrefill: false
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