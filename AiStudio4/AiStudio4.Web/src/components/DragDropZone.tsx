import React, { useState, useRef } from 'react';
import { Upload } from 'lucide-react';
import { cn } from '@/lib/utils';

interface DragDropZoneProps {
  onFilesSelected: (files: File[]) => void;
  disabled?: boolean;
  className?: string;
  maxFiles?: number;
  acceptedTypes?: string;
  children?: React.ReactNode;
}

export const DragDropZone: React.FC<DragDropZoneProps> = ({
  onFilesSelected,
  disabled = false,
  className,
  maxFiles = 5,
  acceptedTypes = '',
  children
}) => {
  const [isDragging, setIsDragging] = useState(false);
  const [dragCounter, setDragCounter] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleDragIn = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setDragCounter(prevCount => prevCount + 1);
    if (e.dataTransfer.items && e.dataTransfer.items.length > 0) {
      setIsDragging(true);
    }
  };

  const handleDragOut = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setDragCounter(prevCount => prevCount - 1);
    if (dragCounter <= 1) {
      setIsDragging(false);
    }
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
    setDragCounter(0);
    
    if (disabled) return;
    
    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      const filesArray = Array.from(e.dataTransfer.files);
      const filesToUpload = filesArray.slice(0, maxFiles);
      onFilesSelected(filesToUpload);
      e.dataTransfer.clearData();
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const filesArray = Array.from(e.target.files);
      const filesToUpload = filesArray.slice(0, maxFiles);
      onFilesSelected(filesToUpload);
      
      
      if (inputRef.current) {
        inputRef.current.value = '';
      }
    }
  };

  const handleClick = () => {
    if (disabled) return;
    inputRef.current?.click();
  };

  return (
    <div
      className={cn(
        'relative border-2 border-dashed rounded-lg p-6 transition-colors',
        isDragging ? 'border-blue-500 bg-blue-500/10' : 'border-gray-700 bg-gray-800/50 hover:bg-gray-700/50',
        disabled ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer',
        className
      )}
      onDragEnter={handleDragIn}
      onDragLeave={handleDragOut}
      onDragOver={handleDragOver}
      onDrop={handleDrop}
      onClick={handleClick}
    >
      <input
        ref={inputRef}
        type="file"
        multiple
        accept={acceptedTypes}
        onChange={handleInputChange}
        className="hidden"
        disabled={disabled}
      />
      
      {children || (
        <div className="flex flex-col items-center justify-center space-y-2 text-center">
          <Upload className="h-8 w-8 text-gray-400" />
          <div className="space-y-1">
            <p className="text-sm font-medium text-gray-300">
              Drag files here or click to upload
            </p>
            <p className="text-xs text-gray-500">
              Upload up to {maxFiles} files
            </p>
          </div>
        </div>
      )}
      
      {isDragging && (
        <div className="absolute inset-0 bg-blue-500/20 flex items-center justify-center rounded-lg z-10">
          <p className="text-blue-500 font-medium">Drop files to upload</p>
        </div>
      )}
    </div>
  );
};