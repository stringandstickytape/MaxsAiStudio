
import React, { useState } from 'react';
import { Model, ServiceProvider } from '@/types/settings';
import { GenericForm, FormFieldDefinition } from '@/components/common/GenericForm';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';

interface ModelFormProps {
  providers: ServiceProvider[];
  initialValues?: Model;
  onSubmit: (data: any) => Promise<void>;
  isProcessing: boolean;
}

export const ModelForm: React.FC<ModelFormProps> = ({ providers, initialValues, onSubmit, isProcessing }) => {
  // State for tiered pricing toggle
  const [showTieredPricing, setShowTieredPricing] = useState(!!initialValues?.priceBoundary);
  
  const providerOptions = providers.map((provider) => ({
    value: provider.guid,
    label: provider.friendlyName,
  }));

  const fieldDefinitions = [
    ['friendlyName', 'Friendly Name', 'text', 'e.g., GPT-4 Turbo', undefined, undefined, undefined, 1],
    ['modelName', 'Model Name', 'text', 'e.g., gpt-4-turbo', undefined, undefined, undefined, 1],
    ['providerGuid', 'Service Provider', 'select', undefined, providerOptions, undefined, undefined, 2],
    ['input1MTokenPrice', 'Input Price/1M (< Boundary)', 'number', '0.00', undefined, '0.01', 0, 1],
    ['output1MTokenPrice', 'Output Price/1M (< Boundary)', 'number', '0.00', undefined, '0.01', 0, 1],
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
    [
      'requires1fTemp',
      'Requires 1.0 Temperature',
      'checkbox',
      undefined,
      undefined,
      undefined,
      'This model requires a temperature setting of 1.0 to function properly',
      2,
    ],
    [
      'allowsTopP',
      'Allows Top-P Sampling',
      'checkbox',
      undefined,
      undefined,
      undefined,
      'This model supports Top-P sampling.',
      2,
    ],
    [
      'reasoningEffort',
      'Reasoning Effort',
      'select',
      undefined,
      [
        { value: 'none', label: 'None' },
        { value: 'low', label: 'Low' },
        { value: 'medium', label: 'Medium' },
        { value: 'high', label: 'High' },
      ],
      undefined,
      'Set the reasoning effort level for this model (if supported by backend)',
      2,
    ],
    [
      'isTtsModel',
      'Enable Text-to-Speech (TTS)',
      'checkbox',
      undefined,
      undefined,
      undefined,
      'Check if this model should be used for Text-to-Speech generation.',
      2
    ],
    [
      'ttsVoiceName',
      'TTS Voice Name',
      'text',
      'e.g., Kore, Gemini, Calite',
      undefined,
      undefined,
      'The prebuilt voice name for TTS (e.g., Kore). Refer to Gemini documentation for available voices.',
      2
    ],
  ];

  // Tiered pricing fields (conditionally rendered)
  const tieredPricingFields = [
    ['priceBoundary', 'Token Boundary', 'number', 'e.g., 128000', undefined, '1000', 'The token limit where pricing changes', 2],
    ['inputPriceAboveBoundary', 'Input Price/1M (> Boundary)', 'number', '0.00', undefined, '0.01', 'Input price for requests above the boundary', 1],
    ['outputPriceAboveBoundary', 'Output Price/1M (> Boundary)', 'number', '0.00', undefined, '0.01', 'Output price for requests above the boundary', 1],
  ];

  // Combine base fields with tiered pricing fields if enabled
  const allFieldDefinitions = showTieredPricing 
    ? [...fieldDefinitions.slice(0, 5), ...tieredPricingFields, ...fieldDefinitions.slice(5)]
    : fieldDefinitions;

  const fields: FormFieldDefinition[] = allFieldDefinitions.map(
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
    priceBoundary: null,
    inputPriceAboveBoundary: null,
    outputPriceAboveBoundary: null,
    color: '#4f46e5',
    starred: false,
    supportsPrefill: false,
    requires1fTemp: false,
    allowsTopP: false,
    reasoningEffort: 'none',
    isTtsModel: false,
    ttsVoiceName: 'Kore',
  };

  // Handle form submission with tiered pricing logic
  const handleFormSubmit = async (data: any) => {
    const submissionData = { ...data };
    
    // If tiered pricing is disabled, set tiered fields to null
    if (!showTieredPricing) {
      submissionData.priceBoundary = null;
      submissionData.inputPriceAboveBoundary = null;
      submissionData.outputPriceAboveBoundary = null;
    }
    
    await onSubmit(submissionData);
  };

  return (
    <div className="space-y-6">
      {/* Tiered Pricing Toggle */}
      <div className="flex items-center space-x-2">
        <Checkbox
          id="tiered-pricing-toggle"
          checked={showTieredPricing}
          onCheckedChange={(checked) => setShowTieredPricing(Boolean(checked))}
        />
        <Label htmlFor="tiered-pricing-toggle">Enable Tiered Pricing</Label>
      </div>
      
      {/* Form */}
      <GenericForm
        fields={fields}
        initialValues={defaultValues}
        onSubmit={handleFormSubmit}
        isProcessing={isProcessing}
        submitButtonText={initialValues ? 'Update Model' : 'Add Model'}
        layout="grid"
      />
    </div>
  );
};


