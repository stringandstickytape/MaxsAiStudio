// src/components/SystemPrompt/SystemPromptEditor.tsx
import React, { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useDispatch } from 'react-redux';
import {
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Badge } from '@/components/ui/badge';
import { X, Plus, Save, Check } from 'lucide-react';
import { SystemPrompt, SystemPromptFormValues } from '@/types/systemPrompt';
import { createSystemPrompt, updateSystemPrompt } from '@/store/systemPromptSlice';

interface SystemPromptEditorProps {
  initialPrompt?: SystemPrompt | null;
  onClose: () => void;
  onApply?: (prompt: SystemPrompt) => void;
}

export function SystemPromptEditor({ initialPrompt, onClose, onApply }: SystemPromptEditorProps) {
  const dispatch = useDispatch();
  const [isCreating, setIsCreating] = useState(!initialPrompt);
  const [isProcessing, setIsProcessing] = useState(false);
  const [newTag, setNewTag] = useState('');
  const [error, setError] = useState<string | null>(null);

  const form = useForm<SystemPromptFormValues>({
    defaultValues: initialPrompt ? {
      title: initialPrompt.title,
      content: initialPrompt.content,
      description: initialPrompt.description,
      tags: initialPrompt.tags,
      isDefault: initialPrompt.isDefault
    } : {
      title: '',
      content: '',
      description: '',
      tags: [],
      isDefault: false
    }
  });

  // Reset form when initialPrompt changes
  useEffect(() => {
    if (initialPrompt) {
      form.reset({
        title: initialPrompt.title,
        content: initialPrompt.content,
        description: initialPrompt.description,
        tags: initialPrompt.tags,
        isDefault: initialPrompt.isDefault
      });
      setIsCreating(false);
    } else {
      form.reset({
        title: '',
        content: '',
        description: '',
        tags: [],
        isDefault: false
      });
      setIsCreating(true);
    }
  }, [initialPrompt, form]);

    const onSubmit = async (data: SystemPromptFormValues) => {
        setIsProcessing(true);
        setError(null);

        try {
            let result;

            if (isCreating) {
                result = await dispatch(createSystemPrompt(data)).unwrap();
                console.log("Created new prompt with result:", result);
            } else if (initialPrompt) {
                result = await dispatch(updateSystemPrompt({
                    ...data,
                    guid: initialPrompt.guid,
                    createdDate: initialPrompt.createdDate,
                    modifiedDate: new Date().toISOString()
                })).unwrap();
            }

            // Only apply the prompt if we have a valid result with a guid
            if (result && result.guid && onApply) {
                console.log("Applying prompt with guid:", result.guid);
                onApply(result);
            } else {
                console.error("Cannot apply prompt - missing guid:", result);
            }

            onClose();
        } catch (err: any) {
            console.error("Error in form submission:", err);
            setError(err?.message || 'Failed to save system prompt');
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
    form.setValue('tags', currentTags.filter(tag => tag !== tagToRemove));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && newTag.trim()) {
      e.preventDefault();
      addTag();
    }
  };

  return (
      <div className="space-y-4 flex flex-col h-full overflow-y-auto">
      <div className="flex justify-between items-center">
        <h2 className="text-xl font-semibold text-gray-100">
          {isCreating ? 'Create New System Prompt' : 'Edit System Prompt'}
        </h2>
        <Button
          variant="ghost"
          size="icon"
          onClick={onClose}
          className="text-gray-400 hover:text-gray-100"
        >
          <X className="h-4 w-4" />
        </Button>
      </div>

      {error && (
        <div className="bg-red-950/30 text-red-400 p-3 rounded-md border border-red-800/50">
          {error}
        </div>
      )}

      <Form {...form}>
         <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 flex flex-col flex-1 overflow-y-auto">
          <FormField
            control={form.control}
            name="title"
            render={({ field }) => (
              <FormItem>
                <FormLabel className="text-gray-200">Title</FormLabel>
                <FormControl>
                  <Input
                    placeholder="E.g., Technical Documentation Assistant"
                    className="bg-gray-700 border-gray-600 text-gray-100"
                    {...field}
                  />
                </FormControl>
                <FormDescription className="text-gray-400">
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
                <FormLabel className="text-gray-200">Prompt Content</FormLabel>
                <FormControl>
                  <Textarea
                    placeholder="You are a helpful assistant..."
                    className="min-h-[200px] bg-gray-700 border-gray-600 text-gray-100 font-mono"
                    {...field}
                  />
                </FormControl>
                <FormDescription className="text-gray-400">
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
                <FormLabel className="text-gray-200">Description (Optional)</FormLabel>
                <FormControl>
                  <Textarea
                    placeholder="Brief description of what this prompt is for..."
                    className="min-h-[80px] bg-gray-700 border-gray-600 text-gray-100"
                    {...field}
                  />
                </FormControl>
                <FormDescription className="text-gray-400">
                  A longer description explaining the purpose of this prompt
                </FormDescription>
                <FormMessage className="text-red-400" />
              </FormItem>
            )}
          />

          <div>
            <FormLabel className="text-gray-200">Tags</FormLabel>
            <div className="flex gap-2 mt-1.5 mb-4">
              <Input
                placeholder="Add tag..."
                value={newTag}
                onChange={(e) => setNewTag(e.target.value)}
                onKeyDown={handleKeyDown}
                className="flex-1 bg-gray-700 border-gray-600 text-gray-100"
              />
              <Button
                type="button"
                onClick={addTag}
                variant="outline"
                className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
              >
                <Plus className="h-4 w-4 mr-1" /> Add
              </Button>
            </div>

            <div className="flex flex-wrap gap-1.5 mb-2">
              {form.watch('tags').map((tag) => (
                <Badge
                  key={tag}
                  variant="outline"
                  className="py-1 px-2 bg-gray-700 text-gray-200 border-gray-600 flex items-center gap-1"
                >
                  {tag}
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => removeTag(tag)}
                    className="h-4 w-4 p-0 hover:bg-gray-600 rounded-full"
                  >
                    <X className="h-3 w-3" />
                  </Button>
                </Badge>
              ))}
            </div>
            <FormDescription className="text-gray-400">
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
                  />
                </FormControl>
                <div className="space-y-1 leading-none">
                  <FormLabel className="text-gray-200">
                    Set as Default Prompt
                  </FormLabel>
                  <FormDescription className="text-gray-400">
                    This prompt will be used for all new conversations
                  </FormDescription>
                </div>
              </FormItem>
            )}
          />

          <div className="flex justify-end gap-3 pt-2 sticky bottom-0 bg-gray-900 pb-4 mt-4">
            <Button
              type="button"
              onClick={onClose}
              variant="outline"
              disabled={isProcessing}
              className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isProcessing}
              className="bg-blue-600 hover:bg-blue-700 text-white"
            >
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
                    onClick={() => {
                        form.handleSubmit(async (data) => {
                            try {
                                let result;
                                if (isCreating) {
                                    result = await dispatch(createSystemPrompt(data)).unwrap();
                                } else if (initialPrompt) {
                                    result = await dispatch(updateSystemPrompt({
                                        ...data,
                                        guid: initialPrompt.guid,
                                        createdDate: initialPrompt.createdDate,
                                        modifiedDate: new Date().toISOString()
                                    })).unwrap();
                                }

                                if (result && result.guid && onApply) {
                                    console.log("Save & Apply - applying prompt with guid:", result.guid);
                                    onApply(result);
                                }

                                onClose();
                            } catch (err) {
                                console.error("Error in Save & Apply:", err);
                                setError(err instanceof Error ? err.message : 'Failed to save and apply prompt');
                            }
                        })();
                    }}
                    disabled={isProcessing}
                    className="bg-green-600 hover:bg-green-700 text-white"
                >
                    <span className="flex items-center gap-2">
                        <Check className="h-4 w-4" />
                        Save & Apply
                    </span>
                </Button>
            )}
          </div>
        </form>
      </Form>
    </div>
  );
}
