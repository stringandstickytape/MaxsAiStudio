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
                                <FormLabel className="text-gray-200">Friendly Name</FormLabel>
                                <FormControl>
                                    <Input
                                        placeholder="e.g., GPT-4 Turbo"
                                        {...field}
                                        className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500"
                                    />
                                </FormControl>
                                <FormMessage className="text-red-400" />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="modelName"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel className="text-gray-200">Model Name</FormLabel>
                                <FormControl>
                                    <Input
                                        placeholder="e.g., gpt-4-turbo"
                                        {...field}
                                        className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500"
                                    />
                                </FormControl>
                                <FormMessage className="text-red-400" />
                            </FormItem>
                        )}
                    />
                </div>

                <FormField
                    control={form.control}
                    name="providerGuid"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel className="text-gray-200">Service Provider</FormLabel>
                            <Select
                                onValueChange={field.onChange}
                                defaultValue={field.value}
                            >
                                <FormControl>
                                    <SelectTrigger className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500">
                                        <SelectValue placeholder="Select a provider" />
                                    </SelectTrigger>
                                </FormControl>
                                <SelectContent className="bg-gray-800 border-gray-700 text-gray-100">
                                    {providers.map(provider => (
                                        <SelectItem
                                            key={provider.guid}
                                            value={provider.guid}
                                            className="focus:bg-gray-700 focus:text-gray-100"
                                        >
                                            {provider.friendlyName}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            <FormMessage className="text-red-400" />
                        </FormItem>
                    )}
                />

                <div className="grid grid-cols-2 gap-4">
                    <FormField
                        control={form.control}
                        name="input1MTokenPrice"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel className="text-gray-200">Input Token Price (per 1M)</FormLabel>
                                <FormControl>
                                    <Input
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        placeholder="0.00"
                                        {...field}
                                        onChange={(e) => field.onChange(parseFloat(e.target.value))}
                                        className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500"
                                    />
                                </FormControl>
                                <FormMessage className="text-red-400" />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="output1MTokenPrice"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel className="text-gray-200">Output Token Price (per 1M)</FormLabel>
                                <FormControl>
                                    <Input
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        placeholder="0.00"
                                        {...field}
                                        onChange={(e) => field.onChange(parseFloat(e.target.value))}
                                        className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500"
                                    />
                                </FormControl>
                                <FormMessage className="text-red-400" />
                            </FormItem>
                        )}
                    />
                </div>

                <FormField
                    control={form.control}
                    name="userNotes"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel className="text-gray-200">Notes</FormLabel>
                            <FormControl>
                                <Input
                                    placeholder="Any additional notes about this model"
                                    {...field}
                                    className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500"
                                />
                            </FormControl>
                            <FormMessage className="text-red-400" />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="additionalParams"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel className="text-gray-200">Additional Parameters (JSON)</FormLabel>
                            <FormControl>
                                <Input
                                    placeholder='{"temperature": 0.7}'
                                    {...field}
                                    className="bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500 placeholder-gray-500"
                                />
                            </FormControl>
                            <FormDescription className="text-gray-400">
                                Optional JSON parameters to include with requests to this model
                            </FormDescription>
                            <FormMessage className="text-red-400" />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="color"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel className="text-gray-200">Color</FormLabel>
                            <div className="flex gap-2">
                                <FormControl>
                                    <Input
                                        type="color"
                                        className="w-12 h-8 p-1 bg-transparent"
                                        {...field}
                                    />
                                </FormControl>
                                <Input
                                    value={field.value}
                                    onChange={field.onChange}
                                    className="flex-1 bg-gray-700 border-gray-600 text-gray-100 focus:ring-blue-500 focus:border-blue-500"
                                />
                            </div>
                            <FormMessage className="text-red-400" />
                        </FormItem>
                    )}
                />

                <div className="grid grid-cols-2 gap-4">
                    <FormField
                        control={form.control}
                        name="starred"
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
                                        Supports Prefill
                                    </FormLabel>
                                    <FormDescription className="text-gray-400">
                                        This model supports prefilling content
                                    </FormDescription>
                                </div>
                            </FormItem>
                        )}
                    />
                </div>

                <div className="flex justify-end gap-2 pt-4">
                    <Button
                        type="submit"
                        disabled={isProcessing}
                        className="bg-blue-600 hover:bg-blue-700 text-white"
                    >
                        {isProcessing ? 'Saving...' : initialValues ? 'Update Model' : 'Add Model'}
                    </Button>
                </div>
            </form>
        </Form>
    );
};  