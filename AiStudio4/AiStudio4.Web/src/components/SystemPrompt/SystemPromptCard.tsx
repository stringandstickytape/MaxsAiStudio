// src/components/SystemPrompt/SystemPromptCard.tsx
import React, { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Check, Edit, Star, Trash2 } from 'lucide-react';
import { SystemPrompt } from '@/types/systemPrompt';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { useSystemPromptManagement } from '@/hooks/useSystemPromptManagement';

interface SystemPromptCardProps {
    prompt: SystemPrompt;
    isDefault: boolean;
    onEdit: () => void;
    onApply: () => void;
}

export function SystemPromptCard({ prompt, isDefault, onEdit, onApply }: SystemPromptCardProps) {
    // Use Zustand store
    const { setDefaultPromptId } = useSystemPromptStore();
    
    // Use management hook instead of RTK Query
    const { 
        deleteSystemPrompt,
        setDefaultSystemPrompt 
    } = useSystemPromptManagement();
    
    const [expanded, setExpanded] = useState(false);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [isProcessing, setIsProcessing] = useState(false);

    const handleSetDefault = async () => {
        if (!isDefault) {
            try {
                setIsProcessing(true);
                await setDefaultSystemPrompt(prompt.guid);
                // Update Zustand store
                setDefaultPromptId(prompt.guid);
            } catch (error) {
                console.error('Failed to set default prompt:', error);
            } finally {
                setIsProcessing(false);
            }
        }
    };

    const handleDelete = () => {
        setDeleteDialogOpen(true);
    };

    const confirmDelete = async () => {
        try {
            setIsProcessing(true);
            await deleteSystemPrompt(prompt.guid);
            setDeleteDialogOpen(false);
        } catch (error) {
            console.error('Failed to delete prompt:', error);
        } finally {
            setIsProcessing(false);
        }
    };

    const toggleExpand = () => {
        setExpanded(!expanded);
    };

    const hasDescription = prompt.description && prompt.description.trim().length > 0;
    const hasTags = prompt.tags && prompt.tags.length > 0;

    return (
        <>
            <Card className="card-base card-hover hover:border-gray-600">
                <CardContent className="p-4">
                    <div className="flex justify-between items-start gap-2">
                        <div className="flex-1">
                            <div className="flex items-center gap-2 mb-1">
                                <h3 className="text-title text-lg">{prompt.title}</h3>
                                {isDefault && (
                                    <Badge variant="outline" className="bg-blue-900/30 text-blue-300 border-blue-700 text-xs">
                                        Default
                                    </Badge>
                                )}
                            </div>
                            {hasDescription && (
                                <p className="text-body mb-2">{prompt.description}</p>
                            )}
                        </div>
                        <div className="flex-shrink-0">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={onApply}
                                className="text-gray-300 hover:text-gray-100 hover:bg-gray-700"
                                disabled={isProcessing}
                            >
                                <Check className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>

                    <div
                        className={`mt-2 p-3 bg-gray-700/30 rounded-md cursor-pointer ${expanded ? '' : 'line-clamp-3'}`}
                        onClick={toggleExpand}
                    >
                        <pre className="text-gray-200 text-sm font-mono whitespace-pre-wrap break-words">
                            {prompt.content}
                        </pre>
                    </div>

                    {hasTags && (
                        <div className="mt-2 flex flex-wrap gap-1">
                            {prompt.tags.map(tag => (
                                <Badge
                                    key={tag}
                                    variant="outline"
                                    className="text-xs bg-gray-700/50 text-gray-300 border-gray-600"
                                >
                                    {tag}
                                </Badge>
                            ))}
                        </div>
                    )}

                    <div className="mt-3 flex justify-between items-center">
                        <div className="text-mono text-gray-500">
                            {new Date(prompt.modifiedDate).toLocaleDateString()}
                        </div>
                        <div className="flex gap-1">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={handleSetDefault}
                                className={`${isDefault ? 'text-blue-400' : 'text-gray-400 hover:text-gray-100'} hover:bg-gray-700`}
                                disabled={isDefault || isProcessing}
                            >
                                <Star className={`h-4 w-4 ${isDefault ? 'fill-blue-400' : ''}`} />
                            </Button>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={onEdit}
                                className="text-gray-400 hover:text-gray-100 hover:bg-gray-700"
                                disabled={isProcessing}
                            >
                                <Edit className="h-4 w-4" />
                            </Button>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={handleDelete}
                                className="text-red-400 hover:text-red-300 hover:bg-red-900/20"
                                disabled={isProcessing}
                            >
                                <Trash2 className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>
                </CardContent>
            </Card>

            <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
                <AlertDialogContent className="bg-gray-800 border-gray-700 text-gray-100">
                    <AlertDialogHeader>
                        <AlertDialogTitle>Are you sure you want to delete this prompt?</AlertDialogTitle>
                        <AlertDialogDescription className="text-gray-400">
                            This action cannot be undone. This will permanently delete the system prompt "{prompt.title}".
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600" disabled={isProcessing}>
                            Cancel
                        </AlertDialogCancel>
                        <AlertDialogAction
                            onClick={confirmDelete}
                            className="bg-red-700 hover:bg-red-800 text-white border-red-900"
                            disabled={isProcessing}
                        >
                            {isProcessing ? 'Deleting...' : 'Delete'}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </>
    );
}