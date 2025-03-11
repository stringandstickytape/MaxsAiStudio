// src/components/FileAttachment.tsx
import React, { useRef } from 'react';
import { Button } from '@/components/ui/button';
import { Paperclip, X } from 'lucide-react';
import { cn } from '@/lib/utils';

interface FileAttachmentProps {
  onAttach: (file: File, content: string) => void;
  disabled?: boolean;
  className?: string;
}

export const FileAttachment: React.FC<FileAttachmentProps> = ({ onAttach, disabled = false, className }) => {
  const inputRef = useRef<HTMLInputElement>(null);

  const handleButtonClick = () => {
    if (localStorage.getItem('isVisualStudio') === 'true') {
      window.chrome?.webview?.postMessage({
        type: 'send',
        content: 'test',
        selectedTools: '',
        addEmbeddings: 'false',
      });

      // Reset input value so the same file can be selected again
      if (inputRef.current) {
        inputRef.current.value = '';
      }
      return;
    }

    if (inputRef.current) {
      inputRef.current.click();
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    try {
      // Read file as text
      const text = await readFileAsText(file);

      // Pass file and content to parent component
      onAttach(file, text);

      // Reset input value so the same file can be selected again
      if (inputRef.current) {
        inputRef.current.value = '';
      }
    } catch (error) {
      console.error('Error reading file:', error);
      // You could show a toast notification here
    }
  };

  const readFileAsText = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (event) => {
        if (event.target?.result) {
          resolve(event.target.result as string);
        } else {
          reject(new Error('Failed to read file content'));
        }
      };
      reader.onerror = () => reject(new Error('File reading error'));
      reader.readAsText(file);
    });
  };

  return (
    <div className={cn('relative', className)}>
      <input
        ref={inputRef}
        type="file"
        accept=".txt,.md,.js,.jsx,.ts,.tsx,.py,.html,.css,.json,.csv"
        onChange={handleFileChange}
        className="hidden"
        disabled={disabled}
      />
      <Button
        variant="outline"
        size="icon"
        type="button"
        onClick={handleButtonClick}
        disabled={disabled}
        className="btn-ghost icon-btn bg-gray-800 border-gray-700 hover:text-blue-400"
        aria-label="Attach file"
      >
        <Paperclip className="h-5 w-5" />
      </Button>
    </div>
  );
};

// Component to display attached files
interface AttachedFileProps {
  filename: string;
  onRemove: () => void;
}

export const AttachedFileDisplay: React.FC<AttachedFileProps> = ({ filename, onRemove }) => {
  return (
    <div className="flex items-center gap-2 py-1 px-2 bg-gray-800 rounded border border-gray-700 text-sm text-gray-300">
      <span className="truncate max-w-[150px]">{filename}</span>
      <Button
        variant="ghost"
        size="icon"
        onClick={onRemove}
        className="h-5 w-5 p-0 text-gray-400 hover:text-gray-100 hover:bg-gray-700/50"
      >
        <X className="h-3 w-3" />
      </Button>
    </div>
  );
};
