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
import { useSystemPromptManagement } from '@/hooks/useResourceManagement';

interface SystemPromptCardProps {
  prompt: SystemPrompt;
  isDefault: boolean;
  onEdit: () => void;
  onApply: () => void;
}

export function SystemPromptCard({ prompt, isDefault, onEdit, onApply }: SystemPromptCardProps) {
  const { setDefaultPromptId } = useSystemPromptStore();
  const { deleteSystemPrompt, setDefaultSystemPrompt } = useSystemPromptManagement();

  const [expanded, setExpanded] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);

  const handleSetDefault = async () => {
    if (!isDefault) {
      try {
        setIsProcessing(true);
        await setDefaultSystemPrompt(prompt.guid);
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
        <CardContent className="p-3"> {/* Reduced padding */}           <div className="flex justify-between items-start gap-2 mb-2"> {/* Top row: Title and Buttons */} 
            {/* Left side: Title and Default Badge - NOW CLICKABLE */}
            <div 
              className="flex-1 min-w-0 cursor-pointer" 
              onClick={toggleExpand} /* Moved click handler here */
              title={expanded ? "Collapse prompt" : "Expand prompt"} /* Add tooltip */
            >
              <div className="flex items-center gap-2"> 
                <h3 className="text-title text-base truncate">{prompt.title}</h3> {/* Truncate long titles */} 
                {isDefault && (
                  <Badge variant="outline" className="bg-blue-900/30 text-blue-300 border-blue-700 text-xs flex-shrink-0">
                    Default
                  </Badge>
                )}
              </div>
            </div>
            
            {/* Right side: Buttons */}
            <div className="flex gap-1 flex-shrink-0"> 
              {/* Apply Button */} 
              <Button
                variant="ghost"
                size="icon"
                onClick={onApply}
                className="text-gray-300 hover:text-gray-100 hover:bg-gray-700"
                disabled={isProcessing}
                title="Apply Prompt"
              >
                <Check className="h-4 w-4" />
              </Button>

              {/* Set Default Button */}
              <Button
                variant="ghost"
                size="icon"
                onClick={handleSetDefault}
                className={`${isDefault ? 'text-blue-400' : 'text-gray-400 hover:text-gray-100'} hover:bg-gray-700`}
                disabled={isDefault || isProcessing}
                title={isDefault ? "Default Prompt" : "Set as Default"}
              >
                <Star className={`h-4 w-4 ${isDefault ? 'fill-blue-400' : ''}`} />
              </Button>

              {/* Edit Button */} 
              <Button
                variant="ghost"
                size="icon"
                onClick={onEdit}
                className="text-gray-400 hover:text-gray-100 hover:bg-gray-700"
                disabled={isProcessing}
                title="Edit Prompt"
              >
                <Edit className="h-4 w-4" />
              </Button>

              {/* Delete Button */} 
              <Button
                variant="ghost"
                size="icon"
                onClick={handleDelete}
                className="text-red-400 hover:text-red-300 hover:bg-red-900/20"
                disabled={isProcessing}
                title="Delete Prompt"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          </div>

          {/* Description (optional) */} 
          {hasDescription && <p className="text-body text-sm mb-2">{prompt.description}</p>} 
          {/* Prompt Content - NO LONGER CLICKABLE */}
          <div
            className={`p-2 bg-gray-700/30 rounded ${expanded ? '' : 'line-clamp-2'}`} // Removed cursor-pointer
            // onClick={toggleExpand} // Removed click handler
          >
            <pre className="text-gray-300 text-xs font-mono whitespace-pre-wrap break-words">{prompt.content}</pre> {/* Smaller font */} 
          </div>

          {/* Tags (optional) */}
          {hasTags && (
            <div className="mt-2 flex flex-wrap gap-1">
              {prompt.tags.map((tag) => (
                <Badge key={tag} variant="outline" className="text-xs bg-gray-700/50 text-gray-300 border-gray-600">
                  {tag}
                </Badge>
              ))}
            </div>
          )}
          
          {/* Removed Date and original button row */}
        </CardContent>
      </Card>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent className="bg-gray-800 border-gray-700 text-gray-100">
          <AlertDialogHeader>
            <AlertDialogTitle>Are you sure you want to delete this prompt?</AlertDialogTitle>
            <AlertDialogDescription className="text-gray-400">
              This action cannot be undone. This will permanently delete the system prompt "{prompt.title}".
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel
              className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
              disabled={isProcessing}
            >
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
