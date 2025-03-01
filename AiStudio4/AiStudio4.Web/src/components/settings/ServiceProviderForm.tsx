import React, { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { ServiceProvider } from '@/types/settings';
import { Form, FormField, FormItem, FormLabel, FormControl, FormDescription, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { v4 as uuidv4 } from 'uuid';

interface ServiceProviderFormProps {
    initialValues?: ServiceProvider;
    onSubmit: (data: any) => Promise<void>;
    isProcessing: boolean;
}

export const ServiceProviderForm: React.FC<ServiceProviderFormProps> = ({
    initialValues,
    onSubmit,
    isProcessing
}) => {
    // Initialize the form with default values
    const form = useForm({
        defaultValues: {
            guid: '',
            url: '',
            apiKey: '',
            friendlyName: '',
            serviceName: ''
        }
    });

    // Reset form with initialValues when they change
    useEffect(() => {
        if (initialValues) {
            console.log('Resetting form with initialValues:', initialValues);
            form.reset(initialValues);
        }
    }, [initialValues, form]);

    const handleSubmit = async (data: any) => {
        // For new providers, generate a GUID
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
                                        placeholder="e.g., OpenAI"
                                        {...field}
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />

                    <FormField
                        control={form.control}
                        name="serviceName"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Service Name</FormLabel>
                                <FormControl>
                                    <Input
                                        placeholder="e.g., OpenAI"
                                        {...field}
                                    />
                                </FormControl>
                                <FormDescription>
                                    Internal service name (e.g., OpenAI, Claude, Gemini)
                                </FormDescription>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                </div>

                <FormField
                    control={form.control}
                    name="url"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>API URL</FormLabel>
                            <FormControl>
                                <Input
                                    placeholder="https://api.example.com/v1/chat/completions"
                                    {...field}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="apiKey"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>API Key</FormLabel>
                            <FormControl>
                                <Input
                                    type="password"
                                    placeholder="Your API key"
                                    {...field}
                                />
                            </FormControl>
                            <FormDescription>
                                Your API key will be stored securely
                            </FormDescription>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <div className="flex justify-end gap-2 pt-4">
                    <Button type="submit" disabled={isProcessing}>
                        {isProcessing ? 'Saving...' : initialValues ? 'Update Provider' : 'Add Provider'}
                    </Button>
                </div>
            </form>
        </Form>
    );
};