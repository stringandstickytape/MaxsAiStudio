// src/components/settings/FontSizeControl.tsx
import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Slider } from '@/components/ui/slider';
import { Plus, Minus, Save } from 'lucide-react';
import { useAppearanceStore } from '@/stores/useAppearanceStore';

interface FontSizeControlProps {
  onChange?: (fontSize: number) => void;
  onSave?: () => void;
}

export function FontSizeControl({ onChange, onSave }: FontSizeControlProps) {
  
  const { fontSize, setFontSize, increaseFontSize, decreaseFontSize, saveAppearanceSettings, isLoading } =
    useAppearanceStore();

  const [hasChanges, setHasChanges] = useState(false);

  
  const handleFontSizeChange = (newSize: number) => {
    setFontSize(newSize);
    setHasChanges(true);

    if (onChange) {
      onChange(newSize);
    }
  };

  const handleSave = async () => {
    try {
      await saveAppearanceSettings();
      setHasChanges(false);

      if (onSave) {
        onSave();
      }
    } catch (error) {
      console.error('Failed to save font size:', error);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between mb-2">
        <div className="text-sm font-medium text-gray-300">Font Size: {fontSize}px</div>
        {hasChanges && (
          <Button
            variant="outline"
            size="sm"
            onClick={handleSave}
            disabled={isLoading}
            className="bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600 flex items-center gap-1"
          >
            <Save className="h-4 w-4" />
            <span>Save</span>
          </Button>
        )}
      </div>

      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="icon"
          onClick={() => {
            decreaseFontSize();
            setHasChanges(true);
          }}
          disabled={fontSize <= 8 || isLoading}
          className="h-8 w-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
        >
          <Minus className="h-4 w-4" />
        </Button>

        <Slider
          value={[fontSize]}
          min={8}
          max={24}
          step={1}
          onValueChange={(values) => handleFontSizeChange(values[0])}
          disabled={isLoading}
          className="flex-1"
        />

        <Button
          variant="outline"
          size="icon"
          onClick={() => {
            increaseFontSize();
            setHasChanges(true);
          }}
          disabled={fontSize >= 24 || isLoading}
          className="h-8 w-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600"
        >
          <Plus className="h-4 w-4" />
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-2 mt-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => handleFontSizeChange(14)}
          className={`h-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600 ${fontSize === 14 ? 'ring-2 ring-blue-500' : ''}`}
        >
          Small
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => handleFontSizeChange(16)}
          className={`h-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600 ${fontSize === 16 ? 'ring-2 ring-blue-500' : ''}`}
        >
          Medium
        </Button>
        <Button
          variant="outline"
          size="sm"
          onClick={() => handleFontSizeChange(18)}
          className={`h-8 bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600 ${fontSize === 18 ? 'ring-2 ring-blue-500' : ''}`}
        >
          Large
        </Button>
      </div>

      <div className="mt-4 text-small-gray-400">
        <p>Adjust the font size of the entire application. Changes will be applied immediately.</p>
      </div>
    </div>
  );
}

