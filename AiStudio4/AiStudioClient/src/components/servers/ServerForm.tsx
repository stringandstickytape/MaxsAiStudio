// AiStudioClient/src/components/servers/ServerForm.tsx
import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { McpServerDefinition, McpTool, useMcpServerStore } from '@/stores/useMcpServerStore';
import { AlertCircle } from 'lucide-react';

// Define themeable properties for the ServerForm component
export const themeableProps = {};

interface ServerFormProps {
  server?: McpServerDefinition;
  onSubmit: (server: McpServerDefinition) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function ServerForm({ server, onSubmit, onCancel, isSubmitting = false }: ServerFormProps) {
  const { fetchServerTools } = useMcpServerStore();
  const [availableTools, setAvailableTools] = useState<McpTool[]>([]);
  const [isLoadingTools, setIsLoadingTools] = useState(false);
  
  const [formData, setFormData] = useState<McpServerDefinition>({
    id: '',
    name: '',
    description: '',
    command: '',
    arguments: '',
    isEnabled: true,
    stdIo: true,
    env: {},
    categories: [],
    selectedTools: [],
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Initialize form with server data if editing
  useEffect(() => {
    if (server) {
      setFormData({
        ...server,
        // Ensure all required fields exist
        env: server.env || {},
        categories: server.categories || [],
        selectedTools: server.selectedTools || [],
      });
      
      // Fetch tools if in edit mode
      if (server.id) {
        const loadTools = async () => {
          setIsLoadingTools(true);
          try {
            const tools = await fetchServerTools(server.id);
            setAvailableTools(tools || []);
          } catch (error) {
            console.error("Failed to fetch tools for MCP server:", error);
            setAvailableTools([]);
          } finally {
            setIsLoadingTools(false);
          }
        };
        loadTools();
      } else {
        setAvailableTools([]); // Clear tools if not in edit mode or no server ID
      }
    } else {
      setFormData({
        id: '',
        name: '',
        description: '',
        command: '',
        arguments: '',
        isEnabled: true,
        stdIo: true,
        env: {},
        categories: [],
        selectedTools: [],
      });
      setAvailableTools([]); // Clear tools for 'add' mode
    }
  }, [server, fetchServerTools]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: value,
    }));

    // Clear error for this field if it exists
    if (errors[name]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[name];
        return newErrors;
      });
    }
  };

  const handleSwitchChange = (name: string, checked: boolean) => {
    setFormData((prev) => ({
      ...prev,
      [name]: checked,
    }));
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Name is required';
    }

    if (!formData.command.trim()) {
      newErrors.command = 'Command is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    onSubmit(formData);
  };

  return (
    <form onSubmit={handleSubmit} className="ServerForm space-y-4" style={{
      backgroundColor: 'transparent', // Inherits from DialogContent
      color: 'var(--global-text-color)',
      fontFamily: 'var(--global-font-family)',
      fontSize: 'var(--global-font-size)',
      // Removed border, borderRadius, boxShadow, padding as these should be on DialogContent
    }}>
      <div className="space-y-2">
        <Label htmlFor="name" style={{ color: 'var(--global-text-color)' }}>Name</Label>
        <Input
          id="name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="Server name"
          className={errors.name ? '' : ''} // Class for error state can be added if defined
          style={{
            backgroundColor: 'var(--global-background-color)', 
            color: 'var(--global-text-color)',
            borderColor: errors.name ? 'var(--global-destructive-color, var(--global-primary-color))' : 'var(--global-border-color)',
            borderRadius: 'var(--global-border-radius)'
          }}
          disabled={isSubmitting}
        />
        {errors.name && (
          <div className="text-sm flex items-center" style={{ color: 'var(--global-destructive-color, var(--global-primary-color))' }}>
            <AlertCircle className="h-4 w-4 mr-1" />
            {errors.name}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description" style={{ color: 'var(--global-text-color)' }}>Description</Label>
        <Textarea
          id="description"
          name="description"
          value={formData.description || ''}
          onChange={handleChange}
          placeholder="Server description"
          rows={2}
          disabled={isSubmitting}
          style={{
            backgroundColor: 'var(--global-background-color)',
            color: 'var(--global-text-color)',
            borderColor: 'var(--global-border-color)',
            borderRadius: 'var(--global-border-radius)'
          }}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="command" style={{ color: 'var(--global-text-color)' }}>Command</Label>
        <Input
          id="command"
          name="command"
          value={formData.command}
          onChange={handleChange}
          placeholder="Command to start the server (e.g., uvx, npx, http://...)"
          className={errors.command ? '' : ''} // Class for error state
          style={{
            backgroundColor: 'var(--global-background-color)',
            color: 'var(--global-text-color)',
            borderColor: errors.command ? 'var(--global-destructive-color, var(--global-primary-color))' : 'var(--global-border-color)',
            borderRadius: 'var(--global-border-radius)'
          }}
          disabled={isSubmitting}
        />
        {errors.command && (
          <div className="text-sm flex items-center" style={{ color: 'var(--global-destructive-color, var(--global-primary-color))' }}>
            <AlertCircle className="h-4 w-4 mr-1" />
            {errors.command}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="arguments" style={{ color: 'var(--global-text-color)' }}>Arguments</Label>
        <Input
          id="arguments"
          name="arguments"
          value={formData.arguments || ''}
          onChange={handleChange}
          placeholder="Command arguments"
          disabled={isSubmitting}
          style={{
            backgroundColor: 'var(--global-background-color)',
            color: 'var(--global-text-color)',
            borderColor: 'var(--global-border-color)',
            borderRadius: 'var(--global-border-radius)'
          }}
        />
      </div>

      <div className="flex items-center space-x-2">
        <Switch
          id="stdIo"
          checked={formData.stdIo}
          onCheckedChange={(checked) => handleSwitchChange('stdIo', checked)}
          disabled={isSubmitting}
          // Switch component is assumed to be themed globally or inherit
        />
        <Label htmlFor="stdIo" style={{ color: 'var(--global-text-color)' }}>Use Standard I/O</Label>
        <div className="text-xs ml-2" style={{ color: 'var(--global-secondary-text-color, var(--global-secondary-color))' }}>
          (Disable for HTTP/SSE endpoints)
        </div>
      </div>

      <div className="flex items-center space-x-2">
        <Switch
          id="isEnabled"
          checked={formData.isEnabled}
          onCheckedChange={(checked) => handleSwitchChange('isEnabled', checked)}
          disabled={isSubmitting}
          // Switch component is assumed to be themed globally or inherit
        />
        <Label htmlFor="isEnabled" style={{ color: 'var(--global-text-color)' }}>Enabled</Label>
      </div>

      <div className="space-y-2">
        <Label htmlFor="categories" style={{ color: 'var(--global-text-color)' }}>Categories</Label>
        <Input
          id="categories"
          name="categories"
          value={formData.categories?.join(', ') || ''}
          onChange={(e) => {
            const categoriesText = e.target.value;
            const categoriesList = categoriesText.split(',').map(cat => cat.trim()).filter(cat => cat !== '');
            setFormData(prev => ({
              ...prev,
              categories: categoriesList
            }));
          }}
          placeholder="Enter categories separated by commas (e.g., Development, Web, 3D Modelling)"
          disabled={isSubmitting}
          style={{
            backgroundColor: 'var(--global-background-color)',
            color: 'var(--global-text-color)',
            borderColor: 'var(--global-border-color)',
            borderRadius: 'var(--global-border-radius)'
          }}
        />
        <div className="text-xs" style={{ color: 'var(--global-secondary-text-color, var(--global-secondary-color))' }}>
          Categories help organize servers in the server modal
        </div>
      </div>

      {/* Tool Selection Section - Only in Edit Mode */}
      {server && (
        <div className="space-y-2">
          <Label style={{ color: 'var(--global-text-color)' }}>Select Tools to Expose to AI</Label>
          {isLoadingTools ? (
            <p className="text-sm" style={{ color: 'var(--global-secondary-text-color, var(--global-secondary-color))' }}>Loading tools...</p>
          ) : availableTools.length === 0 ? (
            <p className="text-sm" style={{ color: 'var(--global-secondary-text-color, var(--global-secondary-color))' }}>
              No tools found for this server, or an error occurred while trying to retrieve them. 
              Please check the server configuration and ensure it is running correctly.
            </p>
          ) : (
            <div className="p-2 rounded-md space-y-1" 
                 style={{ 
                   borderColor: 'var(--global-border-color)',
                   backgroundColor: 'var(--global-secondary-background-color, var(--global-background-color))',
                   borderRadius: 'var(--global-border-radius)'
                 }}>
              {availableTools.map((tool) => (
                <div key={tool.name} className="flex items-center space-x-2">
                  <Switch
                    id={`tool-switch-${tool.name.replace(/\s+/g, '-')}`}
                    checked={formData.selectedTools?.includes(tool.name)}
                    onCheckedChange={(checked) => {
                      setFormData((prev) => {
                        const currentSelected = prev.selectedTools || [];
                        let newSelected;
                        if (checked) {
                          newSelected = [...currentSelected, tool.name];
                        } else {
                          newSelected = currentSelected.filter(name => name !== tool.name);
                        }
                        return { ...prev, selectedTools: newSelected };
                      });
                    }}
                    disabled={isSubmitting}
                    // Switch component is assumed to be themed globally or inherit
                  />
                  <Label htmlFor={`tool-switch-${tool.name.replace(/\s+/g, '-')}`} className="text-sm font-normal" style={{ color: 'var(--global-text-color)' }}>
                    {tool.name}
                    {tool.description && <span className="text-xs ml-2" style={{ color: 'var(--global-secondary-text-color, var(--global-secondary-color))' }}>- {tool.description}</span>}
                  </Label>
                </div>
              ))}
            </div>
          )}
          <div className="text-xs" style={{ color: 'var(--global-secondary-text-color, var(--global-secondary-color))' }}>
            Choose which tools from this server should be available to the AI. <strong>If no tools are selected here, all tools from this server will be exposed. Select specific tools to limit availability.</strong>
          </div>
        </div>
      )}

      <div className="pt-4 flex justify-end space-x-2" style={{ backgroundColor: 'transparent' /* Inherits from DialogContent */ }}>
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={isSubmitting}
          style={{
            backgroundColor: 'var(--global-background-color)',
            borderColor: 'var(--global-border-color)',
            color: 'var(--global-text-color)',
            borderRadius: 'var(--global-border-radius)'
          }}
        >
          Cancel
        </Button>
        <Button 
          type="submit" 
          disabled={isSubmitting}
          style={{
            backgroundColor: 'var(--global-primary-color)',
            color: 'var(--global-primary-foreground-color, #ffffff)', // Added fallback from design
            borderRadius: 'var(--global-border-radius)'
          }}
        >
          {isSubmitting ? 'Saving...' : server ? 'Update Server' : 'Add Server'}
        </Button>
      </div>
    </form>
  );
}