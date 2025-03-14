// src/components/common/GenericForm.tsx
import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Form, FormField, FormItem, FormLabel, FormControl, FormDescription, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { v4 as uuidv4 } from 'uuid';

export type FieldType = 'text' | 'password' | 'textarea' | 'number' | 'checkbox' | 'select' | 'color';

export interface FormFieldDefinition {
  name: string;
  label: string;
  type: FieldType;
  placeholder?: string;
  description?: string;
  required?: boolean;
  min?: number;
  max?: number;
  step?: number;
  options?: { value: string; label: string }[]; 
  colSpan?: number; 
  onChange?: (value: any) => void; 
}

interface GenericFormProps {
  fields: FormFieldDefinition[];
  initialValues?: any;
  onSubmit: (data: any) => Promise<void>;
  isProcessing: boolean;
  submitButtonText?: string;
  cancelButtonText?: string;
  onCancel?: () => void;
  generateUuid?: boolean;
  uuidField?: string;
  layout?: 'single' | 'grid';
  className?: string;
}

export const GenericForm: React.FC<GenericFormProps> = ({
  fields,
  initialValues,
  onSubmit,
  isProcessing,
  submitButtonText = 'Submit',
  cancelButtonText = 'Cancel',
  onCancel,
  generateUuid = true,
  uuidField = 'guid',
  layout = 'single',
  className,
}) => {
  
  const getDefaultValues = () => {
    const defaultValues: Record<string, any> = {};

    fields.forEach((field) => {
      switch (field.type) {
        case 'checkbox':
          defaultValues[field.name] = false;
          break;
        case 'number':
          defaultValues[field.name] = 0;
          break;
        default:
          defaultValues[field.name] = '';
      }
    });

    return defaultValues;
  };

  const form = useForm({
    defaultValues: initialValues || getDefaultValues(),
  });

  useEffect(() => {
    if (initialValues) {
      form.reset(initialValues);
    }
  }, [initialValues, form]);

  const handleSubmit = async (data: any) => {
    if (generateUuid && !data[uuidField]) {
      data[uuidField] = uuidv4();
    }
    await onSubmit(data);
  };

  const getInputStyles = (type: FieldType) => {
    const baseStyles =
      'bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500';

    
    switch (type) {
      case 'color':
        return 'w-12 h-8 p-1 bg-transparent';
      default:
        return baseStyles;
    }
  };

  
  const renderFormFields = () => {
    if (layout === 'grid') {
      
      const rows: FormFieldDefinition[][] = [];
      let currentRow: FormFieldDefinition[] = [];

      fields.forEach((field) => {
        const span = field.colSpan || 1;

        
        if (span === 2 || currentRow.length + span > 2) {
          if (currentRow.length > 0) {
            rows.push([...currentRow]);
            currentRow = [];
          }
          rows.push([field]);
        } else {
          currentRow.push(field);
          if (currentRow.length === 2) {
            rows.push([...currentRow]);
            currentRow = [];
          }
        }
      });

      
      if (currentRow.length > 0) {
        rows.push(currentRow);
      }

      
      return rows.map((rowFields, rowIndex) => (
        <div
          key={`row-${rowIndex}`}
          className={`grid grid-cols-${rowFields.length === 1 && rowFields[0].colSpan === 2 ? '1' : '2'} gap-4`}
        >
          {rowFields.map((field) => renderField(field))}
        </div>
      ));
    }

    
    return fields.map((field) => renderField(field));
  };

  
  const renderField = (field: FormFieldDefinition) => {
    switch (field.type) {
      case 'textarea':
        return (
          <FormField
            key={field.name}
            control={form.control}
            name={field.name}
            render={({ field: formField }) => (
              <FormItem>
                <FormLabel className="text-gray-200">{field.label}</FormLabel>
                <FormControl>
                  <Textarea
                    placeholder={field.placeholder}
                    {...formField}
                    className="input-base"
                    disabled={isProcessing}
                    onChange={(e) => {
                      formField.onChange(e);
                      field.onChange?.(e.target.value);
                    }}
                  />
                </FormControl>
                {field.description && <FormDescription className="text-gray-400">{field.description}</FormDescription>}
                <FormMessage className="text-red-400" />
              </FormItem>
            )}
          />
        );

      case 'checkbox':
        return (
          <FormField
            key={field.name}
            control={form.control}
            name={field.name}
            render={({ field: formField }) => (
              <FormItem className="flex flex-row items-start space-x-3 space-y-0 p-4 border border-gray-700 rounded-md bg-gray-800/50">
                <FormControl>
                  <Checkbox
                    checked={formField.value}
                    onCheckedChange={(checked) => {
                      formField.onChange(checked);
                      field.onChange?.(checked);
                    }}
                    className="data-[state=checked]:bg-blue-600 border-gray-500"
                    disabled={isProcessing}
                  />
                </FormControl>
                <div className="space-y-1 leading-none">
                  <FormLabel className="form-label">{field.label}</FormLabel>
                  {field.description && (
                    <FormDescription className="form-description">{field.description}</FormDescription>
                  )}
                </div>
              </FormItem>
            )}
          />
        );

      case 'select':
        return (
          <FormField
            key={field.name}
            control={form.control}
            name={field.name}
            render={({ field: formField }) => (
              <FormItem>
                <FormLabel className="form-label">{field.label}</FormLabel>
                <Select
                  onValueChange={(value) => {
                    formField.onChange(value);
                    field.onChange?.(value);
                  }}
                  defaultValue={formField.value}
                  disabled={isProcessing}
                >
                  <FormControl>
                    <SelectTrigger className="input-base">
                      <SelectValue placeholder={field.placeholder || `Select a ${field.label.toLowerCase()}`} />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent className="bg-gray-800 border-gray-700 text-gray-100">
                    {field.options?.map((option) => (
                      <SelectItem
                        key={option.value}
                        value={option.value}
                        className="focus:bg-gray-700 focus:text-gray-100"
                      >
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {field.description && (
                  <FormDescription className="form-description">{field.description}</FormDescription>
                )}
                <FormMessage className="text-red-400" />
              </FormItem>
            )}
          />
        );

      case 'color':
        return (
          <FormField
            key={field.name}
            control={form.control}
            name={field.name}
            render={({ field: formField }) => (
              <FormItem>
                <FormLabel className="form-label">{field.label}</FormLabel>
                <div className="flex gap-2">
                  <FormControl>
                    <Input
                      type="color"
                      className={getInputStyles('color')}
                      {...formField}
                      disabled={isProcessing}
                      onChange={(e) => {
                        formField.onChange(e);
                        field.onChange?.(e.target.value);
                      }}
                    />
                  </FormControl>
                  <Input
                    value={formField.value}
                    onChange={(e) => {
                      formField.onChange(e);
                      field.onChange?.(e.target.value);
                    }}
                    className="flex-1 bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500"
                    disabled={isProcessing}
                  />
                </div>
                {field.description && (
                  <FormDescription className="form-description">{field.description}</FormDescription>
                )}
                <FormMessage className="text-red-400" />
              </FormItem>
            )}
          />
        );

      case 'number':
        return (
          <FormField
            key={field.name}
            control={form.control}
            name={field.name}
            render={({ field: formField }) => (
              <FormItem>
                <FormLabel className="form-label">{field.label}</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step={field.step || '1'}
                    min={field.min}
                    max={field.max}
                    placeholder={field.placeholder}
                    {...formField}
                    onChange={(e) => {
                      const value = parseFloat(e.target.value);
                      formField.onChange(value);
                      field.onChange?.(value);
                    }}
                    className={getInputStyles('number')}
                    disabled={isProcessing}
                  />
                </FormControl>
                {field.description && (
                  <FormDescription className="form-description">{field.description}</FormDescription>
                )}
                <FormMessage className="text-red-400" />
              </FormItem>
            )}
          />
        );

      
      default:
        return (
          <FormField
            key={field.name}
            control={form.control}
            name={field.name}
            render={({ field: formField }) => (
              <FormItem>
                <FormLabel className="form-label">{field.label}</FormLabel>
                <FormControl>
                  <Input
                    type={field.type}
                    placeholder={field.placeholder}
                    {...formField}
                    onChange={(e) => {
                      formField.onChange(e);
                      field.onChange?.(e.target.value);
                    }}
                    className={getInputStyles(field.type)}
                    disabled={isProcessing}
                  />
                </FormControl>
                {field.description && (
                  <FormDescription className="form-description">{field.description}</FormDescription>
                )}
                <FormMessage className="text-red-400" />
              </FormItem>
            )}
          />
        );
    }
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(handleSubmit)} className={`space-y-4 ${className || ''}`}>
        {renderFormFields()}

        <div className="flex justify-end gap-2 pt-4">
          {onCancel && (
            <Button
              type="button"
              onClick={onCancel}
              variant="outline"
              disabled={isProcessing}
              className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
            >
              {cancelButtonText}
            </Button>
          )}
          <Button type="submit" disabled={isProcessing} className="bg-blue-600 hover:bg-blue-700 text-white">
            {isProcessing ? 'Processing...' : submitButtonText}
          </Button>
        </div>
      </form>
    </Form>
  );
};


