import React, { useEffect, useState } from 'react';
import { useForm, Controller, FieldValues, FieldErrors } from 'react-hook-form';
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

// Custom form components to replace shadcn/ui form components
const FormLabel = ({ children, className = '', ...props }: React.LabelHTMLAttributes<HTMLLabelElement>) => (
  <label className={`block text-sm font-medium mb-1 ${className}`} {...props}>
    {children}
  </label>
);

const FormDescription = ({ children, className = '', ...props }: React.HTMLAttributes<HTMLParagraphElement>) => (
  <p className={`text-sm text-gray-400 mt-1 ${className}`} {...props}>
    {children}
  </p>
);

const FormMessage = ({ children, className = '', ...props }: React.HTMLAttributes<HTMLSpanElement>) => (
  children ? (
    <span className={`text-sm text-red-400 mt-1 block ${className}`} {...props}>
      {children}
    </span>
  ) : null
);

interface FormItemProps extends React.HTMLAttributes<HTMLDivElement> {
  isCheckbox?: boolean;
}

const FormItem = ({ children, className = '', isCheckbox = false, ...props }: FormItemProps) => (
  <div 
    className={`${isCheckbox ? 'flex flex-row items-start space-x-3 space-y-0 p-4 border rounded-md' : 'mb-4'} ${className}`}
    {...props}
  >
    {children}
  </div>
);

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

  const { control, handleSubmit: rhfHandleSubmit, formState: { errors }, reset } = useForm({
    defaultValues: initialValues || getDefaultValues(),
  });

  useEffect(() => {
    if (initialValues) {
      reset(initialValues);
    }
  }, [initialValues, reset]);

  const handleFormSubmit = async (data: any) => {
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
    backgroundColor: 'var(--global-background-color, transparent)',
    color: 'var(--global-text-color, inherit)',
    fontFamily: 'var(--global-font-family, inherit)',
    fontSize: 'var(--global-font-size, inherit)',
    borderRadius: 'var(--global-border-radius, inherit)',
    borderColor: 'var(--global-border-color, inherit)',
    boxShadow: 'var(--global-box-shadow, none)',
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

  // Common form field wrapper component
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
  }) => {
    const fieldError = errors[field.name];
    
    return (
      <FormItem 
        className={className}
        isCheckbox={isCheckbox}
        style={isCheckbox ? {
          backgroundColor: 'var(--global-background-color, rgba(31, 41, 55, 0.5))',
          color: 'var(--global-text-color, inherit)',
          borderColor: 'var(--global-border-color, #374151)',
          borderRadius: 'var(--global-border-radius, 0.375rem)',
        } : {}}
      >
        {children}
        {!isCheckbox && field.description && (
          <FormDescription 
            className={formDescriptionClass}
            style={{
              color: 'var(--global-secondary-color, inherit)'
            }}
          >
            {field.description}
          </FormDescription>
        )}
        <FormMessage>
          {fieldError?.message as string}
        </FormMessage>
      </FormItem>
    );
  };

  // Icon field component
  const IconFormField = ({ field }: { field: FormFieldDefinition }) => {
    return (
      <Controller
        control={control}
        name={field.name}
        rules={{ required: field.required ? 'This field is required' : false }}
        render={({ field: formField }) => (
          <FormItem>
            <FormLabel className={formLabelClass} style={{ color: 'var(--global-text-color, inherit)' }}>
              {field.label}
            </FormLabel>
            <FormDescription className={formDescriptionClass}>{field.description}</FormDescription>
            <div>
              <IconSelector
                value={formField.value}
                onChange={formField.onChange}
                disabled={isProcessing}
              />
            </div>
            <FormMessage>{errors[field.name]?.message as string}</FormMessage>
          </FormItem>
        )}
      />
    );
  };

  const renderField = (field: FormFieldDefinition) => {
    switch (field.type) {
      case 'icon':
        return <IconFormField key={field.name} field={field} />;
        
      case 'textarea':
        return (
          <Controller
            key={field.name}
            control={control}
            name={field.name}
            rules={{ required: field.required ? 'This field is required' : false }}
            render={({ field: formField }) => (
              <CommonFormField field={field}>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--global-text-color, inherit)'
                  }}
                >
                  {field.label}
                </FormLabel>
                <div>
                  <Textarea
                    placeholder={field.placeholder}
                    {...formField}
                    className="input-base"
                    style={{
                      backgroundColor: 'var(--global-background-color, inherit)',
                      color: 'var(--global-text-color, inherit)',
                      borderColor: 'var(--global-border-color, inherit)',
                      borderRadius: 'var(--global-border-radius, inherit)',
                    }}
                    disabled={isProcessing}
                    onChange={(e) => {
                      formField.onChange(e);
                      field.onChange?.(e.target.value);
                    }}
                  />
                </div>
              </CommonFormField>
            )}
          />
        );

      case 'checkbox':
        return (
          <Controller
            key={field.name}
            control={control}
            name={field.name}
            rules={{ required: field.required ? 'This field is required' : false }}
            render={({ field: formField }) => (
              <CommonFormField field={field} isCheckbox={true}>
                <div>
                  <Checkbox
                    checked={formField.value}
                    onCheckedChange={(checked) => {
                      formField.onChange(checked);
                      field.onChange?.(checked);
                    }}
                    className="data-[state=checked]:bg-blue-600 border-gray-500"
                    style={{
                      borderColor: 'var(--global-border-color, inherit)',
                      backgroundColor: 'var(--global-background-color, transparent)',
                      '--checkbox-checked-bg': 'var(--global-primary-color, #2563eb)'
                    } as React.CSSProperties}
                    disabled={isProcessing}
                  />
                </div>
                <div className="space-y-1 leading-none">
                  <FormLabel 
                    className={formLabelClass}
                    style={{
                      color: 'var(--global-text-color, inherit)'
                    }}
                  >
                    {field.label}
                  </FormLabel>
                  {field.description && (
                    <FormDescription className={formDescriptionClass}>{field.description}</FormDescription>
                  )}
                </div>
              </CommonFormField>
            )}
          />
        );

      case 'select':
        return (
          <Controller
            key={field.name}
            control={control}
            name={field.name}
            rules={{ required: field.required ? 'This field is required' : false }}
            render={({ field: formField }) => (
              <CommonFormField field={field}>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--global-text-color, inherit)'
                  }}
                >
                  {field.label}
                </FormLabel>
                <div>
                  <Select
                    onValueChange={(value) => {
                      formField.onChange(value);
                      field.onChange?.(value);
                    }}
                    defaultValue={formField.value}
                    disabled={isProcessing}
                  >
                    <SelectTrigger 
                      className="input-base"
                      style={{
                        backgroundColor: 'var(--global-background-color, inherit)',
                        color: 'var(--global-text-color, inherit)',
                        borderColor: 'var(--global-border-color, inherit)',
                        borderRadius: 'var(--global-border-radius, inherit)',
                      }}
                    >
                      <SelectValue placeholder={field.placeholder || `Select a ${field.label.toLowerCase()}`} />
                    </SelectTrigger>
                    <SelectContent 
                      className="border rounded-md shadow-md" 
                      style={{
                        backgroundColor: 'var(--global-background-color, #1f2937)',
                        color: 'var(--global-text-color, #f9fafb)',
                        borderColor: 'var(--global-border-color, #374151)',
                        borderRadius: 'var(--global-border-radius, 0.375rem)',
                        boxShadow: 'var(--global-box-shadow, 0 4px 6px -1px rgba(0, 0, 0, 0.1))',
                        fontFamily: 'var(--global-font-family, inherit)',
                        fontSize: 'var(--global-font-size, inherit)'
                      }}
                    >
                      {field.options?.map((option) => (
                        <SelectItem
                          key={option.value}
                          value={option.value}
                          className="focus:bg-gray-700 focus:text-gray-100"
                          style={{
                            backgroundColor: 'transparent',
                            color: 'inherit'
                          }}
                        >
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </CommonFormField>
            )}
          />
        );

      case 'color':
        return (
          <Controller
            key={field.name}
            control={control}
            name={field.name}
            rules={{ required: field.required ? 'This field is required' : false }}
            render={({ field: formField }) => (
              <CommonFormField field={field}>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--global-text-color, inherit)'
                  }}
                >
                  {field.label}
                </FormLabel>
                <div className="flex gap-2">
                  <div>
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
                  </div>
                  <Input
                    value={formField.value}
                    onChange={(e) => {
                      formField.onChange(e);
                      field.onChange?.(e.target.value);
                    }}
                    className="flex-1 bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500"
                    style={{
                      backgroundColor: 'var(--global-background-color, inherit)',
                      color: 'var(--global-text-color, inherit)',
                      borderColor: 'var(--global-border-color, inherit)',
                      borderRadius: 'var(--global-border-radius, inherit)',
                    }}
                    disabled={isProcessing}
                  />
                </div>
              </CommonFormField>
            )}
          />
        );
      
      case 'number':
        return (
          <Controller
            key={field.name}
            control={control}
            name={field.name}
            rules={{
              required: field.required ? 'This field is required' : false,
              min: field.min !== undefined ? { value: field.min, message: `Minimum value is ${field.min}` } : undefined,
              max: field.max !== undefined ? { value: field.max, message: `Maximum value is ${field.max}` } : undefined,
            }}
            render={({ field: formField }) => (
              <CommonFormField field={field}>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--global-text-color, inherit)'
                  }}
                >
                  {field.label}
                </FormLabel>
                <div>
                  <Input
                    type="number"
                    step={field.step || '1'}
                    min={field.min}
                    max={field.max}
                    placeholder={field.placeholder}
                    {...formField}
                    onChange={(e) => {
                      const value = parseFloat(e.target.value);
                      formField.onChange(e);
                      field.onChange?.(value);
                    }}
                    className={getInputStyles('number')}
                    style={{
                      backgroundColor: 'var(--global-background-color, inherit)',
                      color: 'var(--global-text-color, inherit)',
                      borderColor: 'var(--global-border-color, inherit)',
                      borderRadius: 'var(--global-border-radius, inherit)',
                    }}
                    disabled={isProcessing}
                  />
                </div>
              </CommonFormField>
            )}
          />
        );
      
      
      default:
        return (
          <Controller
            key={field.name}
            control={control}
            name={field.name}
            rules={{ required: field.required ? 'This field is required' : false }}
            render={({ field: formField }) => (
              <CommonFormField field={field}>
                <FormLabel 
                  className={formLabelClass}
                  style={{
                    color: 'var(--global-text-color, inherit)'
                  }}
                >
                  {field.label}
                </FormLabel>
                <div>
                  <Input
                    type={field.type}
                    placeholder={field.placeholder}
                    {...formField}
                    onChange={(e) => {
                      formField.onChange(e);
                      field.onChange?.(e.target.value);
                    }}
                    className={getInputStyles(field.type)}
                    style={{
                      backgroundColor: 'var(--global-background-color, inherit)',
                      color: 'var(--global-text-color, inherit)',
                      borderColor: 'var(--global-border-color, inherit)',
                      borderRadius: 'var(--global-border-radius, inherit)',
                    }}
                    disabled={isProcessing}
                  />
                </div>
              </CommonFormField>
            )}
          />
        );
    }
  };

  return (
    <div className="h-full flex-col-full GenericForm" style={baseStyleObject}>
      {title && (
        <div className="flex-none flex-between mb-4">
          <h2 className="text-xl font-semibold" style={{ color: 'var(--global-text-color, #f9fafb)' }}>{title}</h2>
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

      <div className="flex-1 overflow-hidden">
        <form onSubmit={rhfHandleSubmit(handleFormSubmit)} className={`flex flex-col h-full ${className || ''}`}>
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
      </div>
    </div>
  );
}

// Define themeable properties for the GenericForm component
export const themeableProps = {
};