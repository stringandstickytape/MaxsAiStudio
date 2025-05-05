import React, { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Form, FormField, FormItem, FormLabel, FormControl, FormDescription, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { Save, X } from 'lucide-react';
import { v4 as uuidv4 } from 'uuid';
import { IconSelector } from '../settings/IconSelector';

export type FieldType = 'text' | 'password' | 'textarea' | 'number' | 'checkbox' | 'select' | 'color' | 'icon';

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
  title?: string;
}

export function GenericForm({
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
  title,
}: GenericFormProps) {
  const [error, setError] = useState<string | null>(null);
  
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
    setError(null);
    try {
      if (generateUuid && !data[uuidField]) {
        data[uuidField] = uuidv4();
      }
      await onSubmit(data);
    } catch (err: any) {
      console.error('Error in form submission:', err);
      setError(err?.message || 'Failed to save data');
    }
  };

  
  const baseStyles = 'bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500';
  const colorInputStyles = 'w-12 h-8 p-1 bg-transparent';
  const formLabelClass = 'form-label text-gray-200';
  const formDescriptionClass = 'form-description text-gray-400';
  const formMessageClass = 'text-red-400';
  
  // Define base style object with global CSS variables
  const baseStyleObject = {
    backgroundColor: 'var(--genericform-bg, var(--global-background-color, transparent))',
    color: 'var(--genericform-text-color, var(--global-text-color, inherit))',
    fontFamily: 'var(--genericform-font-family, var(--global-font-family, inherit))',
    fontSize: 'var(--genericform-font-size, var(--global-font-size, inherit))',
    borderRadius: 'var(--genericform-border-radius, var(--global-border-radius, inherit))',
    borderColor: 'var(--genericform-border-color, var(--global-border-color, inherit))',
    boxShadow: 'var(--genericform-box-shadow, var(--global-box-shadow, none))',
  };
  
  const getInputStyles = (type: FieldType) => {
    return type === 'color' ? colorInputStyles : baseStyles;
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

  
  const handleChange = (formField: any, fieldDef: FormFieldDefinition, value: any) => {
    formField.onChange(value);
    fieldDef.onChange?.(value);
  };

  
  const CommonFormField = ({
    field,
    children,
    className = '',
    isCheckbox = false
  }: {
    field: FormFieldDefinition;
    children: React.ReactNode;
    className?: string;
    isCheckbox?: boolean;
  }) => (
    <FormField
      key={field.name}
      control={form.control}
      name={field.name}
      render={({ field: formField }) => (
        <FormItem 
          className={isCheckbox ? "flex flex-row items-start space-x-3 space-y-0 p-4 border rounded-md" : className}
          style={isCheckbox ? {
            backgroundColor: 'var(--genericform-checkbox-bg, var(--global-background-color, rgba(31, 41, 55, 0.5)))',
            color: 'var(--genericform-checkbox-text-color, var(--global-text-color, inherit))',
            borderColor: 'var(--genericform-checkbox-border-color, var(--global-border-color, #374151))',
            borderRadius: 'var(--genericform-checkbox-border-radius, var(--global-border-radius, 0.375rem))',
          } : {}}
        >
          {children(formField)}
          {!isCheckbox && field.description && (
            <FormDescription 
              className={formDescriptionClass}
              style={{
                color: 'var(--genericform-description-color, var(--global-secondary-color, inherit))'
              }}
            >
              {field.description}
            </FormDescription>
          )}
          <FormMessage 
            className={formMessageClass} 
            style={{
              color: 'var(--genericform-error-color, #f87171)'
            }}
          />
        </FormItem>
      )}
    />
  );

  // --- ICON FIELD SUPPORT ---
  const IconFormField = ({ field }: { field: FormFieldDefinition }) => (
    <FormField
      key={field.name}
      control={form.control}
      name={field.name}
      render={({ field: formField }) => (
        <FormItem>
          <FormLabel className={formLabelClass}>{field.label}</FormLabel>
          <FormDescription className={formDescriptionClass}>{field.description}</FormDescription>
          <FormControl>
            <IconSelector
              value={formField.value}
              onChange={formField.onChange}
              disabled={isProcessing}
            />
          </FormControl>
          <FormMessage className={formMessageClass} />
        </FormItem>
      )}
    />
  );

  const renderField = (field: FormFieldDefinition) => {
    switch (field.type) {
      case 'icon':
        return <IconFormField field={field} />;
      case 'textarea':
        return (
          <CommonFormField field={field}>
            {(formField) => (
              <>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--genericform-label-color, var(--global-text-color, inherit))'
                  }}
                >
                  {field.label}
                </FormLabel>
                <FormControl>
                  <Textarea
                    placeholder={field.placeholder}
                    {...formField}
                    className="input-base"
                    style={{
                      backgroundColor: 'var(--genericform-textarea-bg, var(--global-background-color, inherit))',
                      color: 'var(--genericform-textarea-text-color, var(--global-text-color, inherit))',
                      borderColor: 'var(--genericform-textarea-border-color, var(--global-border-color, inherit))',
                      borderRadius: 'var(--genericform-textarea-border-radius, var(--global-border-radius, inherit))',
                    }}
                    disabled={isProcessing}
                    onChange={(e) => handleChange(formField, field, e.target.value)}
                  />
                </FormControl>
              </>
            )}
          </CommonFormField>
        );

      case 'checkbox':
        return (
          <CommonFormField field={field} isCheckbox={true}>
            {(formField) => (
              <>
                <FormControl>
                  <Checkbox
                    checked={formField.value}
                    onCheckedChange={(checked) => handleChange(formField, field, checked)}
                    className="data-[state=checked]:bg-blue-600 border-gray-500"
                    style={{
                      borderColor: 'var(--genericform-checkbox-border-color, var(--global-border-color, inherit))',
                      backgroundColor: 'var(--genericform-checkbox-bg, var(--global-background-color, transparent))',
                      '--checkbox-checked-bg': 'var(--genericform-checkbox-checked-bg, var(--global-primary-color, #2563eb))'
                    } as React.CSSProperties}
                    disabled={isProcessing}
                  />
                </FormControl>
                <div className="space-y-1 leading-none">
                  <FormLabel 
                    className={formLabelClass}
                    style={{
                      color: 'var(--genericform-label-color, var(--global-text-color, inherit))'
                    }}
                  >
                    {field.label}
                  </FormLabel>
                  {field.description && (
                    <FormDescription className={formDescriptionClass}>{field.description}</FormDescription>
                  )}
                </div>
              </>
            )}
          </CommonFormField>
        );

      case 'select':
        return (
          <CommonFormField field={field}>
            {(formField) => (
              <>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--genericform-label-color, var(--global-text-color, inherit))'
                  }}
                >
                  {field.label}
                </FormLabel>
                <Select
                  onValueChange={(value) => handleChange(formField, field, value)}
                  defaultValue={formField.value}
                  disabled={isProcessing}
                >
                  <FormControl>
                    <SelectTrigger 
                      className="input-base"
                      style={{
                        backgroundColor: 'var(--genericform-select-bg, var(--global-background-color, inherit))',
                        color: 'var(--genericform-select-text-color, var(--global-text-color, inherit))',
                        borderColor: 'var(--genericform-select-border-color, var(--global-border-color, inherit))',
                        borderRadius: 'var(--genericform-select-border-radius, var(--global-border-radius, inherit))',
                      }}
                    >
                      <SelectValue placeholder={field.placeholder || `Select a ${field.label.toLowerCase()}`} />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent 
                    className="border rounded-md shadow-md" 
                    style={{
                      backgroundColor: 'var(--genericform-dropdown-bg, var(--global-background-color, #1f2937))',
                      color: 'var(--genericform-dropdown-text-color, var(--global-text-color, #f9fafb))',
                      borderColor: 'var(--genericform-dropdown-border-color, var(--global-border-color, #374151))',
                      borderRadius: 'var(--genericform-dropdown-border-radius, var(--global-border-radius, 0.375rem))',
                      boxShadow: 'var(--genericform-dropdown-box-shadow, var(--global-box-shadow, 0 4px 6px -1px rgba(0, 0, 0, 0.1)))',
                      fontFamily: 'var(--genericform-dropdown-font-family, var(--global-font-family, inherit))',
                      fontSize: 'var(--genericform-dropdown-font-size, var(--global-font-size, inherit))'
                    }}
                  >
                    {field.options?.map((option) => (
                      <SelectItem
                        key={option.value}
                        value={option.value}
                        className="focus:bg-gray-700 focus:text-gray-100"
                        style={{
                          backgroundColor: 'var(--genericform-dropdown-item-bg, transparent)',
                          color: 'var(--genericform-dropdown-item-text-color, inherit)'
                        }}
                      >
                        {option.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </>
            )}
          </CommonFormField>
        );

      case 'color':
        return (
          <CommonFormField field={field}>
            {(formField) => (
              <>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--genericform-label-color, var(--global-text-color, inherit))'
                  }}
                >
                  {field.label}
                </FormLabel>
                <div className="flex gap-2">
                  <FormControl>
                    <Input
                      type="color"
                      className={getInputStyles('color')}
                      {...formField}
                      disabled={isProcessing}
                      onChange={(e) => handleChange(formField, field, e.target.value)}
                    />
                  </FormControl>
                  <Input
                    value={formField.value}
                    onChange={(e) => handleChange(formField, field, e.target.value)}
                    className="flex-1 bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500"
                    style={{
                      backgroundColor: 'var(--genericform-input-bg, var(--global-background-color, inherit))',
                      color: 'var(--genericform-input-text-color, var(--global-text-color, inherit))',
                      borderColor: 'var(--genericform-input-border-color, var(--global-border-color, inherit))',
                      borderRadius: 'var(--genericform-input-border-radius, var(--global-border-radius, inherit))',
                    }}
                    disabled={isProcessing}
                  />
                </div>
              </>
            )}
          </CommonFormField>
        );
      
      case 'number':
        return (
          <CommonFormField field={field}>
            {(formField) => (
              <>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--genericform-label-color, var(--global-text-color, inherit))'
                  }}
                >
                  {field.label}
                </FormLabel>
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
                      handleChange(formField, field, value);
                    }}
                    className={getInputStyles('number')}
                    style={{
                      backgroundColor: 'var(--genericform-input-bg, var(--global-background-color, inherit))',
                      color: 'var(--genericform-input-text-color, var(--global-text-color, inherit))',
                      borderColor: 'var(--genericform-input-border-color, var(--global-border-color, inherit))',
                      borderRadius: 'var(--genericform-input-border-radius, var(--global-border-radius, inherit))',
                    }}
                    disabled={isProcessing}
                  />
                </FormControl>
              </>
            )}
          </CommonFormField>
        );
      
      
      default:
        return (
          <CommonFormField field={field}>
            {(formField) => (
              <>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--genericform-label-color, var(--global-text-color, inherit))'
                  }}
                >
                  {field.label}
                </FormLabel>
                <FormControl>
                  <Input
                    type={field.type}
                    placeholder={field.placeholder}
                    {...formField}
                    onChange={(e) => handleChange(formField, field, e.target.value)}
                    className={getInputStyles(field.type)}
                    style={{
                      backgroundColor: 'var(--genericform-input-bg, var(--global-background-color, inherit))',
                      color: 'var(--genericform-input-text-color, var(--global-text-color, inherit))',
                      borderColor: 'var(--genericform-input-border-color, var(--global-border-color, inherit))',
                      borderRadius: 'var(--genericform-input-border-radius, var(--global-border-radius, inherit))',
                    }}
                    disabled={isProcessing}
                  />
                </FormControl>
              </>
            )}
          </CommonFormField>
        );
    }
  };

  return (
    <div className="h-full flex-col-full GenericForm" style={baseStyleObject}>
      {title && (
        <div className="flex-none flex-between mb-4">
          <h2 className="text-xl font-semibold" style={{ color: 'var(--genericform-title-color, var(--global-text-color, #f9fafb))' }}>{title}</h2>
          {onCancel && (
            <Button
              variant="ghost"
              size="icon"
              onClick={onCancel}
              className="text-gray-400 hover:text-gray-100"
              disabled={isProcessing}
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
      )}

      {error && <div className="bg-red-950/30 text-red-400 p-3 rounded-md border border-red-800/50 mb-4">{error}</div>}

      <Form {...form} className="flex-1 overflow-hidden">
        <form onSubmit={form.handleSubmit(handleSubmit)} className={`flex flex-col h-full ${className || ''}`}>
          <div className="space-y-6 flex-1 overflow-y-auto pr-2">
            {renderFormFields()}
          </div>

          <div className="flex-none mt-6 space-x-3 flex justify-end">
            {onCancel && !title && (
              <Button
                type="button"
                onClick={onCancel}
                variant="outline"
                disabled={isProcessing}
                className="btn-secondary"
              >
                {cancelButtonText}
              </Button>
            )}
            <Button 
              type="submit" 
              disabled={isProcessing} 
              className="btn-primary"
            >
              {isProcessing ? (
                <span className="flex items-center gap-2">
                  <div className="animate-spin h-4 w-4 border-2 border-t-transparent border-white rounded-full" />
                  Processing...
                </span>
              ) : (
                <span className="flex items-center gap-2">
                  <Save className="h-4 w-4" />
                  {submitButtonText}
                </span>
              )}
            </Button>
          </div>
        </form>
      </Form>
    </div>
  );
}

// Define themeable properties for the GenericForm component
export const themeableProps = {
    backgroundColor: {
        cssVar: '--genericform-bg',
        description: 'Background color of the form',
        default: 'transparent',
    },
    textColor: {
        cssVar: '--genericform-text-color',
        description: 'Text color of the form',
        default: 'inherit',
    },
    selectBackgroundColor: {
        cssVar: '--genericform-select-bg',
        description: 'Background color of select inputs',
        default: 'inherit',
    },
    selectTextColor: {
        cssVar: '--genericform-select-text-color',
        description: 'Text color of select inputs',
        default: 'inherit',
    },
    dropdownBackgroundColor: {
        cssVar: '--genericform-dropdown-bg',
        description: 'Background color of dropdown menus',
        default: '#1f2937',
    },
    dropdownTextColor: {
        cssVar: '--genericform-dropdown-text-color',
        description: 'Text color of dropdown menus',
        default: '#f9fafb',
    },
    borderColor: {
        cssVar: '--genericform-border-color',
        description: 'Border color for form elements',
        default: 'inherit',
    },
    borderRadius: {
        cssVar: '--genericform-border-radius',
        description: 'Border radius for form elements',
        default: 'inherit',
    },
    boxShadow: {
        cssVar: '--genericform-box-shadow',
        description: 'Box shadow for form elements',
        default: 'none',
    },
};