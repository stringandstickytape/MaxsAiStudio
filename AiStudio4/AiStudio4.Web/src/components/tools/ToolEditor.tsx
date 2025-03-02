// src/components/tools/ToolEditor.tsx
import { useState, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Tool, ToolCategory } from '@/types/toolTypes';
import { addTool, updateTool } from '@/store/toolSlice';
import { AlertCircle, CheckCircle2 } from 'lucide-react';
import { ToolService } from '@/services/ToolService';

interface ToolEditorProps {
  tool: Tool | null;
  onClose: () => void;
  categories: ToolCategory[];
}

export function ToolEditor({ tool, onClose, categories }: ToolEditorProps) {
  const dispatch = useDispatch();
  const [name, setName] = useState(tool?.name || '');
  const [description, setDescription] = useState(tool?.description || '');
  const [schema, setSchema] = useState(tool?.schema || '{\n  "name": "tool_name",\n  "description": "Tool description",\n  "input_schema": {\n    "type": "object",\n    "properties": {\n      "param": {\n        "type": "string",\n        "description": "Parameter description"\n      }\n    },\n    "required": ["param"]\n  }\n}');
  const [schemaType, setSchemaType] = useState<'function' | 'custom' | 'template'>(tool?.schemaType as any || 'function');
  const [selectedCategories, setSelectedCategories] = useState<string[]>(tool?.categories || []);
  const [isValidating, setIsValidating] = useState(false);
  const [isValid, setIsValid] = useState<boolean | null>(null);
  const [validationMessage, setValidationMessage] = useState('');

  const handleValidateSchema = async () => {
    setIsValidating(true);
    try {
      const isValid = await ToolService.validateToolSchema(schema);
      setIsValid(isValid);
      setValidationMessage(isValid ? 'Schema is valid' : 'Schema is invalid');
    } catch (error) {
      setIsValid(false);
      setValidationMessage(`Validation error: ${error instanceof Error ? error.message : 'Unknown error'}`);
    } finally {
      setIsValidating(false);
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

    try {
      // Validate schema one last time
      const isSchemaValid = await ToolService.validateToolSchema(schema);
      if (!isSchemaValid) {
        if (!confirm('Schema validation failed. Save anyway?')) {
          return;
        }
      }

      const toolData: any = {
        name,
        description,
        schema,
        schemaType,
        categories: selectedCategories,
        lastModified: new Date().toISOString(),
      };

      if (tool) {
        toolData.guid = tool.guid;
        toolData.isBuiltIn = tool.isBuiltIn;
        dispatch(updateTool(toolData));
      } else {
        dispatch(addTool(toolData));
      }

      onClose();
    } catch (error) {
      console.error('Error saving tool:', error);
      alert(`Error saving tool: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

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
        />
      </div>

      <div>
        <Label>Schema Type</Label>
        <RadioGroup 
          value={schemaType} 
          onValueChange={(value) => setSchemaType(value as 'function' | 'custom' | 'template')}
          className="flex space-x-4 mt-2"
        >
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="function" id="schema-function" />
            <Label htmlFor="schema-function" className="cursor-pointer">Function</Label>
          </div>
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="custom" id="schema-custom" />
            <Label htmlFor="schema-custom" className="cursor-pointer">Custom</Label>
          </div>
          <div className="flex items-center space-x-2">
            <RadioGroupItem value="template" id="schema-template" />
            <Label htmlFor="schema-template" className="cursor-pointer">Template</Label>
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
              className={`cursor-pointer ${selectedCategories.includes(category.id) ? 'bg-blue-600' : 'bg-gray-800 hover:bg-gray-700'}`}
              onClick={() => handleCategoryToggle(category.id)}
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
          disabled={isValidating}
          className="bg-gray-800 border-gray-700"
        >
          {isValidating ? 'Validating...' : 'Validate'}
        </Button>
        <Button variant="outline" onClick={onClose} className="bg-gray-800 border-gray-700">
          Cancel
        </Button>
        <Button onClick={handleSubmit}>
          {tool ? 'Update' : 'Create'}
        </Button>
      </div>
    </div>
  );
}