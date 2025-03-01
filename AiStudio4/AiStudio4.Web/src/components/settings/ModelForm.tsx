import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { Model, ServiceProvider } from '@/types/settings';
import { Form, FormField, FormItem, FormLabel, FormControl, FormDescription, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Checkbox } from '@/components/ui/checkbox';
import { v4 as uuidv4 } from 'uuid';

interface ModelFormProps {
    providers: ServiceProvider[];
    initialValues?: Model;
    onSubmit: (data: any) => Promise<void>;
    isProcessing: boolean;
}

export const ModelForm: React.FC<ModelFormProps> = ({
    providers,
    initialValues,
    onSubmit,
    isProcessing
}) => {
    const form = useForm({
        defaultValues: initialValues || {
            guid: '',
            modelName: '',
            friendlyName: '',
            providerGuid: '',
            userNotes: '',
            input1MTokenPrice: 0,
            output1MTokenPrice: 0,
            color: '#4f46e5',
            starred: false,
            supportsPrefill: false
        }
    });

    useEffect(() => {
        if (initialValues) {
            console.log('Resetting model form with initialValues:', initialValues);
            form.reset(initialValues);
        }
    }, [initialValues, form]);

    const handleSubmit = async (data: any) => {
        // For new models, generate a GUID
        if (!data.guid) {
            data.guid = uuidv4();
        }
        await onSubmit(data);
    };

    return (
        <Form {...form}>
            <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                    <FormField
                        control={form.control}
                        name="friendlyName"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Friendly Name</FormLabel>
                                <FormControl>
                                    <Input 
                                        placeholder="e.g., GPT-4 Turbo" 
                                        {...field} 
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    
                    <FormField
                        control={form.control}
                        name="modelName"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Model Name</FormLabel>
                                <FormControl>
                                    <Input 
                                        placeholder="e.g., gpt-4-turbo" 
                                        {...field} 
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                </div>

                <FormField
                    control={form.control}
                    name="providerGuid"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Service Provider</FormLabel>
                            <Select 
                                onValueChange={field.onChange} 
                                defaultValue={field.value}
                            >
                                <FormControl>
                                    <SelectTrigger>
                                        <SelectValue placeholder="Select a provider" />
                                    </SelectTrigger>
                                </FormControl>
                                <SelectContent>
                                    {providers.map(provider => (
                                        <SelectItem 
                                            key={provider.guid} 
                                            value={provider.guid}
                                        >
                                            {provider.friendlyName}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <div className="grid grid-cols-2 gap-4">
                    <FormField
                        control={form.control}
                        name="input1MTokenPrice"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Input Token Price (per 1M)</FormLabel>
                                <FormControl>
                                    <Input 
                                        type="number" 
                                        step="0.01"
                                        min="0"
                                        placeholder="0.00" 
                                        {...field}
                                        onChange={(e) => field.onChange(parseFloat(e.target.value))}
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    
                    <FormField
                        control={form.control}
                        name="output1MTokenPrice"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Output Token Price (per 1M)</FormLabel>
                                <FormControl>
                                    <Input 
                                        type="number" 
                                        step="0.01"
                                        min="0"
                                        placeholder="0.00" 
                                        {...field}
                                        onChange={(e) => field.onChange(parseFloat(e.target.value))}
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                </div>

                <FormField
                    control={form.control}
                    name="userNotes"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Notes</FormLabel>
                            <FormControl>
                                <Input 
                                    placeholder="Any additional notes about this model" 
                                    {...field} 
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="additionalParams"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Additional Parameters (JSON)</FormLabel>
                            <FormControl>
                                <Input 
                                    placeholder='{"temperature": 0.7}' 
                                    {...field} 
                                />
                            </FormControl>
                            <FormDescription>
                                Optional JSON parameters to include with requests to this model
                            </FormDescription>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="color"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Color</FormLabel>
                            <div className="flex gap-2">
                                <FormControl>
                                    <Input 
                                        type="color" 
                                        className="w-12 h-8 p-1"
                                        {...field} 
                                    />
                                </FormControl>
                                <Input 
                                    value={field.value}
                                    onChange={field.onChange}
                                    className="flex-1"
                                />
                            </div>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <div className="grid grid-cols-2 gap-4">
                    <FormField
                        control={form.control}
                        name="starred"
                        render={({ field }) => (
                            <FormItem className="flex flex-row items-start space-x-3 space-y-0 p-4 border rounded-md">
                                <FormControl>
                                    <Checkbox
                                        checked={field.value}
                                        onCheckedChange={field.onChange}
                                    />
                                </FormControl>
                                <div className="space-y-1 leading-none">
                                    <FormLabel>
                                        Starred
                                    </FormLabel>
                                    <FormDescription>
                                        Mark this model as a favorite
                                    </FormDescription>
                                </div>
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="supportsPrefill"
                        render={({ field }) => (
                            <FormItem className="flex flex-row items-start space-x-3 space-y-0 p-4 border rounded-md">
                                <FormControl>
                                    <Checkbox
                                        checked={field.value}
                                        onCheckedChange={field.onChange}
                                    />
                                </FormControl>
                                <div className="space-y-1 leading-none">
                                    <FormLabel>
                                        Supports Prefill
                                    </FormLabel>
                                    <FormDescription>
                                        This model supports prefilling content
                                    </FormDescription>
                                </div>
                            </FormItem>
                        )}
                    />
                </div>

                <div className="flex justify-end gap-2 pt-4">
                    <Button type="submit" disabled={isProcessing}>
                        {isProcessing ? 'Saving...' : initialValues ? 'Update Model' : 'Add Model'}
                    </Button>
                </div>
            </form>
        </Form>
    );
};