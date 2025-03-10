// src/components/SystemPrompt/SystemPromptLibrary.tsx
import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Input } from '@/components/ui/input';
import { PlusCircle, Search } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { SystemPrompt } from '@/types/systemPrompt';
import { SystemPromptCard } from './SystemPromptCard';
import { SystemPromptEditor } from './SystemPromptEditor';
import { useConversationStore } from '@/stores/useConversationStore';
import { useSystemPromptStore } from '@/stores/useSystemPromptStore';
import { useSystemPromptManagement } from '@/hooks/useSystemPromptManagement';

interface SystemPromptLibraryProps {
    onApplyPrompt?: (prompt: SystemPrompt) => void;
    conversationId?: string;
}

export function SystemPromptLibrary({
    onApplyPrompt,
    conversationId
}: SystemPromptLibraryProps) {
    // Use Zustand store
    const { 
        prompts, 
        defaultPromptId, 
        conversationPrompts,
        setPrompts,
        setCurrentPrompt
    } = useSystemPromptStore();
    
    // Get active conversation ID from Zustand when not provided as prop
    const { activeConversationId: storeConversationId } = useConversationStore();

    // Use the management hook instead of RTK Query
    const { 
        prompts: serverPrompts, 
        isLoading, 
        setConversationSystemPrompt 
    } = useSystemPromptManagement();

    const [searchTerm, setSearchTerm] = useState('');
    const [showEditor, setShowEditor] = useState(false);
    const [promptToEdit, setPromptToEdit] = useState<SystemPrompt | null>(null);
    const [activeTab, setActiveTab] = useState('all');
    
    // Sync server prompts to Zustand store
    useEffect(() => {
        if (serverPrompts && serverPrompts.length > 0) {
            setPrompts(serverPrompts);
        }
    }, [serverPrompts, setPrompts]);

    const handleCreatePrompt = () => {
        setPromptToEdit(null);
        setShowEditor(true);
    };

    const handleEditPrompt = (prompt: SystemPrompt) => {
        setPromptToEdit(prompt);
        setShowEditor(true);
    };

    const handleCloseEditor = () => {
        setShowEditor(false);
        setPromptToEdit(null);
    };

    const handleApplyPrompt = async (prompt: SystemPrompt) => {
        // Set as current prompt in the store
        setCurrentPrompt(prompt);
        
        if (onApplyPrompt) {
            onApplyPrompt(prompt);
        }

        // If we have a conversation ID, set this prompt as the conversation's system prompt
        const effectiveConversationId = conversationId || storeConversationId;
        if (effectiveConversationId) {
            try {
                await setConversationSystemPrompt({
                    conversationId: effectiveConversationId,
                    promptId: prompt.guid
                });
                console.log(`Set conversation ${effectiveConversationId} system prompt to ${prompt.guid}`);
            } catch (error) {
                console.error('Failed to set conversation system prompt:', error);
            }
        }
    };

    const getFilteredPrompts = () => {
        let filtered = prompts;

        // Apply active tab filtering
        if (activeTab === 'favorites') {
            filtered = filtered.filter(p => p.tags.includes('favorite'));
        } else if (activeTab === 'default') {
            filtered = filtered.filter(p => p.isDefault || p.guid === defaultPromptId);
        } else if (activeTab === 'conversation' && conversationId) {
            const conversationPromptId = conversationPrompts[conversationId];
            filtered = filtered.filter(p => p.guid === conversationPromptId);
        }

        // Apply search filtering
        if (searchTerm) {
            const term = searchTerm.toLowerCase();
            filtered = filtered.filter(
                p => p.title.toLowerCase().includes(term) ||
                    p.content.toLowerCase().includes(term) ||
                    p.description.toLowerCase().includes(term) ||
                    p.tags.some(tag => tag.toLowerCase().includes(term))
            );
        }

        return filtered;
    };

    const filteredPrompts = getFilteredPrompts();

    if (showEditor) {
        return (
            <div className="h-full flex flex-col">
                <div className="flex-1 overflow-auto p-4">
                    <SystemPromptEditor
                        initialPrompt={promptToEdit}
                        onClose={handleCloseEditor}
                        onApply={handleApplyPrompt}
                    />
                </div>
            </div>
        );
    }

    return (
        <div className="h-full flex flex-col overflow-hidden">
            <div className="flex-none p-4 border-b border-gray-700">
                <div className="relative mb-4">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                        <Search className="h-4 w-4 text-gray-400" />
                    </div>
                    <Input
                        placeholder="Search prompts..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="pl-10 bg-gray-800 border-gray-700 text-gray-100"
                    />
                </div>

                <Button
                    onClick={handleCreatePrompt}
                    className="w-full flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-700 text-white"
                >
                    <PlusCircle className="h-4 w-4" /> Create New Prompt
                </Button>
            </div>

            <Tabs defaultValue="all" value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col overflow-hidden">
                <TabsList className="grid grid-cols-4 mx-4 mt-4 mb-2 bg-gray-800 flex-none">
                    <TabsTrigger value="all" className="data-[state=active]:bg-gray-700">
                        All
                    </TabsTrigger>
                    <TabsTrigger value="default" className="data-[state=active]:bg-gray-700">
                        Default
                    </TabsTrigger>
                    {conversationId && (
                        <TabsTrigger value="conversation" className="data-[state=active]:bg-gray-700">
                            Current
                        </TabsTrigger>
                    )}
                    <TabsTrigger value="favorites" className="data-[state=active]:bg-gray-700">
                        Favorites
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="all" className="flex-1 p-4 pt-0 overflow-hidden">
                    <PromptList
                        prompts={filteredPrompts}
                        defaultPromptId={defaultPromptId}
                        onEdit={handleEditPrompt}
                        onApply={handleApplyPrompt}
                        isLoading={isLoading}
                    />
                </TabsContent>

                <TabsContent value="default" className="flex-1 p-4 pt-0 overflow-hidden">
                    <PromptList
                        prompts={filteredPrompts}
                        defaultPromptId={defaultPromptId}
                        onEdit={handleEditPrompt}
                        onApply={handleApplyPrompt}
                        isLoading={isLoading}
                    />
                </TabsContent>

                <TabsContent value="conversation" className="flex-1 p-4 pt-0 overflow-hidden">
                    <PromptList
                        prompts={filteredPrompts}
                        defaultPromptId={defaultPromptId}
                        onEdit={handleEditPrompt}
                        onApply={handleApplyPrompt}
                        isLoading={isLoading}
                    />
                </TabsContent>

                <TabsContent value="favorites" className="flex-1 p-4 pt-0 overflow-hidden">
                    <PromptList
                        prompts={filteredPrompts}
                        defaultPromptId={defaultPromptId}
                        onEdit={handleEditPrompt}
                        onApply={handleApplyPrompt}
                        isLoading={isLoading}
                    />
                </TabsContent>
            </Tabs>
        </div>
    );
}

interface PromptListProps {
    prompts: SystemPrompt[];
    defaultPromptId: string | null;
    onEdit: (prompt: SystemPrompt) => void;
    onApply: (prompt: SystemPrompt) => void;
    isLoading: boolean;
}

function PromptList({ prompts, defaultPromptId, onEdit, onApply, isLoading }: PromptListProps) {
    if (isLoading) {
        return (
            <div className="flex items-center justify-center h-32">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            </div>
        );
    }

    if (prompts.length === 0) {
        return (
            <Card className="bg-gray-800/60 border-gray-700/50 text-center p-8">
                <CardContent>
                    <p className="text-gray-400">No prompts found</p>
                </CardContent>
            </Card>
        );
    }

    return (
        <ScrollArea className="h-full">
            <div className="space-y-3 p-1 pb-4">
                {prompts.map(prompt => (
                    <SystemPromptCard
                        key={prompt.guid}
                        prompt={prompt}
                        isDefault={prompt.guid === defaultPromptId}
                        onEdit={() => onEdit(prompt)}
                        onApply={() => onApply(prompt)}
                    />
                ))}
            </div>
        </ScrollArea>
    );
}