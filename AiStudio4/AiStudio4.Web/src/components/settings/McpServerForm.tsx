import React from 'react';
import { useForm } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { McpServerDefinition } from '@/types/mcpTypes';
import { useMcpServerStore } from '@/stores/useMcpServerStore';

interface McpServerFormProps {
  initialData?: McpServerDefinition;
  onSuccess?: () => void;
  onCancel?: () => void;
}

type FormValues = Omit<McpServerDefinition, 'id' | 'lastModified'>;

const McpServerForm: React.FC<McpServerFormProps> = ({ initialData, onSuccess, onCancel }) => {
  const { addServer, updateServer } = useMcpServerStore();
  
  const defaultValues: FormValues = {
    name: '',
    command: '',
    arguments: '',
    isEnabled: true,
    description: '',
    env: {}
  };
  
  const form = useForm<FormValues>({
    defaultValues: initialData || defaultValues,
  });
  
  const onSubmit = async (values: FormValues) => {
    try {
      if (initialData) {
        await updateServer({
          ...values,
          id: initialData.id,
          lastModified: initialData.lastModified,
        });
      } else {
        await addServer(values);
      }
      
      if (onSuccess) {
        onSuccess();
      }
      form.reset();
    } catch (error) {
      console.error('Failed to save MCP server:', error);
    }
  };
  
  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-gray-200">Name</FormLabel>
              <FormControl>
                <Input placeholder="MCP Server Name" className="bg-gray-700/50 border-gray-600 text-gray-100" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        
        <FormField
          control={form.control}
          name="command"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-gray-200">Command</FormLabel>
              <FormControl>
                <Input placeholder="e.g., uvx" className="bg-gray-700/50 border-gray-600 text-gray-100" {...field} />
              </FormControl>
              <FormDescription className="text-gray-400">
                The command to start the MCP server process
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        
        <FormField
          control={form.control}
          name="arguments"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-gray-200">Arguments</FormLabel>
              <FormControl>
                <Input placeholder="e.g., blender-mcp" className="bg-gray-700/50 border-gray-600 text-gray-100" {...field} />
              </FormControl>
              <FormDescription className="text-gray-400">
                Command-line arguments for the MCP server
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        
        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-gray-200">Description</FormLabel>
              <FormControl>
                <Textarea 
                  placeholder="Describe what this MCP server does" 
                  className="bg-gray-700/50 border-gray-600 text-gray-100"
                  {...field} 
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        
        <FormField
          control={form.control}
          name="isEnabled"
          render={({ field }) => (
            <FormItem className="flex items-center justify-between rounded-lg border p-3 border-gray-700 bg-gray-800/50">
              <div className="space-y-0.5">
                <FormLabel className="text-gray-200">Enabled</FormLabel>
                <FormDescription className="text-gray-400">
                  Whether this MCP server should be available for use
                </FormDescription>
              </div>
              <FormControl>
                <Switch 
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        
        {/* Environment Variables could be added here with a more complex UI */}
        
        <div className="flex justify-end space-x-2 pt-4">
          {onCancel && (
            <Button 
              type="button" 
              variant="outline" 
              onClick={onCancel}
              className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
            >
              Cancel
            </Button>
          )}
          <Button 
            type="submit"
            className="bg-blue-600 hover:bg-blue-700 text-white"
          >
            {initialData ? 'Update' : 'Create'} Server
          </Button>
        </div>
      </form>
    </Form>
  );
};

export default McpServerForm;