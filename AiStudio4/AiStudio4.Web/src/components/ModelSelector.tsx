import React from 'react';
import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ChatService } from '@/services/ChatService';

export interface ModelSelectorProps {
    label: string;
    selectedModel: string;
    models: string[];
    modelType: 'primary' | 'secondary';
    onModelSelect: (model: string) => void;
}

export function ModelSelector({
    label,
    selectedModel,
    models,
    modelType,
    onModelSelect
}: ModelSelectorProps) {
    const displayLabel = selectedModel === "Select Model"
        ? `${label}: Select Model`
        : `${label}: ${selectedModel}`;

    const handleModelSelect = async (model: string) => {
        onModelSelect(model);

        try {
            // Save the selected model as default based on the model type
            if (modelType === 'primary') {
                await ChatService.saveDefaultModel(model);
            } else {
                await ChatService.saveSecondaryModel(model);
            }
        } catch (err) {
            console.error(`Failed to save ${modelType} model:`, err);
        }
    };

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button variant="outline" className="bg-gray-800/80 hover:bg-gray-700/80 text-gray-100 border-gray-600/50 rounded-lg transition-all duration-200 shadow-md hover:shadow-lg transform hover:-translate-y-0.5">
                    {displayLabel}
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="bg-[#1f2937] border-gray-700 text-gray-100 max-h-[300px] overflow-y-auto">
                {models.length > 0 ? (
                    models.map((model, index) => (
                        <DropdownMenuItem
                            className="hover:bg-[#374151] focus:bg-[#374151] cursor-pointer"
                            key={index}
                            onSelect={() => handleModelSelect(model)}
                        >
                            {model}
                        </DropdownMenuItem>
                    ))
                ) : (
                    <DropdownMenuItem disabled className="text-gray-500">
                        No models available
                    </DropdownMenuItem>
                )}
            </DropdownMenuContent>
        </DropdownMenu>
    );
}