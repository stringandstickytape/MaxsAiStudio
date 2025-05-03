// AiStudioClient/src/components/servers/ServerForm.tsx
import React, { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { McpServerDefinition } from '@/stores/useMcpServerStore';
import { AlertCircle } from 'lucide-react';

interface ServerFormProps {
  server?: McpServerDefinition;
  onSubmit: (server: McpServerDefinition) => void;
  onCancel: () => void;
  isSubmitting?: boolean;
}

export function ServerForm({ server, onSubmit, onCancel, isSubmitting = false }: ServerFormProps) {
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
      });
    }
  }, [server]);

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
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">Name</Label>
        <Input
          id="name"
          name="name"
          value={formData.name}
          onChange={handleChange}
          placeholder="Server name"
          className={errors.name ? 'border-red-500' : ''}
          disabled={isSubmitting}
        />
        {errors.name && (
          <div className="text-red-500 text-sm flex items-center">
            <AlertCircle className="h-4 w-4 mr-1" />
            {errors.name}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          name="description"
          value={formData.description || ''}
          onChange={handleChange}
          placeholder="Server description"
          rows={2}
          disabled={isSubmitting}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="command">Command</Label>
        <Input
          id="command"
          name="command"
          value={formData.command}
          onChange={handleChange}
          placeholder="Command to start the server (e.g., uvx, npx, http://...)"
          className={errors.command ? 'border-red-500' : ''}
          disabled={isSubmitting}
        />
        {errors.command && (
          <div className="text-red-500 text-sm flex items-center">
            <AlertCircle className="h-4 w-4 mr-1" />
            {errors.command}
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="arguments">Arguments</Label>
        <Input
          id="arguments"
          name="arguments"
          value={formData.arguments || ''}
          onChange={handleChange}
          placeholder="Command arguments"
          disabled={isSubmitting}
        />
      </div>

      <div className="flex items-center space-x-2">
        <Switch
          id="stdIo"
          checked={formData.stdIo}
          onCheckedChange={(checked) => handleSwitchChange('stdIo', checked)}
          disabled={isSubmitting}
        />
        <Label htmlFor="stdIo">Use Standard I/O</Label>
        <div className="text-xs text-gray-400 ml-2">
          (Disable for HTTP/SSE endpoints)
        </div>
      </div>

      <div className="flex items-center space-x-2">
        <Switch
          id="isEnabled"
          checked={formData.isEnabled}
          onCheckedChange={(checked) => handleSwitchChange('isEnabled', checked)}
          disabled={isSubmitting}
        />
        <Label htmlFor="isEnabled">Enabled</Label>
      </div>

      <div className="pt-4 flex justify-end space-x-2">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving...' : server ? 'Update Server' : 'Add Server'}
        </Button>
      </div>
    </form>
  );
}