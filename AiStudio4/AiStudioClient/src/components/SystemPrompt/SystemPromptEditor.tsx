
import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Form, FormField, FormItem, FormLabel, FormControl, FormDescription, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Badge } from '@/components/ui/badge';
import { X, Plus, Save, Check } from 'lucide-react';
import { SystemPrompt, SystemPromptFormValues } from '@/types/systemPrompt';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';
import { useToolsManagement } from '@/hooks/useToolsManagement';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';
import { useModelStore } from '@/stores/useModelStore';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

interface SystemPromptEditorProps {
  initialPrompt?: SystemPrompt | null;
  onClose: () => void;
  onApply?: (prompt: SystemPrompt) => void;
}

export function SystemPromptEditor({ initialPrompt, onClose, onApply }: SystemPromptEditorProps) {
  const { setCurrentPrompt } = useSystemPromptStore();
  const { models } = useModelStore();
  const { createSystemPrompt, updateSystemPrompt } = useSystemPromptManagement();

  const [isCreating, setIsCreating] = useState(!initialPrompt);
  const [isProcessing, setIsProcessing] = useState(false);
  const [newTag, setNewTag] = useState('');
  const [error, setLocalError] = useState<string | null>(null);

  const { tools } = useToolsManagement();
  const { prompts: userPrompts, fetchUserPrompts } = useUserPromptManagement();

  const form = useForm<SystemPromptFormValues>({
    defaultValues: initialPrompt
      ? {
          title: initialPrompt.title,
          content: initialPrompt.content,
          description: initialPrompt.description,
          tags: initialPrompt.tags,
          isDefault: initialPrompt.isDefault,
          associatedTools: initialPrompt.associatedTools || [],
          associatedUserPromptId: initialPrompt.associatedUserPromptId || 'none',
          primaryModelGuid: initialPrompt.primaryModelGuid || 'none',
          secondaryModelGuid: initialPrompt.secondaryModelGuid || 'none',
        }
      : {
          title: '',
          content: '',
          description: '',
          tags: [],
          isDefault: false,
          associatedTools: [],
          associatedUserPromptId: 'none',
          primaryModelGuid: 'none',
          secondaryModelGuid: 'none',
        },
  });

  useEffect(() => {
    // Fetch user prompts when component mounts
    fetchUserPrompts();
  }, [fetchUserPrompts]);

  useEffect(() => {
    if (initialPrompt) {
      form.reset({
        title: initialPrompt.title,
        content: initialPrompt.content,
        description: initialPrompt.description,
        tags: initialPrompt.tags,
        isDefault: initialPrompt.isDefault,
        associatedTools: initialPrompt.associatedTools || [],
        associatedUserPromptId: initialPrompt.associatedUserPromptId || 'none',
        primaryModelGuid: initialPrompt.primaryModelGuid || 'none',
        secondaryModelGuid: initialPrompt.secondaryModelGuid || 'none',
      });
      setIsCreating(false);
    } else {
      form.reset({
        title: '',
        content: '',
        description: '',
        tags: [],
        isDefault: false,
        associatedTools: [],
        associatedUserPromptId: 'none',
        primaryModelGuid: 'none',
        secondaryModelGuid: 'none',
      });
      setIsCreating(true);
    }
  }, [initialPrompt, form]);

  const onSubmit = async (data: SystemPromptFormValues) => {
    setIsProcessing(true);
    setLocalError(null);

    try {
      let result;

      if (isCreating) {
        result = await createSystemPrompt(data);
      } else if (initialPrompt) {
        result = await updateSystemPrompt({
          ...data,
          guid: initialPrompt.guid,
          createdDate: initialPrompt.createdDate,
          modifiedDate: new Date().toISOString(),
        });
      }

      if (result) {
        setCurrentPrompt(result);
      }

      if (result && result.guid && onApply) {
        onApply(result);
      } else {
        console.error('Cannot apply prompt - missing guid:', result);
      }

      onClose();
    } catch (err: any) {
      console.error('Error in form submission:', err);
      setLocalError(err?.message || 'Failed to save system prompt');
    } finally {
      setIsProcessing(false);
    }
  };

  const saveAndApply = async (data: SystemPromptFormValues) => {
    setIsProcessing(true);
    setLocalError(null);

    try {
      let result;
      if (isCreating) {
        result = await createSystemPrompt(data);
      } else if (initialPrompt) {
        result = await updateSystemPrompt({
          ...data,
          guid: initialPrompt.guid,
          createdDate: initialPrompt.createdDate,
          modifiedDate: new Date().toISOString(),
        });
      }

      if (result) {
        setCurrentPrompt(result);
      }

      if (result && result.guid && onApply) {
        onApply(result);
      }

      onClose();
    } catch (err: any) {
      console.error('Error in Save & Apply:', err);
      setLocalError(err?.message || 'Failed to save and apply prompt');
    } finally {
      setIsProcessing(false);
    }
  };

  const addTag = () => {
    if (newTag.trim() && !form.getValues('tags').includes(newTag.trim())) {
      const currentTags = form.getValues('tags');
      form.setValue('tags', [...currentTags, newTag.trim()]);
      setNewTag('');
    }
  };

  const removeTag = (tagToRemove: string) => {
    const currentTags = form.getValues('tags');
    form.setValue(
      'tags',
      currentTags.filter((tag) => tag !== tagToRemove),
    );
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && newTag.trim()) {
      e.preventDefault();
      addTag();
    }
  };

  return (
    <div className="h-full flex-col-full">

      {error && <div className="bg-red-950/30 text-red-400 p-3 rounded-md border border-red-800/50">{error}</div>}

      <Form {...form} className="flex-1 overflow-hidden">
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col h-full">
          <div className="space-y-6 flex-1  pr-2 max-h-[calc(100vh-350px)]">
            <FormField
              control={form.control}
              name="title"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="form-label">Title</FormLabel>
                  <FormControl>
                    <Input
                      placeholder="E.g., Technical Documentation Assistant"
                      className="input-base"
                      {...field}
                      disabled={isProcessing}
                    />
                  </FormControl>
                  <FormDescription className="form-description">
                    A short, descriptive name for this system prompt
                  </FormDescription>
                  <FormMessage className="text-red-400" />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="content"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="form-label">Prompt Content</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="You are a helpful assistant..."
                      className="min-h-[200px] font-mono input-base"
                      {...field}
                      disabled={isProcessing}
                    />
                  </FormControl>
                  <FormDescription className="form-description">
                    The actual system prompt that will be sent to the AI
                  </FormDescription>
                  <FormMessage className="text-red-400" />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="description"
              render={({ field }) => (
                <FormItem>
                  <FormLabel className="form-label">Description (Optional)</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="Brief description of what this prompt is for..."
                      className="min-h-[80px] input-base"
                      {...field}
                      disabled={isProcessing}
                    />
                  </FormControl>
                  <FormDescription className="form-description">
                    A longer description explaining the purpose of this prompt
                  </FormDescription>
                  <FormMessage className="text-red-400" />
                </FormItem>
              )}
            />

            <div>
              <FormLabel className="form-label">Tags</FormLabel>
              <div className="flex gap-2 mt-1.5 mb-4">
                <Input
                  placeholder="Add tag..."
                  value={newTag}
                  onChange={(e) => setNewTag(e.target.value)}
                  onKeyDown={handleKeyDown}
                  className="flex-1 input-base"
                  disabled={isProcessing}
                />
                <Button
                  type="button"
                  onClick={addTag}
                  variant="outline"
                  className="bg-gray-700 hover:bg-gray-600 border-gray-600"
                  disabled={isProcessing}
                >
                  <Plus className="h-4 w-4 mr-1" /> Add
                </Button>
              </div>

              <div className="flex flex-wrap gap-1.5 mb-2">
                {form.watch('tags').map((tag) => (
                  <Badge
                    key={tag}
                    variant="outline"
                    className="py-1 px-2 bg-gray-700  border-gray-600 flex items-center gap-1"
                  >
                    {tag}
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeTag(tag)}
                      className="h-4 w-4 p-0 hover:bg-gray-600 rounded-full"
                      disabled={isProcessing}
                    >
                      <X className="h-3 w-3" />
                    </Button>
                  </Badge>
                ))}
              </div>
              <FormDescription className="form-description">
                Tags help you organize and find your prompts more easily
              </FormDescription>
            </div>

            <FormField
              control={form.control}
              name="isDefault"
              render={({ field }) => (
                <FormItem className="flex flex-row items-start space-x-3 space-y-0 p-4 border border-gray-700 rounded-md bg-gray-800/50">
                  <FormControl>
                    <Checkbox
                      checked={field.value}
                      onCheckedChange={field.onChange}
                      className="data-[state=checked]:bg-blue-600 border-gray-500"
                      disabled={isProcessing}
                    />
                  </FormControl>
                  <div className="space-y-1 leading-none">
                    <FormLabel className="form-label">Set as Default Prompt</FormLabel>
                    <FormDescription className="form-description">
                      This prompt will be used for all new convs
                    </FormDescription>
                  </div>
                </FormItem>
              )}
            />

            {/* Model Association */}
            <div className="mt-4">
              <FormLabel className="form-label">Associated Models</FormLabel>
              
              {/* Primary Model */}
              <FormField
                control={form.control}
                name="primaryModelGuid"
                render={({ field }) => (
                  <FormItem className="mb-4">
                    <FormLabel className="form-label text-sm">Primary Model</FormLabel>
                    <Select
                      value={field.value || "none"}
                      onValueChange={field.onChange}
                      disabled={isProcessing}
                    >
                      <FormControl>
                        <SelectTrigger className="w-full bg-gray-800 border-gray-700">
                          <SelectValue placeholder="Select primary model" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent className="bg-gray-800 border-gray-700">
                        <SelectItem value="none">None</SelectItem>
                        {models.map((model) => (
                          <SelectItem key={model.guid} value={model.guid}>
                            {model.friendlyName || model.modelName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormDescription className="form-description">
                      The primary model to use with this system prompt
                    </FormDescription>
                  </FormItem>
                )}
              />
              
              {/* Secondary Model */}
              <FormField
                control={form.control}
                name="secondaryModelGuid"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel className="form-label text-sm">Secondary Model</FormLabel>
                    <Select
                      value={field.value || "none"}
                      onValueChange={field.onChange}
                      disabled={isProcessing}
                    >
                      <FormControl>
                        <SelectTrigger className="w-full bg-gray-800 border-gray-700">
                          <SelectValue placeholder="Select secondary model" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent className="bg-gray-800 border-gray-700">
                        <SelectItem value="none">None</SelectItem>
                        {models.map((model) => (
                          <SelectItem key={model.guid} value={model.guid}>
                            {model.friendlyName || model.modelName}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormDescription className="form-description">
                      The secondary model to use with this system prompt
                    </FormDescription>
                  </FormItem>
                )}
              />
            </div>

            {/* Tool Association Multi-Select */}
            <div>
              <FormLabel className="form-label">Associated Tools</FormLabel>
              <div className="flex flex-wrap gap-2 mt-2 mb-2">
                {tools.map((tool) => (
                  <label key={tool.guid} className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={form.watch('associatedTools').includes(tool.guid)}
                      onChange={(e) => {
                        const current = form.getValues('associatedTools') || [];
                        if (e.target.checked) {
                          form.setValue('associatedTools', [...current, tool.guid]);
                        } else {
                          form.setValue('associatedTools', current.filter((id) => id !== tool.guid));
                        }
                      }}
                      disabled={isProcessing}
                    />
                    <span className="text-sm text-gray-200">{tool.name}</span>
                  </label>
                ))}
              </div>
              <FormDescription className="form-description">
                Select one or more tools to associate with this system prompt. These tools will be activated when the prompt is used.
              </FormDescription>
            </div>

            {/* User Prompt Association */}
            <div className="mt-4">
              <FormLabel className="form-label">Associated User Prompt</FormLabel>
              <FormField
                control={form.control}
                name="associatedUserPromptId"
                render={({ field }) => (
                  <FormItem>
                    <Select
                      value={field.value}
                      onValueChange={field.onChange}
                      disabled={isProcessing}
                    >
                      <FormControl>
                        <SelectTrigger className="w-full bg-gray-800 border-gray-700">
                          <SelectValue placeholder="Select a user prompt" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent className="bg-gray-800 border-gray-700">
                        <SelectItem value="none">None</SelectItem>
                        {userPrompts.map((prompt) => (
                          <SelectItem key={prompt.guid} value={prompt.guid}>
                            {prompt.title}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormDescription className="form-description">
                      Select a user prompt to associate with this system prompt. When this system prompt is activated, the associated user prompt will also be activated.
                    </FormDescription>
                  </FormItem>
                )}
              />  
            </div>

            <div className="flex-none mt-4 space-x-3 flex">
              <Button
                type="button"
                onClick={onClose}
                variant="outline"
                disabled={isProcessing}
                className="btn-secondary"
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isProcessing} className="btn-primary">
                {isProcessing ? (
                  <span className="flex items-center gap-2">
                    <div className="animate-spin h-4 w-4 border-2 border-t-transparent border-white rounded-full" />
                    Saving...
                  </span>
                ) : (
                  <span className="flex items-center gap-2">
                    <Save className="h-4 w-4" />
                    {isCreating ? 'Create Prompt' : 'Update Prompt'}
                  </span>
                )}
              </Button>
              {onApply && !isCreating && (
                <Button
                  type="button"
                  onClick={form.handleSubmit(saveAndApply)}
                  disabled={isProcessing}
                  className="btn-primary bg-green-600 hover:bg-green-700"
                >
                  <span className="flex items-center gap-2">
                    <Check className="h-4 w-4" />
                    Save & Apply
                  </span>
                </Button>
              )}
            </div>
          </div>
        </form>
      </Form>
    </div>
  );
}

