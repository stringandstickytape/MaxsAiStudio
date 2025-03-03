// src/components/SystemPrompt/SystemPromptLibrary.tsx
import { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { RootState } from '@/store/store';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Input } from '@/components/ui/input';
import { PlusCircle, Search, X, Check } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { SystemPrompt } from '@/types/systemPrompt';
import { fetchSystemPrompts, setCurrentPrompt, toggleLibrary } from '@/store/systemPromptSlice';
import { SystemPromptCard } from './SystemPromptCard';
import { SystemPromptEditor } from './SystemPromptEditor';

interface SystemPromptLibraryProps {
  onApplyPrompt?: (prompt: SystemPrompt) => void;
  onClose?: () => void;
  isPinned?: boolean;
  isOpen: boolean;
  conversationId?: string;
}

export function SystemPromptLibrary({
  onApplyPrompt,
  onClose,
  isPinned = false,
  isOpen,
  conversationId
}: SystemPromptLibraryProps) {
  const dispatch = useDispatch();
  const { prompts, loading, defaultPromptId, conversationPrompts } = useSelector((state: RootState) => state.systemPrompts);
  
  const [searchTerm, setSearchTerm] = useState('');
  const [showEditor, setShowEditor] = useState(false);
  const [promptToEdit, setPromptToEdit] = useState<SystemPrompt | null>(null);
  const [activeTab, setActiveTab] = useState('all');
  
  // Load prompts on component mount
  useEffect(() => {
    if (isOpen) {
      dispatch(fetchSystemPrompts());
    }
  }, [dispatch, isOpen]);
  
  const handleCloseLibrary = () => {
    if (onClose) {
      onClose();
    } else {
      dispatch(toggleLibrary(false));
    }
  };
  
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
  
  const handleApplyPrompt = (prompt: SystemPrompt) => {
    if (onApplyPrompt) {
      onApplyPrompt(prompt);
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
  
  if (!isOpen) return null;
  
  if (showEditor) {
    return (
        <div className="p-4 h-full overflow-y-auto flex flex-col max-h-[calc(100vh-80px)]">
        <SystemPromptEditor 
          initialPrompt={promptToEdit} 
          onClose={handleCloseEditor} 
          onApply={handleApplyPrompt}
        />
      </div>
    );
  }
  
  return (
    <div className="h-full flex flex-col">
      <div className="p-4 border-b border-gray-700">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-gray-100">System Prompts</h2>
          {!isPinned && onClose && (
            <Button 
              variant="ghost" 
              size="icon" 
              onClick={handleCloseLibrary}
              className="text-gray-400 hover:text-gray-100"
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
        
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
      
      <Tabs defaultValue="all" value={activeTab} onValueChange={setActiveTab} className="flex-1 flex flex-col">
        <TabsList className="grid grid-cols-4 m-4 bg-gray-800">
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
        
        <TabsContent value="all" className="flex-1 p-4 pt-0">
          <PromptList 
            prompts={filteredPrompts} 
            defaultPromptId={defaultPromptId} 
            onEdit={handleEditPrompt} 
            onApply={handleApplyPrompt}
            isLoading={loading}
          />
        </TabsContent>
        
        <TabsContent value="default" className="flex-1 p-4 pt-0">
          <PromptList 
            prompts={filteredPrompts} 
            defaultPromptId={defaultPromptId} 
            onEdit={handleEditPrompt} 
            onApply={handleApplyPrompt}
            isLoading={loading}
          />
        </TabsContent>
        
        <TabsContent value="conversation" className="flex-1 p-4 pt-0">
          <PromptList 
            prompts={filteredPrompts} 
            defaultPromptId={defaultPromptId} 
            onEdit={handleEditPrompt} 
            onApply={handleApplyPrompt}
            isLoading={loading}
          />
        </TabsContent>
        
        <TabsContent value="favorites" className="flex-1 p-4 pt-0">
          <PromptList 
            prompts={filteredPrompts} 
            defaultPromptId={defaultPromptId} 
            onEdit={handleEditPrompt} 
            onApply={handleApplyPrompt}
            isLoading={loading}
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
    <ScrollArea className="h-[calc(100vh-300px)]">
      <div className="space-y-3">
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
