// src/components/tools/ToolEditor.tsx
import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { AlertCircle, CheckCircle2 } from 'lucide-react';
import { 
  useAddToolMutation, 
  useUpdateToolMutation, 
  useValidateToolSchemaMutation 
} from '@/services/api/toolsApi';

interface ToolEditorProps {
  tool: Tool | null;
  onClose: () => void;
  categories: ToolCategory[];
}

export function ToolEditor({ tool, onClose, categories }: ToolEditorProps) {
  // RTK Query hooks
  const [addTool, { isLoading: isAddingTool }] = useAddToolMutation();
  const [updateTool, { isLoading: isUpdatingTool }] = useUpdateToolMutation();
  const [validateSchema, { isLoading: isValidating }] = useValidateToolSchemaMutation();
  
  // Local state
  const [name, setName] = useState(tool?.name || '');
  const [description, setDescription] = useState(tool?.description || '');
  const [schema, setSchema] = useState(tool?.schema || '{\n  "name": "tool_name",\n  "description": "Tool description",\n  "input_schema": {\n    "type": "object",\n    "properties": {\n      "param": {\n        "type": "string",\n        "description": "Parameter description"\n      }\n    },\n    "required": ["param"]\n  }\n}');
  const [schemaType, setSchemaType] = useState<'function' | 'custom' | 'template'>(tool?.schemaType as any || 'function');
  const [selectedCategories, setSelectedCategories] = useState<string[]>(tool?.categories || []);
  const [isValid, setIsValid] = useState<boolean | null>(null);
  const [validationMessage, setValidationMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleValidateSchema = async () => {
    try {
      const isValid = await validateSchema(schema).unwrap();
      setIsValid(isValid);
      setValidationMessage(isValid ? 'Schema is valid' : 'Schema is invalid');
    } catch (error) {
      setIsValid(false);
      setValidationMessage(`Validation error: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  const handleCategoryToggle = (categoryId: string) => {
    setSelectedCategories(prev => 
      prev.includes(categoryId)
        ? prev.filter(id => id !== categoryId)
        : [...prev, categoryId]
    );
  };

  const handleSubmit = async () => {
    // Validate form
    if (!name.trim()) {
      alert('Tool name is required');
      return;
    }

    if (!description.trim()) {
      alert('Tool description is required');
      return;
    }

    setIsSubmitting(true);
    try {
      // Validate schema one last time
      let isSchemaValid;
      try {
        isSchemaValid = await validateSchema(schema).unwrap();
      } catch (error) {
        console.error('Schema validation error:', error);
        isSchemaValid = false;
      }
      
      if (!isSchemaValid) {
        if (!confirm('Schema validation failed. Save anyway?')) {
          setIsSubmitting(false);
          return;
        }
      }

      const toolData: any = {
        name,
        description,
        schema,
        schemaType,
        categories: selectedCategories,
        lastModified: new Date().toISOString()
      };

      if (tool) {
        toolData.guid = tool.guid;
        toolData.isBuiltIn = tool.isBuiltIn;
        await updateTool(toolData).unwrap();
      } else {
        await addTool(toolData).unwrap();
      }

      onClose();
    } catch (error) {
      console.error('Error saving tool:', error);
      alert(`Error saving tool: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  // Determine loading state
  const isLoading = isValidating || isAddingTool || isUpdatingTool || isSubmitting;

  return (
    <div className="space-y-4">
      <div>
        <Label htmlFor="tool-name">Name</Label>
        <Input
          id="tool-name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Tool name"
          className="bg-gray-800 border-gray-700 text-gray-100"
          disabled={isLoading}
        />
      </div>

      <div>
        <Label htmlFor="tool-description">Description</Label>
        <Input
          id="tool-description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Tool description"
          className="bg-gray-800 border-gray-700 text-gray-100"
          disabled={isLoading}
        />
      </div>

      <div>
        <Label>Schema Type</Label>
        <RadioGroup 
          value={schemaType} 
          onValueChange={(value) => setSchemaType(value as 'function' | 'custom' | 'template')}
          className="flex space-x-4 mt-2"
          disabled={isLoading}
        >
          <div className="flex items-center space-x-2">
            <RadioGroupItem 
              value="function" 
              id="schema-function" 
              className="border-gray-500 text-white data-[state=checked]:bg-blue-600 data-[state=checked]:border-blue-600"
              disabled={isLoading}
            />
            <Label htmlFor="schema-function" className="cursor-pointer text-gray-100">Function</Label>
          </div>
          <div className="flex items-center space-x-2">
            <RadioGroupItem 
              value="custom" 
              id="schema-custom" 
              className="border-gray-500 text-white data-[state=checked]:bg-blue-600 data-[state=checked]:border-blue-600"
              disabled={isLoading}
            />
            <Label htmlFor="schema-custom" className="cursor-pointer text-gray-100">Custom</Label>
          </div>
          <div className="flex items-center space-x-2">
            <RadioGroupItem 
              value="template" 
              id="schema-template" 
              className="border-gray-500 text-white data-[state=checked]:bg-blue-600 data-[state=checked]:border-blue-600"
              disabled={isLoading}
            />
            <Label htmlFor="schema-template" className="cursor-pointer text-gray-100">Template</Label>
          </div>
        </RadioGroup>
      </div>

      <div>
        <Label htmlFor="tool-schema">Schema</Label>
        <div className="relative">
          <Textarea
            id="tool-schema"
            value={schema}
            onChange={(e) => setSchema(e.target.value)}
            placeholder="Enter JSON schema..."
            className="min-h-[200px] font-mono text-sm bg-gray-800 border-gray-700 text-gray-100"
            disabled={isLoading}
          />
          {isValid !== null && (
            <div className={`mt-2 flex items-center space-x-2 ${isValid ? 'text-green-400' : 'text-red-400'}`}>
              {isValid ? (
                <CheckCircle2 className="h-4 w-4" />
              ) : (
                <AlertCircle className="h-4 w-4" />
              )}
              <span>{validationMessage}</span>
            </div>
          )}
        </div>
      </div>

      <div>
        <Label>Categories</Label>
        <div className="mt-2 flex flex-wrap gap-2">
          {categories.map(category => (
            <Badge
              key={category.id}
              variant={selectedCategories.includes(category.id) ? "default" : "outline"}
              className={`cursor-pointer ${selectedCategories.includes(category.id) ? 'bg-blue-600' : 'bg-gray-800 hover:bg-gray-700'} ${isLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
              onClick={() => !isLoading && handleCategoryToggle(category.id)}
            >
              {category.name}
            </Badge>
          ))}
        </div>
      </div>

      <div className="flex justify-end space-x-3 pt-4">
        <Button 
          variant="outline" 
          onClick={handleValidateSchema}
          disabled={isLoading}
          className="bg-gray-800 border-gray-700"
        >
          {isValidating ? 'Validating...' : 'Validate'}
        </Button>
        <Button 
          variant="outline" 
          onClick={onClose} 
          className="bg-gray-800 border-gray-700"
          disabled={isLoading}
        >
          Cancel
        </Button>
        <Button 
          onClick={handleSubmit}
          disabled={isLoading}
        >
          {isLoading ? (
            <span className="flex items-center">
              <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              {tool ? 'Updating...' : 'Creating...'}
            </span>
          ) : (
            tool ? 'Update' : 'Create'
          )}
        </Button>
      </div>
    </div>
  );
}