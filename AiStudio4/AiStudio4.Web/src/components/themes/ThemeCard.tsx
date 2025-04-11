// AiStudio4.Web/src/components/themes/ThemeCard.tsx

import { Theme } from '@/types/theme';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Check, Plus, Trash2 } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ThemeCardProps {
  theme: Theme;
  isSelected: boolean;
  onSelect: (themeId: string) => void;
  onDelete: (themeId: string) => void;
  onApply: (theme: Theme) => void;
}

export function ThemeCard({ theme, isSelected, onSelect, onDelete, onApply }: ThemeCardProps) {
  const handleDelete = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (window.confirm(`Are you sure you want to delete the theme "${theme.name}"?`)) {
      onDelete(theme.guid);
    }
  };

  return (
    <Card 
      className={cn(
        "card-base cursor-pointer transition-all hover:shadow-md",
        isSelected ? "border-blue-500 border-2" : "border-gray-700"
      )}
      onClick={() => onSelect(theme.guid)}
    >
      <CardContent className="p-4">
        <div className="flex justify-between items-start">
          <div>
            <h3 className="text-title text-lg">{theme.name}</h3>
            <p className="text-body text-sm">{theme.description}</p>
            {theme.author && (
              <p className="text-xs text-gray-400 mt-1">By: {theme.author}</p>
            )}
            
            {/* Preview color swatches */}
            <div className="flex mt-2 space-x-1">
              {theme.previewColors.map((color, index) => (
                <div 
                  key={index}
                  className="w-6 h-6 rounded-full border border-gray-600"
                  style={{ backgroundColor: color }}
                  title={color}
                />
              ))}
            </div>
          </div>
          
          <div className="flex flex-col gap-2">
            <Button
              variant="ghost"
              size="icon"
              onClick={handleDelete}
              className="h-8 w-8 text-gray-400 hover:text-red-400"
              title="Delete Theme"
            >
              <Trash2 className="h-4 w-4" />
            </Button>
            
            <div className="flex flex-col gap-2 mt-2">
              <Button
                variant={isSelected ? "default" : "outline"}
                size="sm"
                onClick={(e) => {
                  e.stopPropagation();
                  onSelect(theme.guid);
                }}
                className={`w-full ${isSelected ? 
                  'bg-blue-600 hover:bg-blue-700 text-white' : 
                  'bg-gray-700 hover:bg-gray-600 text-gray-200 border-gray-600'
                }`}
              >
                {isSelected ? (
                  <>
                    <Check className="h-4 w-4 mr-1" />
                    <span>Selected</span>
                  </>
                ) : (
                  <>
                    <Plus className="h-4 w-4 mr-1" />
                    <span>Select</span>
                  </>
                )}
              </Button>
              
              <Button
                variant="outline"
                size="sm"
                onClick={(e) => {
                  e.stopPropagation();
                  onApply(theme);
                }}
                className="w-full bg-green-900/20 hover:bg-green-700/30 border-green-800/30 text-gray-200"
              >
                Apply Theme
              </Button>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}