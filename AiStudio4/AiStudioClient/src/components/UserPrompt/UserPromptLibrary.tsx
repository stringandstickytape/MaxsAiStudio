﻿
import { useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Input } from '@/components/ui/input';
import { PlusCircle, Search } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { UserPrompt } from '@/types/userPrompt';
import { UserPromptCard } from './UserPromptCard';
import { UserPromptEditor } from './UserPromptEditor';
import { useUserPromptManagement } from '@/hooks/useUserPromptManagement';

interface UserPromptLibraryProps {
  onInsertPrompt?: (prompt: UserPrompt) => void;
  initialEditPromptId?: string; // Add prop to receive initial edit ID
  initialShowEditor?: boolean; // Add prop to show editor immediately
  onEditorClosed?: () => void; // Add prop to handle editor close
}

export function UserPromptLibrary({ 
  onInsertPrompt, 
  initialEditPromptId, 
  initialShowEditor, 
  onEditorClosed 
}: UserPromptLibraryProps) {
  
  const { prompts, favoritePromptIds, isLoading, fetchUserPrompts } = useUserPromptManagement();

  const [searchTerm, setSearchTerm] = useState('');
  const [showEditor, setShowEditor] = useState(initialShowEditor || false);
  const [promptToEdit, setPromptToEdit] = useState<UserPrompt | null>(null);
  const [activeTab, setActiveTab] = useState('all');

  
  useEffect(() => {
    fetchUserPrompts();
  }, [fetchUserPrompts]);
  
  // Effect to handle initialEditPromptId
  useEffect(() => {
    if (initialEditPromptId && prompts.length > 0) {
      const promptToEditOnInit = prompts.find(p => p.guid === initialEditPromptId);
      if (promptToEditOnInit) {
        setPromptToEdit(promptToEditOnInit);
        setShowEditor(true);
      } else {
        console.warn(`UserPromptLibrary: Prompt with initialEditPromptId=${initialEditPromptId} not found.`);
      }
    }
    // Intentionally only run when the component mounts or the specific ID changes,
    // and prompts are loaded.
  }, [initialEditPromptId, prompts]);

  const handleCreatePrompt = () => {
    setPromptToEdit(null);
    setShowEditor(true);
  };

  const handleEditPrompt = (prompt: UserPrompt) => {
    setPromptToEdit(prompt);
    setShowEditor(true);
  };

  const handleCloseEditor = () => {
    setShowEditor(false);
    setPromptToEdit(null);
    if (onEditorClosed) {
      onEditorClosed();
    }
  };

  const handleApplyPrompt = (prompt: UserPrompt) => {
    if (onInsertPrompt) {
      onInsertPrompt(prompt);
    }
  };

  const getFilteredPrompts = () => {
    let filtered = prompts;

    
    if (activeTab === 'favorites') {
      filtered = filtered.filter((p) => p.isFavorite);
    }

    
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(
        (p) =>
          p.title.toLowerCase().includes(term) ||
          p.content.toLowerCase().includes(term) ||
          p.description?.toLowerCase().includes(term) ||
          p.tags?.some((tag) => tag.toLowerCase().includes(term)) ||
          p.shortcut?.toLowerCase().includes(term)
      );
    }

    return filtered;
  };

  const filteredPrompts = getFilteredPrompts();

  if (showEditor) {
    return (
      <div className="h-full flex flex-col">
        <div className="flex-1 overflow-auto p-4">
          <UserPromptEditor initialPrompt={promptToEdit} onClose={handleCloseEditor} onApply={handleApplyPrompt} />
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
            className="input-base input-with-icon"
          />
        </div>

        <Button
          onClick={handleCreatePrompt}
          className="w-full flex items-center justify-center gap-2 bg-blue-600 hover:bg-blue-700 text-white"
        >
          <PlusCircle className="h-4 w-4" /> Create New Prompt
        </Button>
      </div>

      <Tabs
        defaultValue="all"
        value={activeTab}
        onValueChange={setActiveTab}
        className="flex-1 flex flex-col overflow-hidden"
      >
        <TabsList className="grid grid-cols-2 mx-4 mt-4 mb-2 bg-gray-800 flex-none">
          <TabsTrigger value="all" className="data-[state=active]:bg-gray-700">
            All
          </TabsTrigger>
          <TabsTrigger value="favorites" className="data-[state=active]:bg-gray-700">
            Favorites
          </TabsTrigger>
        </TabsList>

        <TabsContent value="all" className="flex-1 p-4 pt-0 overflow-hidden">
          <PromptList
            prompts={filteredPrompts}
            onEdit={handleEditPrompt}
            onApply={handleApplyPrompt}
            isLoading={isLoading}
          />
        </TabsContent>

        <TabsContent value="favorites" className="flex-1 p-4 pt-0 overflow-hidden">
          <PromptList
            prompts={filteredPrompts}
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
  prompts: UserPrompt[];
  onEdit: (prompt: UserPrompt) => void;
  onApply: (prompt: UserPrompt) => void;
  isLoading: boolean;
}

function PromptList({ prompts, onEdit, onApply, isLoading }: PromptListProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-32">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (prompts.length === 0) {
    return (
      <Card className="card-base text-center p-8">
        <CardContent>
          <p className="text-gray-400">No prompts found</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <ScrollArea className="h-full">
      <div className="space-y-3 p-1 pb-4">
        {prompts.map((prompt) => (
          <UserPromptCard
            key={prompt.guid}
            prompt={prompt}
            onEdit={() => onEdit(prompt)}
            onApply={() => onApply(prompt)}
          />
        ))}
      </div>
    </ScrollArea>
  );
}


